// wwwroot/js/stripe-checkout.js
const stripe = Stripe('pk_test_51YOURPUBLISHABLEKEYHERE'); // Replace with your key

document.addEventListener('DOMContentLoaded', () => {
    const checkoutBtn = document.getElementById('checkout-btn');
    if (!checkoutBtn) return;

    checkoutBtn.addEventListener('click', async () => {
        checkoutBtn.disabled = true;
        checkoutBtn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> Redirecting to Stripe...';

        try {
            const response = await fetch('/checkout/create-session', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value
                }
            });

            const { sessionId, error } = await response.json();

            if (error) {
                showToast('error', error);
                checkoutBtn.disabled = false;
                checkoutBtn.innerHTML = '<i class="fas fa-lock"></i> Pay Securely with Stripe';
                return;
            }

            // Redirect to Stripe Checkout
            const result = await stripe.redirectToCheckout({ sessionId });
            if (result.error) {
                showToast('error', result.error.message);
            }
        } catch (err) {
            showToast('error', 'Network error. Please try again.');
            checkoutBtn.disabled = false;
            checkoutBtn.innerHTML = '<i class="fas fa-lock"></i> Pay Securely with Stripe';
        }
    });
});

// Reuse your existing toast
function showToast(type, message) {
    // ... your existing toast code
}