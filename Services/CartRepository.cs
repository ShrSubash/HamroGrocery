// Services/CartRepository.cs
using HamroGrocery.Models;
using HamroGrocery.Services.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace HamroGrocery.Services
{
    /// <summary>
    /// Session-based cart repository implementation
    /// Stores cart in user session for both authenticated and guest users
    /// </summary>
    public class CartRepository : ICartRepository
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartRepository> _logger;
        private const string CART_SESSION_KEY = "UserCart";

        public CartRepository(
            IHttpContextAccessor httpContextAccessor,
            ILogger<CartRepository> logger)
        {
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public Task<Cart> GetCartAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session is null when retrieving cart");
                    return Task.FromResult(CreateNewCart());
                }

                var cartJson = session.GetString(CART_SESSION_KEY);

                if (string.IsNullOrEmpty(cartJson))
                {
                    _logger.LogDebug("No cart found in session, creating new cart");
                    return Task.FromResult(CreateNewCart());
                }

                var cart = JsonSerializer.Deserialize<Cart>(cartJson);
                if (cart == null)
                {
                    _logger.LogWarning("Failed to deserialize cart, creating new cart");
                    return Task.FromResult(CreateNewCart());
                }

                _logger.LogDebug("Retrieved cart from session: {ItemCount} items", cart.Items.Count);
                return Task.FromResult(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cart from session");
                return Task.FromResult(CreateNewCart());
            }
        }

        public Task SaveCartAsync(Cart cart)
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogError("Session is null when saving cart");
                    return Task.CompletedTask;
                }

                cart.Touch(); // Update timestamp

                var options = new JsonSerializerOptions
                {
                    WriteIndented = false,
                    PropertyNameCaseInsensitive = true
                };

                var cartJson = JsonSerializer.Serialize(cart, options);
                session.SetString(CART_SESSION_KEY, cartJson);

                _logger.LogDebug("Saved cart to session: {ItemCount} items, Total: {Total:C}",
                    cart.Items.Count, cart.CartTotal);

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving cart to session");
                throw;
            }
        }

        public Task ClearCartAsync()
        {
            try
            {
                var session = _httpContextAccessor.HttpContext?.Session;
                if (session == null)
                {
                    _logger.LogWarning("Session is null when clearing cart");
                    return Task.CompletedTask;
                }

                session.Remove(CART_SESSION_KEY);
                _logger.LogInformation("Cart cleared from session");

                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart from session");
                throw;
            }
        }

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

        private Cart CreateNewCart()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var isAuthenticated = httpContext?.User?.Identity?.IsAuthenticated ?? false;

            return new Cart
            {
                IsGuestCart = !isAuthenticated,
                GuestSessionId = isAuthenticated ? null : httpContext?.Session?.Id,
                CreatedAt = DateTime.UtcNow
            };
        }
    }
}