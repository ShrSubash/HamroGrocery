// ============================================
// MODERN HOME PAGE INTERACTIONS - 2025
// ============================================

document.addEventListener('DOMContentLoaded', function () {
    initializeHomePage();
});

// ============================================
// MAIN INITIALIZATION
// ============================================
function initializeHomePage() {
    initCountdownTimer();
    initNewsletterForm();
    initSearchAutocomplete();
    initScrollAnimations();
    initCategoryScroll();
    initLazyLoading();
    initQuickViewModal();
    initWishlist();
    loadInitialCartCount(); // Load cart count on page load
}

// ============================================
// LOAD INITIAL CART COUNT
// ============================================
async function loadInitialCartCount() {
    try {
        const response = await fetch('/Cart/GetCartCount');
        const data = await response.json();
        updateCartCount(data.count);
    } catch (error) {
        console.error('Error loading cart count:', error);
    }
}

// ============================================
// DEALS COUNTDOWN TIMER
// ============================================
function initCountdownTimer() {
    const countdownElement = document.getElementById('dealCountdown');
    if (!countdownElement) return;

    // Set end time to midnight tonight
    const now = new Date();
    const endTime = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);

    function updateCountdown() {
        const now = new Date();
        const timeLeft = endTime - now;

        if (timeLeft <= 0) {
            countdownElement.textContent = '00:00:00';
            return;
        }

        const hours = Math.floor(timeLeft / (1000 * 60 * 60));
        const minutes = Math.floor((timeLeft % (1000 * 60 * 60)) / (1000 * 60));
        const seconds = Math.floor((timeLeft % (1000 * 60)) / 1000);

        countdownElement.textContent =
            `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`;
    }

    updateCountdown();
    setInterval(updateCountdown, 1000);
}

// ============================================
// NEWSLETTER SUBSCRIPTION
// ============================================
function initNewsletterForm() {
    const form = document.getElementById('newsletterForm');
    if (!form) return;

    form.addEventListener('submit', async function (e) {
        e.preventDefault();

        const emailInput = document.getElementById('newsletterEmail');
        const email = emailInput.value.trim();

        if (!isValidEmail(email)) {
            showToast('Please enter a valid email address', 'error');
            return;
        }

        const submitBtn = form.querySelector('button[type="submit"]');
        const originalText = submitBtn.innerHTML;
        submitBtn.disabled = true;
        submitBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Subscribing...';

        try {
            await new Promise(resolve => setTimeout(resolve, 1500));
            showToast('🎉 Successfully subscribed! Check your email for exclusive offers.', 'success');
            emailInput.value = '';
            localStorage.setItem('newsletter_subscribed', 'true');
            localStorage.setItem('newsletter_email', email);
        } catch (error) {
            showToast('Subscription failed. Please try again.', 'error');
        } finally {
            submitBtn.disabled = false;
            submitBtn.innerHTML = originalText;
        }
    });
}

// ============================================
// SEARCH AUTOCOMPLETE
// ============================================
function initSearchAutocomplete() {
    const searchInput = document.getElementById('heroSearchInput');
    if (!searchInput) return;

    let debounceTimer;
    const autocompleteContainer = createAutocompleteContainer(searchInput);

    searchInput.addEventListener('input', function () {
        clearTimeout(debounceTimer);
        const query = this.value.trim();

        if (query.length < 2) {
            autocompleteContainer.style.display = 'none';
            return;
        }

        debounceTimer = setTimeout(() => {
            fetchSearchSuggestions(query, autocompleteContainer);
        }, 300);
    });

    document.addEventListener('click', function (e) {
        if (!searchInput.contains(e.target) && !autocompleteContainer.contains(e.target)) {
            autocompleteContainer.style.display = 'none';
        }
    });
}

