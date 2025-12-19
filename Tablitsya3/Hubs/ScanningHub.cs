using Microsoft.AspNetCore.SignalR;
using Tablitsya3.Models.Scanning;

namespace Tablitsya3.Hubs
{
    /// <summary>
    /// SignalR Hub для real-time оновлень сканування
    /// </summary>
    public class ScanningHub : Hub
    {
        /// <summary>
        /// Підключення до групи проекту
        /// </summary>
        public async Task JoinProject(string projectUuid)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"project_{projectUuid}");
        }

        /// <summary>
        /// Відключення від групи проекту
        /// </summary>
        public async Task LeaveProject(string projectUuid)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project_{projectUuid}");
        }

        /// <summary>
        /// Підключення до загальної групи
        /// </summary>
        public async Task JoinAll()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "all_scanning");
        }

        /// <summary>
        /// Відключення від загальної групи
        /// </summary>
        public async Task LeaveAll()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_scanning");
        }
    }

    /// <summary>
    /// Сервіс для надсилання SignalR повідомлень
    /// </summary>
    public class ScanningHubService
    {
        private readonly IHubContext<ScanningHub> _hubContext;

        public ScanningHubService(IHubContext<ScanningHub> hubContext)
        {
            _hubContext = hubContext;
        }

        /// <summary>
        /// Сповіщення про завершення етапу
        /// </summary>
        public async Task NotifyStageCompletedAsync(string projectUuid, Part part, ProductionStage stage)
        {
            var message = new ScanNotification
            {
                Type = "StageCompleted",
                ProjectUuid = projectUuid,
                PartId = part.Id,
                QRCode = part.QRCode,
                PartName = part.Name,
                Stage = stage,
                StageName = stage.GetDisplayName(),
                ProgressPercent = part.ProgressPercent,
                IsFullyCompleted = part.IsFullyCompleted,
                Timestamp = DateTime.UtcNow
            };

            // Надсилаємо в групу проекту
            await _hubContext.Clients.Group($"project_{projectUuid}")
                .SendAsync("StageCompleted", message);

            // Надсилаємо в загальну групу
            await _hubContext.Clients.Group("all_scanning")
                .SendAsync("StageCompleted", message);
        }

        /// <summary>
        /// Сповіщення про повне завершення деталі
        /// </summary>
        public async Task NotifyPartCompletedAsync(string projectUuid, Part part)
        {
            var message = new ScanNotification
            {
                Type = "PartCompleted",
                ProjectUuid = projectUuid,
                PartId = part.Id,
                QRCode = part.QRCode,
                PartName = part.Name,
                ProgressPercent = 100,
                IsFullyCompleted = true,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group($"project_{projectUuid}")
                .SendAsync("PartCompleted", message);

            await _hubContext.Clients.Group("all_scanning")
                .SendAsync("PartCompleted", message);
        }

        /// <summary>
        /// Сповіщення про імпорт проекту
        /// </summary>
        public async Task NotifyProjectImportedAsync(string projectUuid, string fileName, int partsCount)
        {
            var message = new
            {
                Type = "ProjectImported",
                ProjectUuid = projectUuid,
                FileName = fileName,
                PartsCount = partsCount,
                Timestamp = DateTime.UtcNow
            };

            await _hubContext.Clients.Group("all_scanning")
                .SendAsync("ProjectImported", message);
        }

        /// <summary>
        /// Оновлення статистики проекту
        /// </summary>
        public async Task NotifyStatisticsUpdatedAsync(string projectUuid, ProjectStatistics statistics)
        {
            await _hubContext.Clients.Group($"project_{projectUuid}")
                .SendAsync("StatisticsUpdated", statistics);

            await _hubContext.Clients.Group("all_scanning")
                .SendAsync("StatisticsUpdated", statistics);
        }
    }

    /// <summary>
    /// Модель сповіщення про сканування
    /// </summary>
    public class ScanNotification
    {
        public string Type { get; set; } = string.Empty;
        public string ProjectUuid { get; set; } = string.Empty;
        public int PartId { get; set; }
        public string QRCode { get; set; } = string.Empty;
        public string PartName { get; set; } = string.Empty;
        public ProductionStage? Stage { get; set; }
        public string? StageName { get; set; }
        public int ProgressPercent { get; set; }
        public bool IsFullyCompleted { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
