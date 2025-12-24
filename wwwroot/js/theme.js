// Theme management
class ThemeManager {
    constructor() {
        this.currentTheme = this.getStoredTheme() || 'light';
        this.init();
    }

    init() {
        this.applyTheme(this.currentTheme);
        this.setupToggleButton();
    }

    getStoredTheme() {
        return localStorage.getItem('theme');
    }

    setStoredTheme(theme) {
        localStorage.setItem('theme', theme);
    }

    applyTheme(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        this.updateToggleButton(theme);
        this.setStoredTheme(theme);
        this.currentTheme = theme;
    }

    toggleTheme() {
        const newTheme = this.currentTheme === 'light' ? 'dark' : 'light';
        this.applyTheme(newTheme);
    }

    updateToggleButton(theme) {
        const button = document.getElementById('theme-toggle');
        const icon = document.getElementById('theme-icon');

        if (button && icon) {
            if (theme === 'dark') {
                icon.className = 'fas fa-sun';
                button.setAttribute('aria-label', 'Switch to light mode');
                button.title = 'Switch to light mode';
            } else {
                icon.className = 'fas fa-moon';
                button.setAttribute('aria-label', 'Switch to dark mode');
                button.title = 'Switch to dark mode';
            }
        }
    }

    setupToggleButton() {
        const toggleButton = document.getElementById('theme-toggle');
        if (toggleButton) {
            toggleButton.addEventListener('click', () => {
                this.toggleTheme();
            });
        }
    }
}

// Initialize theme manager when DOM is loaded
document.addEventListener('DOMContentLoaded', function () {
    window.themeManager = new ThemeManager();
});

// Handle system theme changes
if (window.matchMedia) {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function (e) {
        if (!localStorage.getItem('theme')) {
            const theme = e.matches ? 'dark' : 'light';
            window.themeManager.applyTheme(theme);
        }
    });
}