function createAutocompleteContainer(searchInput) {
    const container = document.createElement('div');
    container.className = 'search-autocomplete';
    container.style.cssText = `
        position: absolute;
        top: 100%;
        left: 0;
        right: 0;
        background: white;
        border-radius: 12px;
        margin-top: 8px;
        box-shadow: 0 10px 40px rgba(0,0,0,0.15);
        max-height: 400px;
        overflow-y: auto;
        display: none;
        z-index: 1000;
    `;
    searchInput.parentElement.style.position = 'relative';
    searchInput.parentElement.appendChild(container);
    return container;
}

async function fetchSearchSuggestions(query, container) {
    try {
        const response = await fetch(`/Products/SearchSuggestions?query=${encodeURIComponent(query)}`);

        if (!response.ok) {
            throw new Error('Search failed');
        }

        const suggestions = await response.json();

        if (suggestions.length === 0) {
            container.innerHTML = `
                <div style="padding: 20px; text-align: center; color: #6b7280;">
                    <i class="fas fa-search" style="font-size: 24px; margin-bottom: 8px;"></i>
                    <p style="margin: 0;">No suggestions found</p>
                </div>
            `;
            container.style.display = 'block';
            return;
        }

        container.innerHTML = suggestions.map(item => `
            <a href="${item.url}" 
               class="autocomplete-item"
               style="display: flex; align-items: center; gap: 12px; padding: 12px 20px; 
                      text-decoration: none; color: #374151; transition: all 0.2s;">
                <img src="${item.image}" 
                     style="width: 40px; height: 40px; object-fit: cover; border-radius: 8px; background: #f3f4f6;"
                     onerror="this.src='/images/no-image.jpg'">
                <div style="flex: 1;">
                    <div style="font-weight: 600; font-size: 14px; color: #111827;">${item.name}</div>
                    <div style="font-size: 12px; color: #6b7280;">${item.category} • $${item.price.toFixed(2)}</div>
                </div>
                <i class="fas fa-arrow-right" style="color: #ff6b35;"></i
            </a>
        `).join('');

        container.querySelectorAll('.autocomplete-item').forEach(item => {
            item.addEventListener('mouseenter', function () {
                this.style.background = '#f3f4f6';
            });
            item.addEventListener('mouseleave', function () {
                this.style.background = 'white';
            });
        });

        container.style.display = 'block';

    } catch (error) {
        console.error('Search suggestions error:', error);
        container.style.display = 'none';
    }
}

