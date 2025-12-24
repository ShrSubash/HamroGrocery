// ===================================
// Hamro Grocery Admin Panel
// Modern Dashboard JavaScript
// ===================================

document.addEventListener('DOMContentLoaded', function () {

    // ===================================
    // Theme Toggle Functionality
    // ===================================
    const themeToggle = document.getElementById('themeToggle');
    const html = document.documentElement;

    // Load saved theme preference
    const savedTheme = localStorage.getItem('admin-theme') || 'light';
    html.setAttribute('data-theme', savedTheme);
    updateThemeIcon(savedTheme);

    // Theme toggle click handler
    if (themeToggle) {
        themeToggle.addEventListener('click', function () {
            const currentTheme = html.getAttribute('data-theme');
            const newTheme = currentTheme === 'light' ? 'dark' : 'light';

            html.setAttribute('data-theme', newTheme);
            localStorage.setItem('admin-theme', newTheme);
            updateThemeIcon(newTheme);

            // Smooth transition effect
            html.style.transition = 'all 0.3s ease';
            setTimeout(() => {
                html.style.transition = '';
            }, 300);
        });
    }

    function updateThemeIcon(theme) {
        if (themeToggle) {
            const icon = themeToggle.querySelector('i');
            if (theme === 'dark') {
                icon.className = 'fas fa-sun';
            } else {
                icon.className = 'fas fa-moon';
            }
        }
    }

    // ===================================
    // Active Navigation Link Highlighting
    // ===================================
    const currentPath = window.location.pathname;
    const navLinks = document.querySelectorAll('.sidebar .nav-link');

    navLinks.forEach(link => {
        const linkPath = link.getAttribute('href');

        // Exact match or starts with (for sub-pages)
        if (linkPath === currentPath || currentPath.startsWith(linkPath)) {
            link.classList.add('active');
        }
    });

    // ===================================
    // Mobile Sidebar Toggle
    // ===================================
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.querySelector('.sidebar');
    const overlay = document.createElement('div');
    overlay.className = 'sidebar-overlay';

    if (sidebarToggle && sidebar) {
        // Create overlay for mobile
        document.body.appendChild(overlay);

        // Toggle sidebar
        sidebarToggle.addEventListener('click', function () {
            sidebar.classList.toggle('active');
            overlay.classList.toggle('active');
            document.body.style.overflow = sidebar.classList.contains('active') ? 'hidden' : '';
        });

        // Close sidebar when clicking overlay
        overlay.addEventListener('click', function () {
            sidebar.classList.remove('active');
            overlay.classList.remove('active');
            document.body.style.overflow = '';
        });
    }

    // ===================================
    // Smooth Page Transitions
    // ===================================
    const contentArea = document.querySelector('.content-area');
    if (contentArea) {
        contentArea.style.opacity = '0';
        contentArea.style.transform = 'translateY(20px)';

        setTimeout(() => {
            contentArea.style.transition = 'all 0.4s ease';
            contentArea.style.opacity = '1';
            contentArea.style.transform = 'translateY(0)';
        }, 100);
    }

    // ===================================
    // Stat Card Animation on Scroll
    // ===================================
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.style.opacity = '1';
                entry.target.style.transform = 'translateY(0)';
            }
        });
    }, observerOptions);

    // Observe stat cards
    document.querySelectorAll('.stat-card').forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'all 0.5s ease';
        observer.observe(card);
    });

    // ===================================
    // Auto-refresh Dashboard Stats (every 30 seconds)
    // ===================================
    function refreshDashboardStats() {
        // This would typically make an AJAX call to fetch updated stats
        console.log('Dashboard stats refreshed at', new Date().toLocaleTimeString());

        // Example: You can implement actual AJAX call here
        // fetch('/Admin/Home/GetDashboardStats')
        //     .then(response => response.json())
        //     .then(data => updateStats(data));
    }

    // Auto-refresh every 30 seconds (only on dashboard page)
    if (document.querySelector('.dashboard')) {
        setInterval(refreshDashboardStats, 30000);
    }

    // ===================================
    // Dropdown Menu Enhancement
    // ===================================
    const dropdowns = document.querySelectorAll('.dropdown');

    dropdowns.forEach(dropdown => {
        const toggle = dropdown.querySelector('[data-bs-toggle="dropdown"]');

        if (toggle) {
            toggle.addEventListener('click', function (e) {
                e.stopPropagation();
            });
        }
    });

    // ===================================
    // Table Row Click Handler
    // ===================================
    document.querySelectorAll('.table tbody tr').forEach(row => {
        row.style.cursor = 'pointer';

        row.addEventListener('click', function (e) {
            // Don't trigger if clicking a button or link
            if (e.target.tagName !== 'BUTTON' &&
                e.target.tagName !== 'A' &&
                !e.target.closest('button') &&
                !e.target.closest('a')) {

                // Get order ID or relevant data
                const orderId = this.querySelector('td:first-child strong')?.textContent;
                if (orderId) {
                    console.log('Clicked row:', orderId);
                    // Navigate to detail page
                    // window.location.href = `/Admin/Orders/Details/${orderId}`;
                }
            }
        });
    });

    // ===================================
    // Notification Badge Update
    // ===================================
    function updateNotificationBadge(count) {
        let badge = document.querySelector('.notification-badge');

        if (count > 0) {
            if (!badge) {
                badge = document.createElement('span');
                badge.className = 'notification-badge';
                document.querySelector('.user-area')?.appendChild(badge);
            }
            badge.textContent = count > 9 ? '9+' : count;
            badge.style.display = 'flex';
        } else if (badge) {
            badge.style.display = 'none';
        }
    }

    // ===================================
    // Search Functionality (if search box exists)
    // ===================================
    const searchInput = document.querySelector('.search-input');
    if (searchInput) {
        let searchTimeout;

        searchInput.addEventListener('input', function (e) {
            clearTimeout(searchTimeout);

            searchTimeout = setTimeout(() => {
                const query = e.target.value.trim();
                if (query.length >= 2) {
                    performSearch(query);
                }
            }, 300);
        });
    }

    function performSearch(query) {
        console.log('Searching for:', query);
        // Implement actual search logic here
        // fetch(`/Admin/Search?q=${encodeURIComponent(query)}`)
        //     .then(response => response.json())
        //     .then(data => displaySearchResults(data));
    }

    // ===================================
    // Console Welcome Message
    // ===================================
    console.log('%c🛒 Hamro Grocery Admin Panel',
        'color: #008060; font-size: 20px; font-weight: bold;');
    console.log('%cVersion 1.0.0 | Shopify-Inspired Design',
        'color: #6d7175; font-size: 12px;');
});

// ===================================
// Global Utility Functions
// ===================================

// Format currency
function formatCurrency(amount) {
    return new Intl.NumberFormat('en-NP', {
        style: 'currency',
        currency: 'NPR',
        minimumFractionDigits: 2
    }).format(amount);
}

// Format date
function formatDate(dateString) {
    const date = new Date(dateString);
    return new Intl.DateTimeFormat('en-US', {
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    }).format(date);
}

// Show toast notification
function showToast(message, type = 'success') {
    const toast = document.createElement('div');
    toast.className = `toast-notification toast-${type}`;
    toast.innerHTML = `
        <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
        <span>${message}</span>
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.classList.add('show');
    }, 100);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// Export functions for use in other scripts
window.HamroAdmin = {
    formatCurrency,
    formatDate,
    showToast
};