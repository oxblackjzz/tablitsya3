using System;
using System.Collections.Generic;

namespace �������3.Models
{
    public class WorkshopData
    {
        public Dictionary<int, List<double>> WorkshopOrders { get; set; } = new();
        public Dictionary<int, List<DateTime>> WorkshopOrderDates { get; set; } = new();
        public Dictionary<int, List<string>> WorkshopOrderNames { get; set; } = new();
        public Dictionary<string, DateTime> CustomCompletionDates { get; set; } = new();
        public Dictionary<int, int> WorkshopCapacities { get; set; } = new()
        {
            { 1, 1000 },
            { 3, 1000 },
            { 6, 1000 }
        };
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        public DateTime StartDate { get; set; } = new DateTime(2025, 1, 1);
        [Obsolete("�������������� WorkshopCapacities ��� ������������ ��������� ������� ����")]
        public int DailyCapacity { get; set; } = 1000;
        public int ProductionLeadTime { get; set; } = 5;
        public int DaysBeforeProduction { get; set; } = 1; // �̲����: �� ������������� 1 ���� ������ 2
    }
}