// ============================================
// SCROLL ANIMATIONS
// ============================================
function initScrollAnimations() {
    const observerOptions = {
        threshold: 0.1,
        rootMargin: '0px 0px -50px 0px'
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('fade-in');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    document.querySelectorAll('.product-card, .category-card, .testimonial-card, .deal-card, .feature-item').forEach(el => {
        el.style.opacity = '0';
        el.style.transform = 'translateY(30px)';
        el.style.transition = 'opacity 0.6s ease, transform 0.6s ease';
        observer.observe(el);
    });
}

const style = document.createElement('style');
style.textContent = `
    .fade-in {
        opacity: 1 !important;
        transform: translateY(0) !important;
    }
`;
document.head.appendChild(style);

// ============================================
// CATEGORY SCROLL ARROWS
// ============================================
function initCategoryScroll() {
    const categoryScroll = document.querySelector('.category-scroll');
    if (!categoryScroll) return;

    categoryScroll.style.scrollBehavior = 'smooth';

    if (window.innerWidth > 768) {
        addScrollButtons(categoryScroll);
    }
}

function addScrollButtons(scrollContainer) {
    const parent = scrollContainer.parentElement;
    const leftBtn = createScrollButton('left');
    const rightBtn = createScrollButton('right');

    parent.style.position = 'relative';
    parent.appendChild(leftBtn);
    parent.appendChild(rightBtn);

    leftBtn.addEventListener('click', () => {
        scrollContainer.scrollBy({ left: -200, behavior: 'smooth' });
    });

    rightBtn.addEventListener('click', () => {
        scrollContainer.scrollBy({ left: 200, behavior: 'smooth' });
    });

    scrollContainer.addEventListener('scroll', () => {
        leftBtn.style.opacity = scrollContainer.scrollLeft > 0 ? '1' : '0';
        rightBtn.style.opacity =
            scrollContainer.scrollLeft < (scrollContainer.scrollWidth - scrollContainer.clientWidth - 10) ? '1' : '0';
    });

    leftBtn.style.opacity = '0';
}

function createScrollButton(direction) {
    const btn = document.createElement('button');
    btn.innerHTML = `<i class="fas fa-chevron-${direction}"></i>`;
    btn.style.cssText = `
        position: absolute;
        top: 50%;
        ${direction}: 10px;
        transform: translateY(-50%);
        width: 40px;
        height: 40px;
        border-radius: 50%;
        background: white;
        border: 1px solid #e5e7eb;
        color: #ff6b35;
        cursor: pointer;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        z-index: 10;
        transition: all 0.3s;
    `;

    btn.addEventListener('mouseenter', function () {
        this.style.background = '#ff6b35';
        this.style.color = 'white';
        this.style.transform = 'translateY(-50%) scale(1.1)';
    });

    btn.addEventListener('mouseleave', function () {
        this.style.background = 'white';
        this.style.color = '#ff6b35';
        this.style.transform = 'translateY(-50%)';
    });

    return btn;
}

// ============================================
// LAZY LOADING IMAGES
// ============================================
function initLazyLoading() {
    if ('IntersectionObserver' in window) {
        const imageObserver = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const img = entry.target;
                    img.src = img.dataset.src || img.src;
                    img.classList.add('loaded');
                    imageObserver.unobserve(img);
                }
            });
        });

        document.querySelectorAll('.product-image, .deal-image img').forEach(img => {
            imageObserver.observe(img);
        });
    }
}

