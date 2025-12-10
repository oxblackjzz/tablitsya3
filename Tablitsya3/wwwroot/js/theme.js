// Theme management - Dark/Light mode
window.ThemeManager = {
    // Get current theme from localStorage or system preference
    getCurrentTheme: function() {
        const saved = localStorage.getItem('theme');
        if (saved) {
            return saved;
        }
        // Check system preference
        if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
            return 'dark';
        }
        return 'light';
    },

    // Set theme
    setTheme: function(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        
        // Update Bootstrap classes for navbar
        const navbar = document.querySelector('.navbar');
        if (navbar) {
            if (theme === 'dark') {
                navbar.classList.remove('navbar-light', 'bg-light');
                navbar.classList.add('navbar-dark', 'bg-dark');
            } else {
                navbar.classList.remove('navbar-dark', 'bg-dark');
                navbar.classList.add('navbar-light', 'bg-light');
            }
        }
        
        console.log('Theme set to:', theme);
        return theme;
    },

    // Toggle theme
    toggleTheme: function() {
        const current = this.getCurrentTheme();
        const newTheme = current === 'dark' ? 'light' : 'dark';
        return this.setTheme(newTheme);
    },

    // Initialize theme on page load
    init: function() {
        const theme = this.getCurrentTheme();
        this.setTheme(theme);
        
        // Listen for system theme changes
        if (window.matchMedia) {
            window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
                if (!localStorage.getItem('theme')) {
                    this.setTheme(e.matches ? 'dark' : 'light');
                }
            });
        }
        
        console.log('ThemeManager initialized with theme:', theme);
    }
};

// Auto-initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', () => ThemeManager.init());
} else {
    ThemeManager.init();
}
