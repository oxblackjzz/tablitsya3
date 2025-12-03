namespace �������3.Models
{
    /// <summary>
    /// ����� � ������ ����
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// ��� ��䳿
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// г���� ��������� (Info, Warning, Error)
        /// </summary>
        public LogLevel Level { get; set; }

        /// <summary>
        /// ����������� ��� ����
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// ������� ��䳿 (����� ����������/������)
        /// </summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// ��������� ���������� (�����������)
        /// </summary>
        public string? Details { get; set; }

        /// <summary>
        /// Stack trace ��� �������
        /// </summary>
        public string? StackTrace { get; set; }
    }

    /// <summary>
    /// г�� ���������
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// ������������ �����������
        /// </summary>
        Info = 0,

        /// <summary>
        /// ������������
        /// </summary>
        Warning = 1,

        /// <summary>
        /// �������
        /// </summary>
        Error = 2,

        /// <summary>
        /// ³����������� (��� ����������)
        /// </summary>
        Debug = 3
    }
}