// ============================================
// QUICK VIEW MODAL
// ============================================
function initQuickViewModal() {
    const modal = document.createElement('div');
    modal.id = 'quickViewModal';
    modal.style.cssText = `
        display: none;
        position: fixed;
        top: 0;
        left: 0;
        right: 0;
        bottom: 0;
        background: rgba(0, 0, 0, 0.7);
        z-index: 9999;
        padding: 20px;
        overflow-y: auto;
    `;

    modal.innerHTML = `
        <div style="max-width: 900px; margin: 50px auto; background: white; border-radius: 16px; position: relative;">
            <button onclick="closeQuickView()" style="position: absolute; top: 16px; right: 16px; width: 40px; height: 40px; 
                    border-radius: 50%; background: #f3f4f6; border: none; cursor: pointer; z-index: 10;">
                <i class="fas fa-times"></i>
            </button>
            <div id="quickViewContent" style="padding: 40px;">
                <div style="text-align: center; padding: 60px;">
                    <i class="fas fa-spinner fa-spin" style="font-size: 48px; color: #10b981;"></i>
                </div>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
}

async function quickView(productId) {
    const modal = document.getElementById('quickViewModal');
    const content = document.getElementById('quickViewContent');

    modal.style.display = 'block';
    document.body.style.overflow = 'hidden';

    content.innerHTML = `
        <div style="text-align: center; padding: 60px;">
            <i class="fas fa-spinner fa-spin" style="font-size: 48px; color: #10b981;"></i>
            <p style="margin-top: 16px; color: #6b7280;">Loading product details...</p>
        </div>
    `;

    try {
        const response = await fetch(`/Products/QuickView/${productId}`);

        if (!response.ok) {
            throw new Error('Failed to load product');
        }

        const html = await response.text();
        content.innerHTML = html;

    } catch (error) {
        content.innerHTML = `
            <div style="text-align: center; padding: 60px;">
                <i class="fas fa-exclamation-circle" style="font-size: 48px; color: #ef4444;"></i>
                <p style="margin-top: 16px; color: #6b7280;">Failed to load product details</p>
                <button onclick="closeQuickView()" style="margin-top: 20px; padding: 10px 24px; 
                        background: #10b981; color: white; border: none; border-radius: 8px; cursor: pointer;">
                    Close
                </button>
            </div>
        `;
    }
}

function closeQuickView() {
    const modal = document.getElementById('quickViewModal');
    modal.style.display = 'none';
    document.body.style.overflow = 'auto';
}

document.addEventListener('click', function (e) {
    const modal = document.getElementById('quickViewModal');
    if (e.target === modal) {
        closeQuickView();
    }
});

// ============================================
// WISHLIST FUNCTIONALITY
// ============================================
function initWishlist() {
    if (!localStorage.getItem('wishlist')) {
        localStorage.setItem('wishlist', JSON.stringify([]));
    }
    updateWishlistCount();
}

function addToWishlist(productId) {
    let wishlist = JSON.parse(localStorage.getItem('wishlist') || '[]');

    if (wishlist.includes(productId)) {
        wishlist = wishlist.filter(id => id !== productId);
        showToast('Removed from wishlist', 'info');
    } else {
        wishlist.push(productId);
        showToast('Added to wishlist ❤️', 'success');
    }

    localStorage.setItem('wishlist', JSON.stringify(wishlist));
    updateWishlistCount();
    updateWishlistIcons();
}

function updateWishlistCount() {
    const wishlist = JSON.parse(localStorage.getItem('wishlist') || '[]');
    const countElement = document.querySelector('.wishlist-count');
    if (countElement) {
        countElement.textContent = wishlist.length;
        countElement.style.display = wishlist.length > 0 ? 'flex' : 'none';
    }
}

function updateWishlistIcons() {
    const wishlist = JSON.parse(localStorage.getItem('wishlist') || '[]');

    document.querySelectorAll('[onclick^="addToWishlist"]').forEach(btn => {
        const match = btn.getAttribute('onclick').match(/\d+/);
        if (match) {
            const productId = parseInt(match[0]);
            const icon = btn.querySelector('i');

            if (wishlist.includes(productId)) {
                icon.classList.remove('far');
                icon.classList.add('fas');
                icon.style.color = '#ef4444';
            } else {
                icon.classList.remove('fas');
                icon.classList.add('far');
                icon.style.color = '';
            }
        }
    });
}

// ============================================
// ENHANCED ADD TO CART - FIXED FOR YOUR CARTCONTROLLER
// ============================================
async function addToCart(button) {
    const productId = button.getAttribute('data-product-id');
    const productName = button.getAttribute('data-product-name');
    const productPrice = button.getAttribute('data-product-price');

    if (!productId) {
        showToast('Invalid product. Please try again.', 'error');
        return;
    }

    const originalHTML = button.innerHTML;
    button.disabled = true;
    button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Adding...';

    try {
        // Get anti-forgery token
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

        if (!token) {
            throw new Error('Security token not found. Please refresh the page.');
        }

        const response = await fetch('/Cart/AddToCart', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': token
            },
            body: JSON.stringify({
                productId: parseInt(productId),
                quantity: 1,
                variationId: null
            })
        });

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const result = await response.json();

        if (result.success) {
            // Update cart count
            updateCartCount(result.cartCount);

            // Show success animation
            button.innerHTML = '<i class="fas fa-check"></i> Added!';
            button.style.background = '#10b981';

            // Show toast
            showToast(`${productName} added to cart!`, 'success');

            // Reset button after 2 seconds
            setTimeout(() => {
                button.innerHTML = originalHTML;
                button.disabled = false;
                button.style.background = '';
            }, 2000);

        } else {
            throw new Error(result.message || 'Failed to add to cart');
        }

    } catch (error) {
        console.error('Add to cart error:', error);
        showToast(error.message || 'Failed to add item to cart', 'error');
        button.innerHTML = originalHTML;
        button.disabled = false;
    }
}

function updateCartCount(count) {
    const selectors = [
        '#cartCount',
        '.cart-count',
        '#cart-count',
        '.mini-cart-count'
    ];

    selectors.forEach(selector => {
        const element = document.querySelector(selector);
        if (element) {
            element.textContent = count;
            element.style.display = count > 0 ? 'inline-flex' : 'none';

            // Animate
            element.style.transform = 'scale(1.3)';
            setTimeout(() => {
                element.style.transform = 'scale(1)';
            }, 300);
        }
    });
}

// ============================================
// TOAST NOTIFICATIONS
// ============================================
function showToast(message, type = 'info') {
    document.querySelectorAll('.custom-toast').forEach(toast => toast.remove());

    const toast = document.createElement('div');
    toast.className = 'custom-toast';

    const bgColors = {
        success: '#10b981',  // Keep green for success
        error: '#ef4444',     // Red for errors
        warning: '#f59e0b',   // Yellow for warnings
        info: '#3b82f6'       // Blue for info
    };

    const icons = {
        success: 'fa-check-circle',
        error: 'fa-exclamation-circle',
        warning: 'fa-exclamation-triangle',
        info: 'fa-info-circle'
    };

    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        background: ${bgColors[type]};
        color: white;
        padding: 16px 24px;
        border-radius: 12px;
        box-shadow: 0 10px 25px rgba(0,0,0,0.2);
        z-index: 10000;
        display: flex;
        align-items: center;
        gap: 12px;
        font-weight: 600;
        animation: slideInRight 0.3s ease;
        max-width: 400px;
    `;

    toast.innerHTML = `
        <i class="fas ${icons[type]}" style="font-size: 20px;"></i>
        <span>${message}</span>
    `;

    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

const toastStyle = document.createElement('style');
toastStyle.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(400px);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(400px);
            opacity: 0;
        }
    }
