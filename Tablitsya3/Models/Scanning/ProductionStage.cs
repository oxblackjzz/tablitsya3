namespace Tablitsya3.Models.Scanning
{
    /// <summary>
    /// Етапи виробництва деталей
    /// </summary>
    public enum ProductionStage
    {
        /// <summary>Порізка</summary>
        Cutting = 1,
        
        /// <summary>Поклейка кромки</summary>
        EdgeBanding = 2,
        
        /// <summary>Свердління</summary>
        Drilling = 3,
        
        /// <summary>Сортування</summary>
        Sorting = 4,
        
        /// <summary>Пакування</summary>
        Packing = 5
    }

    public static class ProductionStageExtensions
    {
        public static string GetDisplayName(this ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => "Порізка",
                ProductionStage.EdgeBanding => "Поклейка",
                ProductionStage.Drilling => "Свердління",
                ProductionStage.Sorting => "Сортування",
                ProductionStage.Packing => "Пакування",
                _ => "Невідомо"
            };
        }

        public static string GetIcon(this ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => "bi-scissors",
                ProductionStage.EdgeBanding => "bi-bounding-box",
                ProductionStage.Drilling => "bi-gear",
                ProductionStage.Sorting => "bi-sort-alpha-down",
                ProductionStage.Packing => "bi-box-seam",
                _ => "bi-question"
            };
        }

        public static string GetColor(this ProductionStage stage)
        {
            return stage switch
            {
                ProductionStage.Cutting => "#e74c3c",
                ProductionStage.EdgeBanding => "#f39c12",
                ProductionStage.Drilling => "#3498db",
                ProductionStage.Sorting => "#9b59b6",
                ProductionStage.Packing => "#27ae60",
                _ => "#95a5a6"
            };
        }
    }
}
