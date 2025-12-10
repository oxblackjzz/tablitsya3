using Microsoft.JSInterop;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для взаємодії з JavaScript drag & drop функціоналом
    /// </summary>
    public class DragDropInterop : IAsyncDisposable
    {
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<DragDropInterop> _logger;
        private bool _disposed;

        public DragDropInterop(IJSRuntime jsRuntime, ILogger<DragDropInterop> logger)
        {
            _jsRuntime = jsRuntime;
            _logger = logger;
        }

        /// <summary>
        /// Ініціалізує Sortable для елемента
        /// </summary>
        public async Task<bool> InitSortableAsync(string elementId, DotNetObjectReference<object> dotNetHelper, int workshopNumber)
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<bool>(
                    "DragDropInterop.initSortable",
                    elementId,
                    dotNetHelper,
                    workshopNumber
                );
                
                if (result)
                {
                    _logger.LogInformation("✅ Sortable initialized for element: {ElementId}", elementId);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Sortable for: {ElementId}", elementId);
                return false;
            }
        }

        /// <summary>
        /// Знищує Sortable інстанс
        /// </summary>
        public async Task DestroySortableAsync(string elementId)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync("DragDropInterop.destroySortable", elementId);
                _logger.LogInformation("🗑️ Sortable destroyed for element: {ElementId}", elementId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to destroy Sortable for: {ElementId}", elementId);
            }
        }

        /// <summary>
        /// Ініціалізує drag & drop між цехами
        /// </summary>
        public async Task<bool> InitCrossWorkshopDragAsync(string[] containerIds, DotNetObjectReference<object> dotNetHelper)
        {
            try
            {
                var result = await _jsRuntime.InvokeAsync<bool>(
                    "DragDropInterop.initCrossWorkshopDrag",
                    containerIds,
                    dotNetHelper
                );
                
                if (result)
                {
                    _logger.LogInformation("✅ Cross-workshop drag initialized");
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize cross-workshop drag");
                return false;
            }
        }

        /// <summary>
        /// Показує toast повідомлення
        /// </summary>
        public async Task ShowToastAsync(string message, ToastType type = ToastType.Info)
        {
            try
            {
                await _jsRuntime.InvokeVoidAsync(
                    "DragDropInterop.showToast",
                    message,
                    type.ToString().ToLower()
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to show toast");
            }
        }

        /// <summary>
        /// Показує діалог підтвердження
        /// </summary>
        public async Task<bool> ConfirmAsync(string message)
        {
            try
            {
                return await _jsRuntime.InvokeAsync<bool>("DragDropInterop.confirm", message);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to show confirm dialog");
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_disposed)
            {
                _disposed = true;
            }
            await Task.CompletedTask;
        }
    }

    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
