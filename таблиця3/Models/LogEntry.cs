namespace таблиця3.Models
{
    /// <summary>
    /// Запис в журналі подій
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Час події
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Рівень логування (Info, Warning, Error)
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// Повідомлення про подію
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Джерело події (назва компонента/сервісу)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// Додаткова інформація (опціонально)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Stack trace для помилок
        /// </summary>
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// Рівні логування
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Інформаційне повідомлення
        /// </summary>
        Info = 0,

        /// <summary>
        /// Попередження
        /// </summary>
        Warning = 1,

        /// <summary>
        /// Помилка
        /// </summary>
        Error = 2,

        /// <summary>
        /// Відлагодження (для розробників)
        /// </summary>
        Debug = 3
    }
}
