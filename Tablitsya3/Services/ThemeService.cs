using Microsoft.JSInterop;

namespace Tablitsya3.Services
{
    /// <summary>
    /// Сервіс для управління темою (темна/світла)
    /// </summary>
    public class ThemeService
    {
        private readonly IJSRuntime _jsRuntime;
        private string _currentTheme = "light";

        public event Action<string>? OnThemeChanged;

        public ThemeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string CurrentTheme => _currentTheme;
        public bool IsDarkMode => _currentTheme == "dark";

        /// <summary>
        /// Ініціалізує тему з localStorage або системних налаштувань
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _currentTheme = await _jsRuntime.InvokeAsync<string>("ThemeManager.getCurrentTheme");
            }
            catch
            {
                _currentTheme = "light";
            }
        }

        /// <summary>
        /// Встановлює тему
        /// </summary>
        public async Task SetThemeAsync(string theme)
        {
            try
            {
                _currentTheme = await _jsRuntime.InvokeAsync<string>("ThemeManager.setTheme", theme);
                OnThemeChanged?.Invoke(_currentTheme);
            }
            catch
            {
                // Ignore JS errors
            }
        }

        /// <summary>
        /// Перемикає тему
        /// </summary>
        public async Task ToggleThemeAsync()
        {
            try
            {
                _currentTheme = await _jsRuntime.InvokeAsync<string>("ThemeManager.toggleTheme");
                OnThemeChanged?.Invoke(_currentTheme);
            }
            catch
            {
                // Ignore JS errors
            }
        }
    }
}
