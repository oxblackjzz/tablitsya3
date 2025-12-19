using Microsoft.EntityFrameworkCore;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для роботи зі скануванням деталей
    /// </summary>
    public class ScanningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectFileParserService _parser;
        private readonly LoggingService _logger;

        public ScanningService(ApplicationDbContext context, ProjectFileParserService parser, LoggingService logger)
        {
            _context = context;
            _parser = parser;
            _logger = logger;
        }

        #region Імпорт проектів

        /// <summary>
        /// Імпорт проекту з XML контенту
        /// </summary>
        public async Task<ImportResult> ImportProjectAsync(string xmlContent, string fileName, bool clearExisting = false, int workshopNumber = 1)
        {
            var result = new ImportResult();

            try
            {
                // Валідація
                if (!_parser.ValidateProjectXml(xmlContent))
                {
                    result.Message = "Невалідний XML файл";
                    result.Errors.Add("Файл не є валідним .project файлом");
                    return result;
                }

                // Парсинг
                var project = _parser.ParseProjectFile(xmlContent, fileName);
                if (project == null)
                {
                    result.Message = "Помилка парсингу файлу";
                    return result;
                }

                // Перевірка чи проект вже існує
                var existingProject = await _context.ImportedProjects
                    .FirstOrDefaultAsync(p => p.ProjectUuid == project.ProjectUuid);

                if (existingProject != null)
                {
                    if (clearExisting)
                    {
                        // Видаляємо існуючі дані
                        await DeleteProjectDataAsync(project.ProjectUuid);
                    }
                    else
                    {
                        result.Message = $"Проект {project.ProjectUuid} вже існує";
                        result.Errors.Add("Використайте опцію 'Очистити існуючі' для перезапису");
                        return result;
                    }
                }

                // Зберігаємо проект
                var projectEntity = new ImportedProjectEntity
                {
                    ProjectUuid = project.ProjectUuid,
                    FileName = fileName,
                    ImportedDate = DateTime.UtcNow,
                    TotalCost = project.TotalCost,
                    MaterialCost = project.MaterialCost,
                    OperationCost = project.OperationCost,
                    Currency = project.Currency,
                    Version = project.Version,
                    ProductsCount = project.ProductsCount,
                    PartsCount = project.PartsCount,
                    TotalSquareMeters = project.TotalSquareMeters,
                    IsActive = true,
                    WorkshopNumber = workshopNumber
                };

                _context.ImportedProjects.Add(projectEntity);

                // Зберігаємо товари та деталі
                int addedParts = 0;
                int skippedParts = 0;

                foreach (var product in project.Products)
                {
                    // Зберігаємо товар
                    var productEntity = new ProductEntity
                    {
                        ProjectUuid = project.ProjectUuid,
                        ProductId = product.ProductId,
                        Name = product.Name,
                        Code = product.Code,
                        Description = product.Description,
                        Count = product.Count,
                        Cost = product.Cost,
                        MaterialCost = product.MaterialCost,
                        OperationCost = product.OperationCost,
                        OrderDate = product.OrderDate,
                        CreatedDate = DateTime.UtcNow
                    };

                    _context.Products.Add(productEntity);

                    // Зберігаємо деталі
                    foreach (var part in product.Parts)
                    {
                        // Перевіряємо чи деталь вже існує
                        var existingPart = await _context.Parts
                            .FirstOrDefaultAsync(p => 
                                p.ProjectExternalUuid == part.ProjectExternalUuid &&
                                p.PartId == part.PartId &&
                                p.PartCounter == part.PartCounter);

                        if (existingPart != null)
                        {
                            skippedParts++;
                            continue;
                        }

                        var partEntity = MapPartToEntity(part);
                        _context.Parts.Add(partEntity);
                        addedParts++;
                    }
                }

                await _context.SaveChangesAsync();

                result.Success = true;
                result.AddedCount = addedParts;
                result.SkippedCount = skippedParts;
                result.TotalCount = addedParts + skippedParts;
                result.Project = project;
                result.Message = $"Імпортовано {addedParts} деталей, пропущено {skippedParts}";

                _logger.LogInfo($"Імпорт проекту {project.ProjectUuid}: {addedParts} додано, {skippedParts} пропущено", "ScanningService");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка імпорту проекту: {ex.Message}", ex, "ScanningService");
                result.Message = $"Помилка: {ex.Message}";
                result.Errors.Add(ex.ToString());
                return result;
            }
        }

        /// <summary>
        /// Видалення даних проекту
        /// </summary>
        public async Task<int> DeleteProjectDataAsync(string projectUuid)
        {
            var partsCount = await _context.Parts
                .Where(p => p.ProjectExternalUuid == projectUuid)
                .ExecuteDeleteAsync();

            await _context.Products
                .Where(p => p.ProjectUuid == projectUuid)
                .ExecuteDeleteAsync();

            await _context.ImportedProjects
                .Where(p => p.ProjectUuid == projectUuid)
                .ExecuteDeleteAsync();

            await _context.ScanLogs
                .Where(s => s.QRCode.StartsWith(projectUuid))
                .ExecuteDeleteAsync();

            _logger.LogInfo($"Видалено проект {projectUuid}: {partsCount} деталей", "ScanningService");

            return partsCount;
        }

        #endregion

        #region Сканування

        /// <summary>
        /// Сканування QR-коду
        /// </summary>
        public async Task<ScanResult> ScanQRCodeAsync(string qrCode, ProductionStage? stage = null, string? userId = null, string? deviceId = null)
        {
            var result = new ScanResult { Success = false };

            try
            {
                // Парсинг QR-коду
                var parsedPart = Part.ParseQRCode(qrCode);
                if (parsedPart == null)
                {
                    result.Message = "Невірний формат QR-коду";
                    await LogScanAsync(0, qrCode, stage, false, result.Message, userId, deviceId);
                    return result;
                }

                // Пошук деталі в БД
                var partEntity = await _context.Parts
                    .FirstOrDefaultAsync(p =>
                        p.ProjectExternalUuid == parsedPart.ProjectExternalUuid &&
                        p.PartId == parsedPart.PartId &&
                        p.PartCounter == parsedPart.PartCounter);

                if (partEntity == null)
                {
                    result.Message = "Деталь не знайдена в базі даних";
                    await LogScanAsync(0, qrCode, stage, false, result.Message, userId, deviceId);
                    return result;
                }

                var part = MapEntityToPart(partEntity);

                // Якщо етап не вказано - автоматичне визначення
                if (!stage.HasValue)
                {
                    stage = part.CurrentStage;
                    if (!stage.HasValue)
                    {
                        result.Success = true;
                        result.Part = part;
                        result.Message = "Всі етапи вже завершені";
                        result.IsFullyCompleted = true;
                        await LogScanAsync(partEntity.Id, qrCode, null, true, result.Message, userId, deviceId);
                        return result;
                    }
                }

                // Перевірка чи можна перейти до етапу
                if (!part.CanAdvanceToStage(stage.Value))
                {
                    result.Part = part;
                    result.Stage = stage;
                    
                    if (!part.IsStageRequired(stage.Value))
                    {
                        result.Message = $"Етап '{stage.Value.GetDisplayName()}' не потрібен для цієї деталі";
                    }
                    else if (part.IsStageCompleted(stage.Value))
                    {
                        result.Message = $"Етап '{stage.Value.GetDisplayName()}' вже завершений";
                    }
                    else
                    {
                        result.Message = $"Неможливо перейти до етапу '{stage.Value.GetDisplayName()}' - не завершені попередні етапи";
                    }
                    
                    await LogScanAsync(partEntity.Id, qrCode, stage, false, result.Message, userId, deviceId);
                    return result;
                }

                // Завершуємо етап
                CompleteStageOnEntity(partEntity, stage.Value);
                await _context.SaveChangesAsync();

                // Оновлюємо модель
                part = MapEntityToPart(partEntity);

                result.Success = true;
                result.Part = part;
                result.Stage = stage;
                result.IsFullyCompleted = part.IsFullyCompleted;
                
                if (stage.Value == ProductionStage.EdgeBanding && part.EdgeBandingSidesRequired > 0)
                {
                    if (part.IsEdgeBandingFullyCompleted())
                    {
                        result.Message = $"Обклейка кромкою завершена ({part.EdgeBandingSidesCompleted}/{part.EdgeBandingSidesRequired})";
                    }
                    else
                    {
                        result.Message = $"Обклейка: скан {part.EdgeBandingSidesCompleted} з {part.EdgeBandingSidesRequired}";
                    }
                }
                else
                {
                    result.Message = $"Етап '{stage.Value.GetDisplayName()}' завершено";
                }

                if (result.IsFullyCompleted)
                {
                    result.Message += " 🎉 Деталь повністю завершена!";
                }

                await LogScanAsync(partEntity.Id, qrCode, stage, true, result.Message, userId, deviceId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка сканування: {ex.Message}", ex, "ScanningService");
                result.Message = $"Помилка: {ex.Message}";
                return result;
            }
        }

        /// <summary>
        /// Пошук деталі за QR-кодом
        /// </summary>
        public async Task<Part?> GetPartByQRCodeAsync(string qrCode)
        {
            var parsed = Part.ParseQRCode(qrCode);
            if (parsed == null) return null;

            var entity = await _context.Parts
                .FirstOrDefaultAsync(p =>
                    p.ProjectExternalUuid == parsed.ProjectExternalUuid &&
                    p.PartId == parsed.PartId &&
                    p.PartCounter == parsed.PartCounter);

            return entity != null ? MapEntityToPart(entity) : null;
        }

        private async Task LogScanAsync(int partId, string qrCode, ProductionStage? stage, bool success, string? message, string? userId, string? deviceId)
        {
            var log = new ScanLogEntity
            {
                PartId = partId,
                QRCode = qrCode,
                Stage = stage.HasValue ? (int)stage.Value : 0,
                ScanDate = DateTime.UtcNow,
                UserId = userId,
                DeviceId = deviceId,
                Success = success,
                Message = message
            };

            _context.ScanLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        #endregion

        #region Отримання даних

        /// <summary>
        /// Отримання списку деталей з пагінацією
        /// </summary>
        public async Task<(List<Part> Parts, int Total)> GetPartsAsync(
            int page = 1, 
            int pageSize = 50,
            string? projectUuid = null,
            string? fileName = null,
            string? orderName = null,
            bool? completed = null,
            ProductionStage? currentStage = null)
        {
            var query = _context.Parts.AsQueryable();

            // Фільтри
            if (!string.IsNullOrEmpty(projectUuid))
                query = query.Where(p => p.ProjectExternalUuid == projectUuid);

            if (!string.IsNullOrEmpty(fileName))
                query = query.Where(p => p.SourceFileName == fileName);

            if (!string.IsNullOrEmpty(orderName))
                query = query.Where(p => p.OrderName.Contains(orderName));

            if (completed.HasValue)
            {
                if (completed.Value)
                    query = query.Where(p => p.IsPackingCompleted);
                else
                    query = query.Where(p => !p.IsPackingCompleted);
            }

            var total = await query.CountAsync();

            var entities = await query
                .OrderByDescending(p => p.CreatedDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var parts = entities.Select(MapEntityToPart).ToList();

            // Фільтр по поточному етапу (після маппінгу)
            if (currentStage.HasValue)
            {
                parts = parts.Where(p => p.CurrentStage == currentStage.Value).ToList();
            }

            return (parts, total);
        }

        /// <summary>
        /// Отримання списку імпортованих проектів
        /// </summary>
        public async Task<List<ImportedProjectEntity>> GetProjectsAsync()
        {
            return await _context.ImportedProjects
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.ImportedDate)
                .ToListAsync();
        }

        /// <summary>
        /// Отримання унікальних назв файлів
        /// </summary>
        public async Task<List<string>> GetUniqueFileNamesAsync()
        {
            return await _context.Parts
                .Where(p => !string.IsNullOrEmpty(p.SourceFileName))
                .Select(p => p.SourceFileName)
                .Distinct()
                .OrderBy(f => f)
                .ToListAsync();
        }

        /// <summary>
        /// Отримання унікальних назв замовлень
        /// </summary>
        public async Task<List<string>> GetUniqueOrderNamesAsync(string? projectUuid = null)
        {
            var query = _context.Parts.AsQueryable();

            if (!string.IsNullOrEmpty(projectUuid))
                query = query.Where(p => p.ProjectExternalUuid == projectUuid);

            return await query
                .Where(p => !string.IsNullOrEmpty(p.OrderName))
                .Select(p => p.OrderName)
                .Distinct()
                .OrderBy(n => n)
                .ToListAsync();
        }

        /// <summary>
        /// Отримання статистики по проекту
        /// </summary>
        public async Task<ProjectStatistics?> GetProjectStatisticsAsync(string projectUuid)
        {
            var parts = await _context.Parts
                .Where(p => p.ProjectExternalUuid == projectUuid)
                .ToListAsync();

            if (!parts.Any()) return null;

            var stats = new ProjectStatistics
            {
                ProjectUuid = projectUuid,
                FileName = parts.First().SourceFileName,
                TotalParts = parts.Count,
                CompletedParts = parts.Count(p => p.IsPackingCompleted),
                TotalSquareMeters = parts.Sum(p => p.SquareMeters),
                CompletedSquareMeters = parts.Where(p => p.IsPackingCompleted).Sum(p => p.SquareMeters)
            };

            // Статистика по етапах
            foreach (ProductionStage stage in Enum.GetValues<ProductionStage>())
            {
                var stageStats = new StageStatistics
                {
                    Stage = stage,
                    TotalRequired = parts.Count(p => IsStageRequired(p, stage)),
                    Completed = parts.Count(p => IsStageCompleted(p, stage)),
                    InProgress = 0 // Може бути визначено додатково
                };

                stats.StageStats[stage] = stageStats;
            }

            return stats;
        }

        /// <summary>
        /// Отримання кількості деталей
        /// </summary>
        public async Task<int> GetPartsCountAsync(string? projectUuid = null)
        {
            var query = _context.Parts.AsQueryable();

            if (!string.IsNullOrEmpty(projectUuid))
                query = query.Where(p => p.ProjectExternalUuid == projectUuid);

            return await query.CountAsync();
        }

        #endregion

        #region Mapping helpers

        private PartEntity MapPartToEntity(Part part)
        {
            return new PartEntity
            {
                ProjectExternalUuid = part.ProjectExternalUuid,
                PartId = part.PartId,
                PartCounter = part.PartCounter,
                Name = part.Name,
                Code = part.Code,
                Length = part.Length,
                Width = part.Width,
                Thickness = part.Thickness,
                Material = part.Material,
                Quantity = part.Quantity,
                CreatedDate = part.CreatedDate,
                SourceFileName = part.SourceFileName,
                OrderName = part.OrderName,
                IsCutCompleted = part.IsCutCompleted,
                CutCompletedDate = part.CutCompletedDate,
                IsEdgeBandingCompleted = part.IsEdgeBandingCompleted,
                EdgeBandingCompletedDate = part.EdgeBandingCompletedDate,
                IsDrillingCompleted = part.IsDrillingCompleted,
                DrillingCompletedDate = part.DrillingCompletedDate,
                IsSortingCompleted = part.IsSortingCompleted,
                SortingCompletedDate = part.SortingCompletedDate,
                IsPackingCompleted = part.IsPackingCompleted,
                PackingCompletedDate = part.PackingCompletedDate,
                RequiresCutting = part.RequiresCutting,
                RequiresEdgeBanding = part.RequiresEdgeBanding,
                RequiresDrilling = part.RequiresDrilling,
                RequiresSorting = part.RequiresSorting,
                RequiresPacking = part.RequiresPacking,
                EdgeBandingSidesRequired = part.EdgeBandingSidesRequired,
                EdgeBandingSidesCompleted = part.EdgeBandingSidesCompleted,
                EdgeBandingCompletedDates = part.EdgeBandingCompletedDates
            };
        }

        private Part MapEntityToPart(PartEntity entity)
        {
            return new Part
            {
                Id = entity.Id,
                ProjectExternalUuid = entity.ProjectExternalUuid,
                PartId = entity.PartId,
                PartCounter = entity.PartCounter,
                Name = entity.Name,
                Code = entity.Code,
                Length = entity.Length,
                Width = entity.Width,
                Thickness = entity.Thickness,
                Material = entity.Material,
                Quantity = entity.Quantity,
                CreatedDate = entity.CreatedDate,
                SourceFileName = entity.SourceFileName,
                OrderName = entity.OrderName,
                IsCutCompleted = entity.IsCutCompleted,
                CutCompletedDate = entity.CutCompletedDate,
                IsEdgeBandingCompleted = entity.IsEdgeBandingCompleted,
                EdgeBandingCompletedDate = entity.EdgeBandingCompletedDate,
                IsDrillingCompleted = entity.IsDrillingCompleted,
                DrillingCompletedDate = entity.DrillingCompletedDate,
                IsSortingCompleted = entity.IsSortingCompleted,
                SortingCompletedDate = entity.SortingCompletedDate,
                IsPackingCompleted = entity.IsPackingCompleted,
                PackingCompletedDate = entity.PackingCompletedDate,
                RequiresCutting = entity.RequiresCutting,
                RequiresEdgeBanding = entity.RequiresEdgeBanding,
                RequiresDrilling = entity.RequiresDrilling,
                RequiresSorting = entity.RequiresSorting,
                RequiresPacking = entity.RequiresPacking,
                EdgeBandingSidesRequired = entity.EdgeBandingSidesRequired,
                EdgeBandingSidesCompleted = entity.EdgeBandingSidesCompleted,
                EdgeBandingCompletedDates = entity.EdgeBandingCompletedDates
            };
        }

        private void CompleteStageOnEntity(PartEntity entity, ProductionStage stage)
        {
            var now = DateTime.UtcNow;

            switch (stage)
            {
                case ProductionStage.Cutting:
                    entity.IsCutCompleted = true;
                    entity.CutCompletedDate = now;
                    break;

                case ProductionStage.EdgeBanding:
                    if (entity.EdgeBandingSidesRequired > 0)
                    {
                        entity.EdgeBandingSidesCompleted++;
                        var dates = string.IsNullOrEmpty(entity.EdgeBandingCompletedDates)
                            ? new List<string>()
                            : entity.EdgeBandingCompletedDates.Split(';').ToList();
                        dates.Add(now.ToString("yyyy-MM-dd HH:mm:ss"));
                        entity.EdgeBandingCompletedDates = string.Join(";", dates);

                        if (entity.EdgeBandingSidesCompleted >= entity.EdgeBandingSidesRequired)
                        {
                            entity.IsEdgeBandingCompleted = true;
                            entity.EdgeBandingCompletedDate = now;
                        }
                    }
                    else
                    {
                        entity.IsEdgeBandingCompleted = true;
                        entity.EdgeBandingCompletedDate = now;
                    }
                    break;

                case ProductionStage.Drilling:
                    entity.IsDrillingCompleted = true;
                    entity.DrillingCompletedDate = now;
                    break;

                case ProductionStage.Sorting:
                    entity.IsSortingCompleted = true;
                    entity.SortingCompletedDate = now;
                    break;

                case ProductionStage.Packing:
                    entity.IsPackingCompleted = true;
                    entity.PackingCompletedDate = now;
                    break;
            }
        }

        private bool IsStageRequired(PartEntity entity, ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => entity.RequiresCutting,
                ProductionStage.EdgeBanding => entity.RequiresEdgeBanding,
                ProductionStage.Drilling => entity.RequiresDrilling,
                ProductionStage.Sorting => entity.RequiresSorting,
                ProductionStage.Packing => entity.RequiresPacking,
                _ => false
            };
        }

        private bool IsStageCompleted(PartEntity entity, ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => entity.IsCutCompleted,
                ProductionStage.EdgeBanding => entity.EdgeBandingSidesRequired > 0 
                    ? entity.EdgeBandingSidesCompleted >= entity.EdgeBandingSidesRequired 
                    : entity.IsEdgeBandingCompleted,
                ProductionStage.Drilling => entity.IsDrillingCompleted,
                ProductionStage.Sorting => entity.IsSortingCompleted,
                ProductionStage.Packing => entity.IsPackingCompleted,
                _ => false
            };
        }

        #endregion
    }
}