`;
document.head.appendChild(toastStyle);

// ============================================
// PERFORMANCE OPTIMIZATIONS
// ============================================
function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

// Smooth scroll for anchor links
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// Back to top button
function initBackToTop() {
    const backToTop = document.createElement('button');
    backToTop.innerHTML = '<i class="fas fa-arrow-up"></i>';
    backToTop.id = 'backToTop';
    backToTop.style.cssText = `
        position: fixed;
        bottom: 30px;
        right: 30px;
        width: 50px;
        height: 50px;
        border-radius: 50%;
        background: linear-gradient(135deg, #ff6b35, #e85d2c);
        color: white;
        border: none;
        cursor: pointer;
        opacity: 0;
        visibility: hidden;
        transition: all 0.3s;
        box-shadow: 0 4px 12px rgba(255, 107, 53, 0.3);
        z-index: 1000;
    `;

    document.body.appendChild(backToTop);

    window.addEventListener('scroll', debounce(() => {
        if (window.pageYOffset > 300) {
            backToTop.style.opacity = '1';
            backToTop.style.visibility = 'visible';
        } else {
            backToTop.style.opacity = '0';
            backToTop.style.visibility = 'hidden';
        }
    }, 100));

    backToTop.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });

    backToTop.addEventListener('mouseenter', function () {
        this.style.transform = 'scale(1.1)';
    });

    backToTop.addEventListener('mouseleave', function () {
        this.style.transform = 'scale(1)';
    });
}

initBackToTop();

// ============================================
// UTILITY FUNCTIONS
// ============================================
function isValidEmail(email) {
    const re = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return re.test(email);
}

// ============================================
// EXPOSE GLOBAL FUNCTIONS
// ============================================
window.addToCart = addToCart;
window.addToWishlist = addToWishlist;
window.quickView = quickView;
window.closeQuickView = closeQuickView;

// Loading state management
window.addEventListener('load', () => {
    document.body.classList.add('loaded');

    setTimeout(() => {
        document.querySelectorAll('.hero-title, .hero-subtitle, .hero-search').forEach((el, index) => {
            setTimeout(() => {
                el.style.opacity = '1';
                el.style.transform = 'translateY(0)';
            }, index * 100);
        });
    }, 100);
});

console.log('🚀 Hamro Grocery - Modern Homepage Loaded Successfully!');