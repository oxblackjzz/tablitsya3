using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Tablitsya3.Models;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для Undo/Redo операцій
    /// </summary>
    public class UndoRedoService
    {
        private readonly ILogger<UndoRedoService> _logger;
        private readonly Stack<WorkshopDataSnapshot> _undoStack = new();
        private readonly Stack<WorkshopDataSnapshot> _redoStack = new();
        private const int MaxHistorySize = 50;
        
        public UndoRedoService(ILogger<UndoRedoService> logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// Чи можна виконати Undo
        /// </summary>
        public bool CanUndo => _undoStack.Count > 0;
        
        /// <summary>
        /// Чи можна виконати Redo
        /// </summary>
        public bool CanRedo => _redoStack.Count > 0;
        
        /// <summary>
        /// Кількість операцій в історії Undo
        /// </summary>
        public int UndoCount => _undoStack.Count;
        
        /// <summary>
        /// Кількість операцій в історії Redo
        /// </summary>
        public int RedoCount => _redoStack.Count;
        
        /// <summary>
        /// Подія при зміні стану історії
        /// </summary>
        public event Action? OnStateChanged;
        
        /// <summary>
        /// Зберігає поточний стан перед зміною
        /// </summary>
        public void SaveState(WorkshopData data, string actionDescription)
        {
            var snapshot = CreateSnapshot(data, actionDescription);
            _undoStack.Push(snapshot);
            
            // Очищаємо redo після нової дії
            _redoStack.Clear();
            
            // Обмежуємо розмір історії
            while (_undoStack.Count > MaxHistorySize)
            {
                var items = _undoStack.ToArray();
                _undoStack.Clear();
                for (int i = 0; i < items.Length - 1; i++)
                {
                    _undoStack.Push(items[i]);
                }
            }
            
            _logger.LogDebug("State saved: {Action}, Undo stack: {Count}", actionDescription, _undoStack.Count);
            OnStateChanged?.Invoke();
        }
        
        /// <summary>
        /// Виконує Undo - повертає попередній стан
        /// </summary>
        public WorkshopDataSnapshot? Undo(WorkshopData currentData)
        {
            if (!CanUndo) return null;
            
            // Зберігаємо поточний стан в redo
            var currentSnapshot = CreateSnapshot(currentData, "Before Undo");
            _redoStack.Push(currentSnapshot);
            
            // Повертаємо попередній стан
            var previousSnapshot = _undoStack.Pop();
            
            _logger.LogInformation("Undo: {Action}", previousSnapshot.ActionDescription);
            OnStateChanged?.Invoke();
            
            return previousSnapshot;
        }
        
        /// <summary>
        /// Виконує Redo - повертає скасований стан
        /// </summary>
        public WorkshopDataSnapshot? Redo(WorkshopData currentData)
        {
            if (!CanRedo) return null;
            
            // Зберігаємо поточний стан в undo
            var currentSnapshot = CreateSnapshot(currentData, "Before Redo");
            _undoStack.Push(currentSnapshot);
            
            // Повертаємо скасований стан
            var redoSnapshot = _redoStack.Pop();
            
            _logger.LogInformation("Redo: {Action}", redoSnapshot.ActionDescription);
            OnStateChanged?.Invoke();
            
            return redoSnapshot;
        }
        
        /// <summary>
        /// Очищає всю історію
        /// </summary>
        public void Clear()
        {
            _undoStack.Clear();
            _redoStack.Clear();
            _logger.LogDebug("History cleared");
            OnStateChanged?.Invoke();
        }
        
        /// <summary>
        /// Отримує опис останньої дії для Undo
        /// </summary>
        public string? GetUndoDescription()
        {
            return _undoStack.Count > 0 ? _undoStack.Peek().ActionDescription : null;
        }
        
        /// <summary>
        /// Отримує опис останньої дії для Redo
        /// </summary>
        public string? GetRedoDescription()
        {
            return _redoStack.Count > 0 ? _redoStack.Peek().ActionDescription : null;
        }
        
        private WorkshopDataSnapshot CreateSnapshot(WorkshopData data, string actionDescription)
        {
            return new WorkshopDataSnapshot
            {
                ActionDescription = actionDescription,
                Timestamp = DateTime.UtcNow,
                WorkshopOrders = data.WorkshopOrders.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                ),
                WorkshopOrderDates = data.WorkshopOrderDates.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                ),
                WorkshopOrderNames = data.WorkshopOrderNames.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value.ToList()
                ),
                CustomCompletionDates = new Dictionary<string, DateTime>(data.CustomCompletionDates)
            };
        }
    }
    
    /// <summary>
    /// Знімок стану даних для Undo/Redo
    /// </summary>
    public class WorkshopDataSnapshot
    {
        public string ActionDescription { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public Dictionary<int, List<double>> WorkshopOrders { get; set; } = new();
        public Dictionary<int, List<DateTime>> WorkshopOrderDates { get; set; } = new();
        public Dictionary<int, List<string>> WorkshopOrderNames { get; set; } = new();
        public Dictionary<string, DateTime> CustomCompletionDates { get; set; } = new();
        
        /// <summary>
        /// Застосовує знімок до WorkshopData
        /// </summary>
        public void ApplyTo(WorkshopData data)
        {
            data.WorkshopOrders = WorkshopOrders.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
            data.WorkshopOrderDates = WorkshopOrderDates.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
            data.WorkshopOrderNames = WorkshopOrderNames.ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value.ToList()
            );
            data.CustomCompletionDates = new Dictionary<string, DateTime>(CustomCompletionDates);
            data.LastUpdated = DateTime.UtcNow;
        }
    }
}
