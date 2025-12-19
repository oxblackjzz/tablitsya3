using System.Text.Json.Serialization;

namespace Tablitsya3.Models.Scanning
{
    /// <summary>
    /// Модель деталі для сканування
    /// </summary>
    public class Part
    {
        public int Id { get; set; }
        
        /// <summary>UUID проекту з .project файлу</summary>
        public string ProjectExternalUuid { get; set; } = string.Empty;
        
        /// <summary>ID деталі з XML</summary>
        public int PartId { get; set; }
        
        /// <summary>Лічильник для унікальності (якщо деталей з однаковим ID декілька)</summary>
        public int PartCounter { get; set; }
        
        /// <summary>Назва деталі</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Код деталі (part.code)</summary>
        public string Code { get; set; } = string.Empty;
        
        /// <summary>Довжина в мм</summary>
        public double Length { get; set; }
        
        /// <summary>Ширина в мм</summary>
        public double Width { get; set; }
        
        /// <summary>Товщина в мм</summary>
        public double Thickness { get; set; } = 16;
        
        /// <summary>Матеріал</summary>
        public string Material { get; set; } = string.Empty;
        
        /// <summary>Кількість</summary>
        public int Quantity { get; set; } = 1;
        
        /// <summary>Дата створення</summary>
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        
        /// <summary>Назва файлу джерела</summary>
        public string SourceFileName { get; set; } = string.Empty;
        
        /// <summary>Назва замовлення (good name)</summary>
        public string OrderName { get; set; } = string.Empty;

        // === Статуси етапів ===
        
        public bool IsCutCompleted { get; set; }
        public DateTime? CutCompletedDate { get; set; }
        
        public bool IsEdgeBandingCompleted { get; set; }
        public DateTime? EdgeBandingCompletedDate { get; set; }
        
        public bool IsDrillingCompleted { get; set; }
        public DateTime? DrillingCompletedDate { get; set; }
        
        public bool IsSortingCompleted { get; set; }
        public DateTime? SortingCompletedDate { get; set; }
        
        public bool IsPackingCompleted { get; set; }
        public DateTime? PackingCompletedDate { get; set; }

        // === Чи потрібні етапи ===
        
        public bool RequiresCutting { get; set; } = true;
        public bool RequiresEdgeBanding { get; set; } = true;
        public bool RequiresDrilling { get; set; } = true;
        public bool RequiresSorting { get; set; } = true;
        public bool RequiresPacking { get; set; } = true;

        // === Для поклейки кромки (може бути декілька сторін) ===
        
        /// <summary>Кількість сторін що потребують обклейки</summary>
        public int EdgeBandingSidesRequired { get; set; } = 0;
        
        /// <summary>Кількість сторін вже обклеєних</summary>
        public int EdgeBandingSidesCompleted { get; set; } = 0;
        
        /// <summary>Дати завершення кожної сторони (розділені ;)</summary>
        public string EdgeBandingCompletedDates { get; set; } = string.Empty;

        // === Обчислювані властивості ===

        /// <summary>Площа деталі в м²</summary>
        [JsonIgnore]
        public double SquareMeters => (Length * Width) / 1_000_000;

        /// <summary>Розміри у форматі LxW</summary>
        [JsonIgnore]
        public string Dimensions => $"{Length:F0}x{Width:F0}";

        /// <summary>QR-код деталі</summary>
        [JsonIgnore]
        public string QRCode => $"{ProjectExternalUuid}/{PartId}/{PartCounter}";

        /// <summary>Чи деталь повністю завершена</summary>
        [JsonIgnore]
        public bool IsFullyCompleted =>
            (!RequiresCutting || IsCutCompleted) &&
            (!RequiresEdgeBanding || IsEdgeBandingFullyCompleted()) &&
            (!RequiresDrilling || IsDrillingCompleted) &&
            (!RequiresSorting || IsSortingCompleted) &&
            (!RequiresPacking || IsPackingCompleted);

        /// <summary>Кількість завершених етапів</summary>
        [JsonIgnore]
        public int CompletedStagesCount
        {
            get
            {
                int count = 0;
                if (RequiresCutting && IsCutCompleted) count++;
                if (RequiresEdgeBanding && IsEdgeBandingFullyCompleted()) count++;
                if (RequiresDrilling && IsDrillingCompleted) count++;
                if (RequiresSorting && IsSortingCompleted) count++;
                if (RequiresPacking && IsPackingCompleted) count++;
                return count;
            }
        }

        /// <summary>Загальна кількість потрібних етапів</summary>
        [JsonIgnore]
        public int TotalRequiredStages
        {
            get
            {
                int count = 0;
                if (RequiresCutting) count++;
                if (RequiresEdgeBanding) count++;
                if (RequiresDrilling) count++;
                if (RequiresSorting) count++;
                if (RequiresPacking) count++;
                return count;
            }
        }

        /// <summary>Прогрес виконання (0-100%)</summary>
        [JsonIgnore]
        public int ProgressPercent => TotalRequiredStages > 0 
            ? (int)Math.Round((double)CompletedStagesCount / TotalRequiredStages * 100) 
            : 100;

        /// <summary>Поточний етап</summary>
        [JsonIgnore]
        public ProductionStage? CurrentStage
        {
            get
            {
                if (RequiresCutting && !IsCutCompleted) return ProductionStage.Cutting;
                if (RequiresEdgeBanding && !IsEdgeBandingFullyCompleted()) return ProductionStage.EdgeBanding;
                if (RequiresDrilling && !IsDrillingCompleted) return ProductionStage.Drilling;
                if (RequiresSorting && !IsSortingCompleted) return ProductionStage.Sorting;
                if (RequiresPacking && !IsPackingCompleted) return ProductionStage.Packing;
                return null;
            }
        }

