/**
 * HAMRO GROCERY - HOME PAGE SCRIPTS
 * Handles: Add to Cart, Animations, Search, Dynamic Cart Updates
 */

(function () {
    'use strict';

    // ============================================
    // DOM READY
    // ============================================
    document.addEventListener('DOMContentLoaded', function () {
        initAddToCart();
        initScrollAnimations();
        initQuickActions();
        loadCartCount(); // Load initial cart count
    });

    // ============================================
    // ADD TO CART - PRODUCTION READY
    // ============================================
    function initAddToCart() {
        // Use event delegation for dynamic buttons
        document.addEventListener('click', async function (e) {
            const btn = e.target.closest('.btn-add-to-cart');
            if (!btn || btn.disabled) return;

            e.preventDefault();

            // Extract data attributes
            const productIdStr = btn.getAttribute('data-product-id');
            const productName = btn.getAttribute('data-product-name') || 'Product';
            const variationIdStr = btn.getAttribute('data-variation-id');

            console.log('🛒 Add to Cart clicked:', {
                productIdStr: productIdStr,
                variationIdStr: variationIdStr,
                productName: productName
            });

            // ✅ CRITICAL: Validate productId exists and is valid
            if (!productIdStr || productIdStr === '' || productIdStr === 'null' || productIdStr === 'undefined') {
                console.error('❌ Invalid product ID:', productIdStr);
                showToast('Product information missing. Please refresh the page.', 'error');
                return;
            }

            const productId = parseInt(productIdStr, 10);

            // ✅ CRITICAL: Validate productId is a valid number
            if (isNaN(productId) || productId <= 0) {
                console.error('❌ Product ID is not a valid number:', productIdStr, '→', productId);
                showToast('Invalid product. Please try again.', 'error');
                return;
            }

            // Parse variation ID (optional)
            const variationId = variationIdStr && variationIdStr !== 'null'
                ? parseInt(variationIdStr, 10)
                : null;

            console.log('✅ Valid productId:', productId, 'variationId:', variationId);

            const originalHTML = btn.innerHTML;

            // Disable button and show loading
            btn.disabled = true;
            btn.classList.add('adding');
            btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Adding...';

            try {
                const requestBody = {
                    productId: productId,
                    quantity: 1,
                    variationId: variationId
                };

                console.log('📤 Sending request:', requestBody);

                const response = await fetch('/Cart/AddToCart', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': getAntiForgeryToken()
                    },
                    body: JSON.stringify(requestBody)
                });

                console.log('📥 Response status:', response.status);

                if (!response.ok) {
                    const errorText = await response.text();
                    throw new Error(`Server error: ${response.status} - ${errorText}`);
                }

                const data = await response.json();
                console.log('📥 Response data:', data);

                if (data.success) {
                    // Update cart count
                    updateCartCount(data.cartCount);

                    // Show success state
                    btn.classList.remove('adding');
                    btn.classList.add('added');
                    btn.innerHTML = '<i class="fas fa-check"></i> Added!';

                    // Show success toast
                    showToast(`${productName} added to cart!`, 'success');

                    // Reset button after 2 seconds
                    setTimeout(() => {
                        btn.innerHTML = originalHTML;
                        btn.classList.remove('added');
                        btn.disabled = false;
                    }, 2000);
                } else {
                    throw new Error(data.message || 'Failed to add to cart');
                }
            } catch (error) {
                console.error('❌ Cart error:', error);

                btn.classList.remove('adding');
                btn.innerHTML = '<i class="fas fa-times"></i> Failed';

                showToast(error.message || 'Failed to add to cart', 'error');

                setTimeout(() => {
                    btn.innerHTML = originalHTML;
                    btn.disabled = false;
                }, 2000);
            }
        });
    }

    // ============================================
    // LOAD CART COUNT ON PAGE LOAD
    // ============================================
    function loadCartCount() {
        fetch('/Cart/GetCartCount')
            .then(response => {
                if (!response.ok) {
                    throw new Error('Failed to load cart count');
                }
                return response.json();
            })
            .then(data => {
                updateCartCount(data.count);
            })
            .catch(error => {
                console.error('Error loading cart count:', error);
                // Don't show error to user on initial load
            });
    }

    // ============================================
    // UPDATE CART COUNT - GLOBALLY ACCESSIBLE
    // ============================================
    function updateCartCount(count) {
        const badge = document.getElementById('cartCount');
        if (badge) {
            badge.textContent = count || 0;

            // Show/hide badge based on count
            if (count > 0) {
                badge.style.display = 'inline-flex';
                badge.style.opacity = '1';
            } else {
                badge.style.display = 'none';
            }

            // Bounce animation
            badge.style.animation = 'none';
            setTimeout(() => {
                badge.style.animation = 'cartBounce 0.5s ease';
            }, 10);
        }
    }

    // Make updateCartCount globally accessible for other scripts
    window.updateCartCount = updateCartCount;

    window.addEventListener('cartUpdated', function () {
        console.log('🔄 Cart updated event received, refreshing count...');
        loadCartCount(); // Reload cart count when triggered from other pages
    });
    // ============================================
    // TOAST NOTIFICATIONS
    // ============================================
    function showToast(message, type = 'success') {
        const toast = document.createElement('div');
        toast.className = `toast-notification toast-${type}`;
        toast.innerHTML = `
            <div class="toast-icon">
                <i class="fas fa-${type === 'success' ? 'check-circle' : 'exclamation-circle'}"></i>
            </div>
            <div class="toast-content">
                <strong>${type === 'success' ? 'Success!' : 'Error'}</strong>
                <p>${message}</p>
            </div>
            <button class="toast-close" onclick="this.closest('.toast-notification').remove()">
                <i class="fas fa-times"></i>
            </button>
        `;

        let container = document.querySelector('.toast-container');
        if (!container) {
            container = document.createElement('div');
            container.className = 'toast-container';
            document.body.appendChild(container);
        }

        container.appendChild(toast);

        // Auto remove after 3 seconds
        setTimeout(() => {
            toast.style.animation = 'slideOut 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }

    // ============================================
    // SCROLL ANIMATIONS
    // ============================================
    function initScrollAnimations() {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                    observer.unobserve(entry.target);
                }
            });
        }, { threshold: 0.1, rootMargin: '0px 0px -50px 0px' });

        document.querySelectorAll('.product-card, .feature-item').forEach((el, index) => {
            el.style.opacity = '0';
            el.style.transform = 'translateY(30px)';
            el.style.transition = `all 0.6s ease ${index * 0.05}s`;
            observer.observe(el);
        });
    }

    // ============================================
    // QUICK ACTIONS (Wishlist, Quick View)
    // ============================================
    function initQuickActions() {
        document.addEventListener('click', function (e) {
            const btn = e.target.closest('.quick-action-btn');
            if (!btn) return;

            const card = btn.closest('.product-card');
            const productLink = card.querySelector('.product-name a');

            if (btn.querySelector('.fa-eye')) {
                // Quick View - TODO: Implement modal
                console.log('Quick view:', productLink?.href);
                showToast('Quick view coming soon!', 'info');
            } else if (btn.querySelector('.fa-heart')) {
                // Add to Wishlist - TODO: Implement
                btn.querySelector('i').classList.toggle('far');
                btn.querySelector('i').classList.toggle('fas');
                showToast('Added to wishlist!', 'success');
            }
        });
    }

    // ============================================
    // UTILITIES
    // ============================================
    function getAntiForgeryToken() {
        const token = document.querySelector('input[name="__RequestVerificationToken"]');
        if (!token) {
            console.warn('⚠️ Anti-forgery token not found in DOM');
            return '';
        }
        return token.value;
    }

    // ============================================
    // CSS ANIMATION FOR CART BADGE
    // ============================================
    const style = document.createElement('style');
    style.textContent = `
        @keyframes cartBounce {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.3); }
        }
    `;
    document.head.appendChild(style);

})();