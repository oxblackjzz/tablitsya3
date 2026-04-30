using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tablitsya3.Data;
using Tablitsya3.Data.Entities;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    // WorkerScanContext визначено в WorkerAuthService.cs

    /// <summary>
    /// Сервіс для роботи зі скануванням деталей
    /// </summary>
    public class ScanningService
    {
        private readonly ApplicationDbContext _context;
        private readonly ProjectFileParserService _parser;
        private readonly LoggingService _logger;
        private readonly IMemoryCache _cache;

        // Ключі кешування
        private const string CACHE_KEY_OVERALL_STATS = "scanning_overall_stats";
        private const string CACHE_KEY_PROJECT_STATS_PREFIX = "scanning_project_stats_";
        private static readonly TimeSpan StatsCacheExpiration = TimeSpan.FromMinutes(2);

        public ScanningService(
            ApplicationDbContext context, 
            ProjectFileParserService parser, 
            LoggingService logger,
            IMemoryCache cache)
        {
            _context = context;
            _parser = parser;
            _logger = logger;
            _cache = cache;
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
                    LdpColors = project.LdpColors,
                    IsActive = true,
                    WorkshopNumber = workshopNumber
                };

                _context.ImportedProjects.Add(projectEntity);

                // ✅ BATCH ІМПОРТ - збираємо всі деталі для одного запиту
                var allPartsToAdd = new List<PartEntity>();
                var allProductsToAdd = new List<ProductEntity>();
                int addedParts = 0;
                int skippedParts = 0;

                // ✅ Завантажуємо існуючі ключі деталей одним запитом
                var existingPartKeys = await _context.Parts
                    .Where(p => p.ProjectExternalUuid == project.ProjectUuid)
                    .Select(p => new { p.ProjectExternalUuid, p.PartId, p.PartCounter })
                    .ToListAsync();

                var existingKeysSet = existingPartKeys
                    .Select(k => $"{k.ProjectExternalUuid}_{k.PartId}_{k.PartCounter}")
                    .ToHashSet();

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
                        CreatedDate = DateTime.UtcNow,
                        Counterparty = product.Counterparty,
                        CounterpartyOrderNumber = product.CounterpartyOrderNumber,
                        LdpColors = product.LdpColors
                    };

                    allProductsToAdd.Add(productEntity);

                    // ✅ Перевіряємо деталі через HashSet (O(1) замість O(n) запиту до БД)
                    foreach (var part in product.Parts)
                    {
                        var partKey = $"{part.ProjectExternalUuid}_{part.PartId}_{part.PartCounter}";

                        if (existingKeysSet.Contains(partKey))
                        {
                            skippedParts++;
                            continue;
                        }

                        var partEntity = MapPartToEntity(part);
                        allPartsToAdd.Add(partEntity);
                        existingKeysSet.Add(partKey); // Додаємо в set щоб уникнути дублікатів в межах імпорту
                        addedParts++;
                    }
                }

                // ✅ BATCH INSERT - додаємо всі записи разом
                if (allProductsToAdd.Any())
                {
                    _context.Products.AddRange(allProductsToAdd);
                }

                if (allPartsToAdd.Any())
                {
                    _context.Parts.AddRange(allPartsToAdd);
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
            return await ScanQRCodeAsync(qrCode, stage, null, null, null, userId, deviceId);
        }

        /// <summary>
        /// Сканування QR-коду з ID працівника та станції
        /// </summary>
        public async Task<ScanResult> ScanQRCodeAsync(
            string qrCode, 
            ProductionStage? stage,
            int? workerId,
            int? workstationId,
            int? sessionId,
            string? userId = null, 
            string? deviceId = null)
        {
            var result = new ScanResult { Success = false };

            try
            {
                // Парсинг QR-коду
                var parsedPart = Part.ParseQRCode(qrCode);
                if (parsedPart == null)
                {
                    result.Message = "Невірний формат QR-коду";
                    await LogScanAsync(0, qrCode, stage, false, result.Message, workerId, workstationId, sessionId, userId, deviceId);
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
                    // Не логуємо з PartId=0, використовуємо nullable
                    await LogScanAsync(null, qrCode, stage, false, result.Message, workerId, workstationId, sessionId, userId, deviceId);
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
                        await LogScanAsync(partEntity.Id, qrCode, null, true, result.Message, workerId, workstationId, sessionId, userId, deviceId);
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
                    
                    await LogScanAsync(partEntity.Id, qrCode, stage, false, result.Message, workerId, workstationId, sessionId, userId, deviceId);
                    return result;
                }

                // Завершуємо етап
                CompleteStageOnEntity(partEntity, stage.Value);
                
                // Створюємо лог запис в рамках тієї ж транзакції
                var log = new ScanLogEntity
                {
                    PartId = partEntity.Id,
                    QRCode = qrCode,
                    Stage = stage.HasValue ? (int)stage.Value : 0,
                    ScanDate = DateTime.UtcNow,
                    UserId = userId,
                    WorkerId = workerId,
                    WorkstationId = workstationId,
                    SessionId = sessionId,
                    DeviceId = deviceId,
                    Success = true,
                    Message = null // Буде оновлено після збереження
                };
                _context.ScanLogs.Add(log);
                
                // Зберігаємо всі зміни в одній транзакції
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

                // Оновлюємо повідомлення в логу
                log.Message = result.Message;
                await _context.SaveChangesAsync();

                return result;
            }
            catch (DbUpdateException dbEx)
            {
                var innerMessage = dbEx.InnerException?.Message ?? dbEx.Message;
                _logger.LogError($"Помилка збереження в БД: {innerMessage}", dbEx, "ScanningService");
                result.Message = $"Помилка збереження: {innerMessage}";
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
            await LogScanAsync(partId, qrCode, stage, success, message, null, null, null, userId, deviceId);
        }

        private async Task LogScanAsync(int partId, string qrCode, ProductionStage? stage, bool success, string? message, 
            int? workerId, int? workstationId, int? sessionId, string? userId, string? deviceId)
        {
            await LogScanAsync((int?)partId, qrCode, stage, success, message, workerId, workstationId, sessionId, userId, deviceId);
        }

        private async Task LogScanAsync(int? partId, string qrCode, ProductionStage? stage, bool success, string? message, 
            int? workerId, int? workstationId, int? sessionId, string? userId, string? deviceId)
        {
            try
            {
                var log = new ScanLogEntity
                {
                    PartId = partId ?? 0, // 0 якщо деталь не знайдена
                    QRCode = qrCode,
                    Stage = stage.HasValue ? (int)stage.Value : 0,
                    ScanDate = DateTime.UtcNow,
                    UserId = userId,
                    WorkerId = workerId,
                    WorkstationId = workstationId,
                    SessionId = sessionId,
                    DeviceId = deviceId,
                    Success = success,
                    Message = message
                };

                _context.ScanLogs.Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Логування помилки не повинно переривати основний процес
                _logger.LogError($"Помилка логування сканування: {ex.Message}", ex, "ScanningService");
            }
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
        /// Отримання статистики по проекту (оптимізовано SQL агрегацією + кеш)
        /// </summary>
        public async Task<ProjectStatistics?> GetProjectStatisticsAsync(string projectUuid)
        {
            // ✅ Спочатку перевіряємо кеш
            var cacheKey = $"{CACHE_KEY_PROJECT_STATS_PREFIX}{projectUuid}";
            if (_cache.TryGetValue(cacheKey, out ProjectStatistics? cachedStats))
            {
                return cachedStats;
            }

            // ✅ SQL агрегація - рахує на сервері БД
            var basicStats = await _context.Parts
                .Where(p => p.ProjectExternalUuid == projectUuid)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalParts = g.Count(),
                    CompletedParts = g.Count(p => p.IsPackingCompleted),
                    TotalSquareMeters = g.Sum(p => p.Length * p.Width / 1_000_000),
                    CompletedSquareMeters = g.Where(p => p.IsPackingCompleted).Sum(p => p.Length * p.Width / 1_000_000),
                    FileName = g.Max(p => p.SourceFileName) // Беремо будь-який файл
                })
                .FirstOrDefaultAsync();

            if (basicStats == null || basicStats.TotalParts == 0) return null;

            var stats = new ProjectStatistics
            {
                ProjectUuid = projectUuid,
                FileName = basicStats.FileName ?? "",
                TotalParts = basicStats.TotalParts,
                CompletedParts = basicStats.CompletedParts,
                TotalSquareMeters = basicStats.TotalSquareMeters,
                CompletedSquareMeters = basicStats.CompletedSquareMeters
            };

            // ✅ SQL агрегація для статистики по етапах
            var stageStats = await _context.Parts
                .Where(p => p.ProjectExternalUuid == projectUuid)
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    CuttingRequired = g.Count(p => p.RequiresCutting),
                    CuttingCompleted = g.Count(p => p.IsCutCompleted),
                    EdgeBandingRequired = g.Count(p => p.RequiresEdgeBanding),
                    EdgeBandingCompleted = g.Count(p => p.IsEdgeBandingCompleted),
                    DrillingRequired = g.Count(p => p.RequiresDrilling),
                    DrillingCompleted = g.Count(p => p.IsDrillingCompleted),
                    SortingRequired = g.Count(p => p.RequiresSorting),
                    SortingCompleted = g.Count(p => p.IsSortingCompleted),
                    PackingRequired = g.Count(p => p.RequiresPacking),
                    PackingCompleted = g.Count(p => p.IsPackingCompleted)
                })
                .FirstOrDefaultAsync();

            if (stageStats != null)
            {
                stats.StageStats[ProductionStage.Cutting] = new StageStatistics
                {
                    Stage = ProductionStage.Cutting,
                    TotalRequired = stageStats.CuttingRequired,
                    Completed = stageStats.CuttingCompleted
                };
                stats.StageStats[ProductionStage.EdgeBanding] = new StageStatistics
                {
                    Stage = ProductionStage.EdgeBanding,
                    TotalRequired = stageStats.EdgeBandingRequired,
                    Completed = stageStats.EdgeBandingCompleted
                };
                stats.StageStats[ProductionStage.Drilling] = new StageStatistics
                {
                    Stage = ProductionStage.Drilling,
                    TotalRequired = stageStats.DrillingRequired,
                    Completed = stageStats.DrillingCompleted
                };
                stats.StageStats[ProductionStage.Sorting] = new StageStatistics
                {
                    Stage = ProductionStage.Sorting,
                    TotalRequired = stageStats.SortingRequired,
                    Completed = stageStats.SortingCompleted
                };
                stats.StageStats[ProductionStage.Packing] = new StageStatistics
                {
                    Stage = ProductionStage.Packing,
                    TotalRequired = stageStats.PackingRequired,
                    Completed = stageStats.PackingCompleted
                };
            }

            // ✅ Зберігаємо в кеш
            _cache.Set(cacheKey, stats, StatsCacheExpiration);

            return stats;
        }

        /// <summary>
        /// Отримання загальної статистики по всіх проектах (оптимізовано SQL агрегацією + кеш)
        /// </summary>
        public async Task<ProjectStatistics> GetOverallStatisticsAsync()
        {
            // ✅ Спочатку перевіряємо кеш
            if (_cache.TryGetValue(CACHE_KEY_OVERALL_STATS, out ProjectStatistics? cachedStats) && cachedStats != null)
            {
                return cachedStats;
            }

            // ✅ SQL агрегація - рахує на сервері БД, не завантажує всі записи в пам'ять
            var basicStats = await _context.Parts
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalParts = g.Count(),
                    CompletedParts = g.Count(p => p.IsPackingCompleted),
                    TotalSquareMeters = g.Sum(p => p.Length * p.Width / 1_000_000),
                    CompletedSquareMeters = g.Where(p => p.IsPackingCompleted).Sum(p => p.Length * p.Width / 1_000_000)
                })
                .FirstOrDefaultAsync();

            var stats = new ProjectStatistics
            {
                ProjectUuid = "all",
                FileName = "Всі проекти",
                TotalParts = basicStats?.TotalParts ?? 0,
                CompletedParts = basicStats?.CompletedParts ?? 0,
                TotalSquareMeters = basicStats?.TotalSquareMeters ?? 0,
                CompletedSquareMeters = basicStats?.CompletedSquareMeters ?? 0
            };

            // ✅ SQL агрегація для статистики по етапах
            var stageStats = await _context.Parts
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    // Cutting
                    CuttingRequired = g.Count(p => p.RequiresCutting),
                    CuttingCompleted = g.Count(p => p.IsCutCompleted),
                    // EdgeBanding
                    EdgeBandingRequired = g.Count(p => p.RequiresEdgeBanding),
                    EdgeBandingCompleted = g.Count(p => p.IsEdgeBandingCompleted),
                    // Drilling
                    DrillingRequired = g.Count(p => p.RequiresDrilling),
                    DrillingCompleted = g.Count(p => p.IsDrillingCompleted),
                    // Sorting
                    SortingRequired = g.Count(p => p.RequiresSorting),
                    SortingCompleted = g.Count(p => p.IsSortingCompleted),
                    // Packing
                    PackingRequired = g.Count(p => p.RequiresPacking),
                    PackingCompleted = g.Count(p => p.IsPackingCompleted)
                })
                .FirstOrDefaultAsync();

            if (stageStats != null)
            {
                stats.StageStats[ProductionStage.Cutting] = new StageStatistics
                {
                    Stage = ProductionStage.Cutting,
                    TotalRequired = stageStats.CuttingRequired,
                    Completed = stageStats.CuttingCompleted
                };
                stats.StageStats[ProductionStage.EdgeBanding] = new StageStatistics
                {
                    Stage = ProductionStage.EdgeBanding,
                    TotalRequired = stageStats.EdgeBandingRequired,
                    Completed = stageStats.EdgeBandingCompleted
                };
                stats.StageStats[ProductionStage.Drilling] = new StageStatistics
                {
                    Stage = ProductionStage.Drilling,
                    TotalRequired = stageStats.DrillingRequired,
                    Completed = stageStats.DrillingCompleted
                };
                stats.StageStats[ProductionStage.Sorting] = new StageStatistics
                {
                    Stage = ProductionStage.Sorting,
                    TotalRequired = stageStats.SortingRequired,
                    Completed = stageStats.SortingCompleted
                };
                stats.StageStats[ProductionStage.Packing] = new StageStatistics
                {
                    Stage = ProductionStage.Packing,
                    TotalRequired = stageStats.PackingRequired,
                    Completed = stageStats.PackingCompleted
                };
            }

            // ✅ Зберігаємо в кеш
            _cache.Set(CACHE_KEY_OVERALL_STATS, stats, StatsCacheExpiration);

            return stats;
        }

        /// <summary>
        /// Інвалідація кешу статистики (викликати після сканування/імпорту)
        /// </summary>
        public void InvalidateStatsCache()
        {
            _cache.Remove(CACHE_KEY_OVERALL_STATS);
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
                ProductName = part.ProductName,
                Counterparty = part.Counterparty,
                CounterpartyOrderNumber = part.CounterpartyOrderNumber,
                LdpColors = part.LdpColors,
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
                ProductName = entity.ProductName,
                Counterparty = entity.Counterparty,
                CounterpartyOrderNumber = entity.CounterpartyOrderNumber,
                LdpColors = entity.LdpColors,
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

        #region Архівація та очищення

        /// <summary>
        /// Архівація/видалення старих ScanLogs для оптимізації продуктивності
        /// </summary>
        /// <param name="daysToKeep">Кількість днів для зберігання (за замовчуванням 30)</param>
        /// <returns>Кількість видалених записів</returns>
        public async Task<int> ArchiveOldScanLogsAsync(int daysToKeep = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

                // ✅ Використовуємо ExecuteDeleteAsync для ефективного видалення без завантаження в пам'ять
                var deletedCount = await _context.ScanLogs
                    .Where(s => s.ScanDate < cutoffDate)
                    .ExecuteDeleteAsync();

                if (deletedCount > 0)
                {
                    _logger.LogInfo($"Архівація ScanLogs: видалено {deletedCount} записів старше {daysToKeep} днів", "ScanningService");
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка архівації ScanLogs: {ex.Message}", ex, "ScanningService");
                return 0;
            }
        }

        /// <summary>
        /// Отримання статистики по ScanLogs для моніторингу
        /// </summary>
        public async Task<ScanLogsStatistics> GetScanLogsStatisticsAsync()
        {
            var stats = await _context.ScanLogs
                .GroupBy(_ => 1)
                .Select(g => new ScanLogsStatistics
                {
                    TotalRecords = g.Count(),
                    OldestRecord = g.Min(s => s.ScanDate),
                    NewestRecord = g.Max(s => s.ScanDate),
                    RecordsLast24Hours = g.Count(s => s.ScanDate >= DateTime.UtcNow.AddHours(-24)),
                    RecordsLast7Days = g.Count(s => s.ScanDate >= DateTime.UtcNow.AddDays(-7)),
                    RecordsOlderThan30Days = g.Count(s => s.ScanDate < DateTime.UtcNow.AddDays(-30))
                })
                .FirstOrDefaultAsync();

            return stats ?? new ScanLogsStatistics();
        }

        /// <summary>
        /// Очищення завершених деталей старше вказаної дати
        /// </summary>
        public async Task<int> CleanupCompletedPartsAsync(int daysToKeep = 90)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

                // Видаляємо тільки повністю завершені деталі
                var deletedCount = await _context.Parts
                    .Where(p => p.IsPackingCompleted && p.PackingCompletedDate < cutoffDate)
                    .ExecuteDeleteAsync();

                if (deletedCount > 0)
                {
                    _logger.LogInfo($"Очищення Parts: видалено {deletedCount} завершених деталей старше {daysToKeep} днів", "ScanningService");
                }

                return deletedCount;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка очищення Parts: {ex.Message}", ex, "ScanningService");
                return 0;
            }
        }

        #endregion
    }

    /// <summary>
    /// Статистика по ScanLogs для моніторингу
    /// </summary>
    public class ScanLogsStatistics
    {
        public int TotalRecords { get; set; }
        public DateTime? OldestRecord { get; set; }
        public DateTime? NewestRecord { get; set; }
        public int RecordsLast24Hours { get; set; }
        public int RecordsLast7Days { get; set; }
        public int RecordsOlderThan30Days { get; set; }

        public string EstimatedSize => $"~{TotalRecords * 200 / 1024 / 1024} MB"; // ~200 байт на запис
    }
}
