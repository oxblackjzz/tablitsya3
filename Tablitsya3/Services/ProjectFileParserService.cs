using System.Globalization;
using System.Xml.Linq;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для парсингу .project XML файлів
    /// </summary>
    public class ProjectFileParserService
    {
        private readonly LoggingService _logger;

        public ProjectFileParserService(LoggingService logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Парсинг XML контенту .project файлу
        /// </summary>
        public ImportedProject ParseProjectFile(string xmlContent, string fileName = "")
        {
            var project = new ImportedProject
            {
                FileName = fileName,
                ImportedDate = DateTime.UtcNow
            };

            try
            {
                if (string.IsNullOrWhiteSpace(xmlContent))
                {
                    throw new ArgumentException("XML файл порожній");
                }

                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                if (root == null || root.Name.LocalName != "project")
                {
                    throw new Exception("XML не містить кореневого елементу 'project'");
                }

                // Метадані проекту - шукаємо UUID в різних атрибутах
                project.ProjectUuid = root.Attribute("project.externaluuid")?.Value 
                    ?? root.Attribute("project.uuid")?.Value 
                    ?? root.Attribute("externaluuid")?.Value
                    ?? root.Attribute("externalUuid")?.Value
                    ?? root.Attribute("uuid")?.Value
                    ?? Guid.NewGuid().ToString();
                    
                project.Version = root.Attribute("version")?.Value ?? "";
                project.Currency = root.Attribute("currency")?.Value ?? "грн";
                project.TotalCost = ParseDecimal(root.Attribute("cost")?.Value);
                project.MaterialCost = ParseDecimal(root.Attribute("costMaterial")?.Value);
                project.OperationCost = ParseDecimal(root.Attribute("costOperation")?.Value);

                _logger.LogInfo($"Парсинг проекту: UUID={project.ProjectUuid}, File={fileName}", "ProjectParser");

                // Парсинг товарів (goods)
                var partCounters = new Dictionary<int, int>();
                var goods = root.Elements("good").Where(g => g.Attribute("typeId")?.Value == "product").ToList();

                foreach (var good in goods)
                {
                    var product = ParseProduct(good, project.ProjectUuid, fileName, partCounters);
                    if (product != null)
                    {
                        project.Products.Add(product);
                    }
                }

                // Статистика
                project.ProductsCount = project.Products.Count;
                project.PartsCount = project.Products.Sum(p => p.Parts.Count);
                project.TotalSquareMeters = project.Products.Sum(p => p.TotalSquareMeters);

                _logger.LogInfo($"Парсинг завершено: {project.ProductsCount} товарів, {project.PartsCount} деталей, {project.TotalSquareMeters:F2} м²", "ProjectParser");

                return project;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Помилка парсингу проекту: {ex.Message}", ex, "ProjectParser");
                throw;
            }
        }

        /// <summary>
        /// Парсинг товару (good)
        /// </summary>
        private ImportedProduct? ParseProduct(XElement goodElement, string projectUuid, string fileName, Dictionary<int, int> partCounters)
        {
            try
            {
                var product = new ImportedProduct
                {
                    ProductId = ParseInt(goodElement.Attribute("id")?.Value),
                    Name = goodElement.Attribute("name")?.Value ?? "",
                    Code = goodElement.Attribute("code")?.Value ?? "",
                    Description = goodElement.Attribute("description")?.Value ?? "",
                    Count = Math.Max(1, ParseInt(goodElement.Attribute("count")?.Value)),
                    Cost = ParseDecimal(goodElement.Attribute("cost")?.Value),
                    MaterialCost = ParseDecimal(goodElement.Attribute("costMaterial")?.Value),
                    OperationCost = ParseDecimal(goodElement.Attribute("costOperation")?.Value),
                    OrderDate = ParseProjectDate(goodElement.Attribute("orderDate")?.Value)
                };

                // Парсинг деталей
                var partElements = goodElement.Elements("part").ToList();

                // Для кожного екземпляру товару (count)
                for (int productIndex = 1; productIndex <= product.Count; productIndex++)
                {
                    foreach (var partElement in partElements)
                    {
                        var parts = ParsePartElement(partElement, projectUuid, fileName, product.Name, partCounters);
                        product.Parts.AddRange(parts);
                    }
                }

                return product.Parts.Any() ? product : null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Помилка парсингу товару: {ex.Message}", "ProjectParser");
                return null;
            }
        }

        /// <summary>
        /// Парсинг елементу деталі
        /// </summary>
        private List<Part> ParsePartElement(XElement partElement, string projectUuid, string fileName, string orderName, Dictionary<int, int> partCounters)
        {
            var parts = new List<Part>();

            try
            {
                var idStr = partElement.Attribute("id")?.Value;
                if (string.IsNullOrEmpty(idStr) || !int.TryParse(idStr, out int partId))
                {
                    return parts;
                }

                var name = partElement.Attribute("name")?.Value ?? "";
                var code = partElement.Attribute("part.code")?.Value ?? "";

                // Фільтрація порожніх або службових деталей
                if (string.IsNullOrWhiteSpace(name) ||
                    name.Contains("Ящик") ||
                    ParseDouble(partElement.Attribute("l")?.Value) == 0 ||
                    ParseDouble(partElement.Attribute("w")?.Value) == 0)
                {
                    return parts;
                }

                // Кількість деталей
                int count = Math.Max(1, ParseInt(partElement.Attribute("count")?.Value));
                int usedCount = ParseInt(partElement.Attribute("usedCount")?.Value);
                int actualCount = Math.Max(count, usedCount);

                // Розміри
                double length = ParseDouble(partElement.Attribute("l")?.Value);
                if (length == 0) length = ParseDouble(partElement.Attribute("dl")?.Value);
                
                double width = ParseDouble(partElement.Attribute("w")?.Value);
                if (width == 0) width = ParseDouble(partElement.Attribute("dw")?.Value);

                // Кількість сторін для обклейки кромкою
                int edgeBandingSides = CountEdgeBandingSides(partElement);

                // Матеріал
                string material = DetermineMaterial(name, partElement);

                // Чи потрібен текстовий маркер (txt)
                bool hasTxt = partElement.Attribute("txt")?.Value?.ToLower() == "true";

                // Створюємо actualCount деталей
                for (int i = 1; i <= actualCount; i++)
                {
                    // Генеруємо унікальний counter
                    if (!partCounters.ContainsKey(partId))
                    {
                        partCounters[partId] = 0;
                    }
                    partCounters[partId]++;

                    var part = new Part
                    {
                        ProjectExternalUuid = projectUuid,
                        PartId = partId,
                        PartCounter = partCounters[partId],
                        Name = name,
                        Code = code,
                        Length = length,
                        Width = width,
                        Thickness = 16, // За замовчуванням
                        Material = material,
                        Quantity = 1,
                        CreatedDate = DateTime.UtcNow,
                        SourceFileName = fileName,
                        OrderName = orderName,
                        EdgeBandingSidesRequired = edgeBandingSides
                    };

                    // Встановлюємо вимоги до етапів
                    SetStageRequirements(part, hasTxt);

                    parts.Add(part);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Помилка парсингу деталі: {ex.Message}", "ProjectParser");
            }

            return parts;
        }

        /// <summary>
        /// Підрахунок сторін для обклейки кромкою
        /// </summary>
        private int CountEdgeBandingSides(XElement partElement)
        {
            int count = 0;
            
            // Атрибути обклейки: elt (top), elb (bottom), ell (left), elr (right)
            if (!string.IsNullOrEmpty(partElement.Attribute("elt")?.Value)) count++;
            if (!string.IsNullOrEmpty(partElement.Attribute("elb")?.Value)) count++;
            if (!string.IsNullOrEmpty(partElement.Attribute("ell")?.Value)) count++;
            if (!string.IsNullOrEmpty(partElement.Attribute("elr")?.Value)) count++;

            return count;
        }

        /// <summary>
        /// Визначення матеріалу за назвою
        /// </summary>
        private string DetermineMaterial(string name, XElement partElement)
        {
            var lowerName = name.ToLower();
            
            if (lowerName.Contains("двп") || lowerName.Contains("хдф"))
                return "ХДФ";
            if (lowerName.Contains("дзеркало"))
                return "Дзеркало";
            if (lowerName.Contains("профиль") || lowerName.Contains("профіль"))
                return "Профіль";
            if (lowerName.Contains("скло"))
                return "Скло";
            
            return "ЛДСП";
        }

        /// <summary>
        /// Встановлення вимог до етапів
        /// </summary>
        private void SetStageRequirements(Part part, bool hasTxt)
        {
            var lowerName = part.Name.ToLower();
            var lowerMaterial = part.Material.ToLower();

            // Порізка - потрібна завжди
            part.RequiresCutting = true;

            // Обклейка кромкою - залежить від XML
            part.RequiresEdgeBanding = part.EdgeBandingSidesRequired > 0;

            // Свердління - визначаємо за типом деталі
            part.RequiresDrilling = DetermineDrillingRequired(part, lowerName, lowerMaterial);

            // Сортування та пакування - завжди
            part.RequiresSorting = true;
            part.RequiresPacking = true;
        }

        /// <summary>
        /// Визначення чи потрібне свердління
        /// </summary>
        private bool DetermineDrillingRequired(Part part, string lowerName, string lowerMaterial)
        {
            // Не потребують свердління:
            // - Задні стінки (ДВП, ХДФ)
            // - Дзеркала
            // - Профілі
            // - Дуже маленькі деталі
            
            if (lowerName.Contains("задня стенка") ||
                lowerName.Contains("двп") ||
                lowerName.Contains("хдф") ||
                lowerName.Contains("дзеркало") ||
                lowerName.Contains("профиль") ||
                lowerName.Contains("профіль") ||
                lowerMaterial.Contains("хдф") ||
                lowerMaterial.Contains("дзеркало"))
            {
                return false;
            }

            // Маленькі деталі
            if (part.Length < 100 || part.Width < 100)
            {
                return false;
            }

            // За замовчуванням - потрібне
            return true;
        }

        /// <summary>
        /// Парсинг дати з формату проекту (дММyyyyHHmmss)
        /// </summary>
        private DateTime? ParseProjectDate(string? dateStr)
        {
            if (string.IsNullOrEmpty(dateStr)) return null;
            
            try
            {
                // Формат: dMMyyyyHHmmss (наприклад: 2092025112249)
                if (dateStr.Length >= 13)
                {
                    var day = int.Parse(dateStr.Substring(0, 1));
                    var month = int.Parse(dateStr.Substring(1, 2));
                    var year = int.Parse(dateStr.Substring(3, 4));
                    var hour = int.Parse(dateStr.Substring(7, 2));
                    var minute = int.Parse(dateStr.Substring(9, 2));
                    var second = int.Parse(dateStr.Substring(11, 2));
                    
                    return new DateTime(year, month, day, hour, minute, second);
                }
            }
            catch
            {
                // Ігноруємо помилки парсингу дати
            }
            
            return null;
        }

        private double ParseDouble(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
                return result;
            return 0;
        }

        private int ParseInt(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (int.TryParse(value, out int result))
                return result;
            return 0;
        }

        private decimal ParseDecimal(string? value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
                return result;
            return 0;
        }

        /// <summary>
        /// Валідація XML структури
        /// </summary>
        public bool ValidateProjectXml(string xmlContent)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(xmlContent))
                    return false;

                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                if (root?.Name.LocalName != "project")
                    return false;

                // Перевіряємо наявність goods
                var hasGoods = root.Elements("good").Any(g => g.Attribute("typeId")?.Value == "product");
                
                return hasGoods;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Отримання короткої інформації про проект без повного парсингу
        /// </summary>
        public (string projectUuid, int goodsCount, decimal cost)? GetProjectInfo(string xmlContent)
        {
            try
            {
                var doc = XDocument.Parse(xmlContent);
                var root = doc.Root;

                if (root?.Name.LocalName != "project")
                    return null;

                var projectUuid = root.Attribute("project.uuid")?.Value 
                    ?? root.Attribute("project.externaluuid")?.Value 
                    ?? "";
                var cost = ParseDecimal(root.Attribute("cost")?.Value);
                var goodsCount = root.Elements("good")
                    .Where(g => g.Attribute("typeId")?.Value == "product")
                    .Count();

                return (projectUuid, goodsCount, cost);
            }
            catch
            {
                return null;
            }
        }
    }
}
