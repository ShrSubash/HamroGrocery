// Services/SessionCartRepository.cs
using HamroGrocery.Models;
using HamroGrocery.Services.Interfaces;
using System.Text.Json;

namespace HamroGrocery.Services
{
    /// <summary>
    /// Session-based implementation of cart persistence
    /// Uses HTTP Session for temporary cart storage
    /// </summary>
    public class SessionCartRepository : ICartRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SessionCartRepository> _logger;
        private const string CartSessionKey = "HamroGrocery_Cart";

        public SessionCartRepository(
            IHttpContextAccessor httpContextAccessor,
            ILogger<SessionCartRepository> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves cart from session storage
        /// </summary>
        public async Task<Cart> GetCartAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session is not available, returning empty cart");
                    return new Cart();
                }

                var cartJson = session.GetString(CartSessionKey);

                if (string.IsNullOrEmpty(cartJson))
                {
                    _logger.LogDebug("No cart found in session, creating new cart");
                    return new Cart();
                }

                var cart = JsonSerializer.Deserialize<Cart>(cartJson);
                _logger.LogDebug("Cart retrieved from session with {ItemCount} items", cart?.Items.Count ?? 0);

                return cart ?? new Cart();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart from session");
                return new Cart(); // Return empty cart on error
            }
        }

        /// <summary>
        /// Saves cart to session storage
        /// </summary>
        public async Task SaveCartAsync(Cart cart)
        {
            try
            {
                ArgumentNullException.ThrowIfNull(cart);

                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogError("Cannot save cart: Session is not available");
                    throw new InvalidOperationException("Session is not available");
                }

                cart.Touch(); // Update timestamp

                var cartJson = JsonSerializer.Serialize(cart);
                session.SetString(CartSessionKey, cartJson);

                _logger.LogDebug("Cart saved to session with {ItemCount} items, Total: {Total:C}",
                    cart.Items.Count, cart.CartTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cart to session");
                throw;
            }
        }

        /// <summary>
        /// Clears cart from session storage
        /// </summary>
        public async Task ClearCartAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session != null)
                {
                    session.Remove(CartSessionKey);
                    _logger.LogInformation("Cart cleared from session");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart from session");
                throw;
            }
        }

        /// <summary>
        /// Gets cart item count for navbar display
        /// </summary>
        public async Task<int> GetCartItemCountAsync()
        {
            try
            {
                var cart = await GetCartAsync();
                return cart.TotalItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart item count");
                return 0;
            }
        }
    }
}