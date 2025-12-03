using System.Collections.Generic;

namespace �������3.Models
{
    public class ProductionSchedule
    {
        public List<Order> Orders { get; set; } = new();
        public Dictionary<int, double> DailyLoad { get; set; } = new();
        public int DailyCapacity { get; set; } = 1000;
        public int ProductionLeadTime { get; set; } = 5;
    }
}