        // === Методи ===

        public bool IsEdgeBandingFullyCompleted()
        {
            if (!RequiresEdgeBanding) return true;
            if (EdgeBandingSidesRequired <= 0) return IsEdgeBandingCompleted;
            return EdgeBandingSidesCompleted >= EdgeBandingSidesRequired;
        }

        public bool CanPerformEdgeBandingScan()
        {
            if (!RequiresEdgeBanding) return false;
            if (EdgeBandingSidesRequired <= 0) return !IsEdgeBandingCompleted;
            return EdgeBandingSidesCompleted < EdgeBandingSidesRequired;
        }

        public string GetEdgeBandingProgress()
        {
            if (!RequiresEdgeBanding) return "N/A";
            if (EdgeBandingSidesRequired <= 0) return IsEdgeBandingCompleted ? "✓" : "○";
            return $"{EdgeBandingSidesCompleted}/{EdgeBandingSidesRequired}";
        }

        public bool IsStageRequired(ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => RequiresCutting,
                ProductionStage.EdgeBanding => RequiresEdgeBanding,
                ProductionStage.Drilling => RequiresDrilling,
                ProductionStage.Sorting => RequiresSorting,
                ProductionStage.Packing => RequiresPacking,
                _ => false
            };
        }

        public bool IsStageCompleted(ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => IsCutCompleted,
                ProductionStage.EdgeBanding => IsEdgeBandingFullyCompleted(),
                ProductionStage.Drilling => IsDrillingCompleted,
                ProductionStage.Sorting => IsSortingCompleted,
                ProductionStage.Packing => IsPackingCompleted,
                _ => false
            };
        }

        public bool CanAdvanceToStage(ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => RequiresCutting && !IsCutCompleted,
                ProductionStage.EdgeBanding => RequiresEdgeBanding && CanPerformEdgeBandingScan() && 
                                             (!RequiresCutting || IsCutCompleted),
                ProductionStage.Drilling => RequiresDrilling && !IsDrillingCompleted && 
                                          (!RequiresCutting || IsCutCompleted) && 
                                          (!RequiresEdgeBanding || IsEdgeBandingFullyCompleted()),
                ProductionStage.Sorting => RequiresSorting && !IsSortingCompleted && 
                                         (!RequiresCutting || IsCutCompleted) && 
                                         (!RequiresEdgeBanding || IsEdgeBandingFullyCompleted()) && 
                                         (!RequiresDrilling || IsDrillingCompleted),
                ProductionStage.Packing => RequiresPacking && !IsPackingCompleted && 
                                         (!RequiresCutting || IsCutCompleted) && 
                                         (!RequiresEdgeBanding || IsEdgeBandingFullyCompleted()) && 
                                         (!RequiresDrilling || IsDrillingCompleted) && 
                                         (!RequiresSorting || IsSortingCompleted),
                _ => false
            };
        }

        public string GetStageStatus(ProductionStage stage)
        {
            if (!IsStageRequired(stage)) return "N/A";
            
            if (stage == ProductionStage.EdgeBanding)
            {
                if (EdgeBandingSidesRequired > 0)
                {
                    return $"{EdgeBandingSidesCompleted}/{EdgeBandingSidesRequired}";
                }
            }
            
            return IsStageCompleted(stage) ? "✓" : "○";
        }

        public void CompleteStage(ProductionStage stage)
        {
            var now = DateTime.UtcNow;
            
            switch (stage)
            {
                case ProductionStage.Cutting:
                    IsCutCompleted = true;
                    CutCompletedDate = now;
                    break;
                    
                case ProductionStage.EdgeBanding:
                    if (EdgeBandingSidesRequired > 0)
                    {
                        EdgeBandingSidesCompleted++;
                        var dates = string.IsNullOrEmpty(EdgeBandingCompletedDates) 
                            ? new List<string>() 
                            : EdgeBandingCompletedDates.Split(';').ToList();
                        dates.Add(now.ToString("yyyy-MM-dd HH:mm:ss"));
                        EdgeBandingCompletedDates = string.Join(";", dates);
                        
                        if (EdgeBandingSidesCompleted >= EdgeBandingSidesRequired)
                        {
                            IsEdgeBandingCompleted = true;
                            EdgeBandingCompletedDate = now;
                        }
                    }
                    else
                    {
                        IsEdgeBandingCompleted = true;
                        EdgeBandingCompletedDate = now;
                    }
                    break;
                    
                case ProductionStage.Drilling:
                    IsDrillingCompleted = true;
                    DrillingCompletedDate = now;
                    break;
                    
                case ProductionStage.Sorting:
                    IsSortingCompleted = true;
                    SortingCompletedDate = now;
                    break;
                    
                case ProductionStage.Packing:
                    IsPackingCompleted = true;
                    PackingCompletedDate = now;
                    break;
            }
        }

        /// <summary>Парсинг QR-коду</summary>
        public static Part? ParseQRCode(string qrCode)
        {
            if (string.IsNullOrWhiteSpace(qrCode)) return null;
            
            var parts = qrCode.Split('/');
            if (parts.Length < 3) return null;
            
            if (!int.TryParse(parts[1], out int partId)) return null;
            if (!int.TryParse(parts[2], out int partCounter)) return null;
            
            return new Part
            {
                ProjectExternalUuid = parts[0],
                PartId = partId,
                PartCounter = partCounter
            };
        }
    }
}
