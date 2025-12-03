using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Tablitsya3.Models;
using AppLogLevel = Tablitsya3.Models.LogLevel;

namespace Tablitsya3.Services
{
    public class LoggingService
    {
        private readonly List<LogEntry> _logs = new();
        private const int MaxLogEntries = 1000;

        public event Action? OnLogAdded;

        public void LogInfo(string message, string source = "System")
        {
            AddLog(AppLogLevel.Info, message, source);
        }

        public void LogWarning(string message, string source = "System", string? details = null)
        {
            AddLog(AppLogLevel.Warning, message, source, details);
        }

        public void LogError(string message, Exception? ex = null, string source = "System")
        {
            var fullMessage = ex != null ? $"{message}: {ex.Message}" : message;
            var stackTrace = ex?.StackTrace;
            AddLog(AppLogLevel.Error, fullMessage, source, ex?.ToString(), stackTrace);
        }

        public void LogDebug(string message, string source = "System", string? details = null)
        {
            AddLog(AppLogLevel.Debug, message, source, details);
        }

        private void AddLog(AppLogLevel level, string message, string source, string? details = null, string? stackTrace = null)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Level = level,
                Message = message,
                Source = source,
                Details = details,
                StackTrace = stackTrace
            };

            _logs.Add(entry);

            if (_logs.Count > MaxLogEntries)
            {
                _logs.RemoveAt(0);
            }

            Console.WriteLine($"[{entry.Timestamp:HH:mm:ss}] [{entry.Level}] [{entry.Source}] {entry.Message}");

            OnLogAdded?.Invoke();
        }

        public List<LogEntry> GetLogs()
        {
            return _logs.ToList();
        }

        public List<LogEntry> GetLogs(AppLogLevel? level = null, string? source = null)
        {
            var query = _logs.AsEnumerable();

            if (level.HasValue)
            {
                query = query.Where(l => l.Level == level.Value);
            }

            if (!string.IsNullOrEmpty(source))
            {
                query = query.Where(l => l.Source.Contains(source, StringComparison.OrdinalIgnoreCase));
            }

            return query.OrderByDescending(l => l.Timestamp).ToList();
        }

        public void ClearLogs()
        {
            _logs.Clear();
            LogInfo("Logs cleared", "LoggingService");
            OnLogAdded?.Invoke();
        }

        public Dictionary<AppLogLevel, int> GetLogCounts()
        {
            return _logs.GroupBy(l => l.Level)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public string ExportLogsAsText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Log Export ===");
            sb.AppendLine($"Generated: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine($"Total entries: {_logs.Count}");
            sb.AppendLine();

            foreach (var log in _logs.OrderByDescending(l => l.Timestamp))
            {
                sb.AppendLine($"[{log.Timestamp:dd.MM.yyyy HH:mm:ss}] [{log.Level}] [{log.Source}]");
                sb.AppendLine($"  {log.Message}");

                if (!string.IsNullOrEmpty(log.Details))
                {
                    sb.AppendLine($"  Details: {log.Details}");
                }

                if (!string.IsNullOrEmpty(log.StackTrace))
                {
                    sb.AppendLine($"  Stack Trace:");
                    sb.AppendLine($"  {log.StackTrace}");
                }

                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
