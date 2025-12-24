// Services/CartService.cs
using HamroGrocery.Data;
using HamroGrocery.Models;
using HamroGrocery.Models.Entities;
using HamroGrocery.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HamroGrocery.Services
{
    public class CartService : ICartService
    {
        private readonly ICartRepository _cartRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartService> _logger;

        public CartService(
            ICartRepository cartRepository,
            ApplicationDbContext context,
            ILogger<CartService> logger)
        {
            _cartRepository = cartRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<Cart> GetCartAsync()
        {
            return await _cartRepository.GetCartAsync();
        }

        public async Task<CartOperationResult> AddToCartAsync(int productId, int? variationId, int quantity)
        {
            try
            {
                if (quantity <= 0)
                    return CartOperationResult.FailureResult("Quantity must be greater than zero");

                var product = await _context.Products
                    .Include(p => p.ProductVariations)
                    .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive && !p.IsDeleted);

                if (product == null)
                    return CartOperationResult.FailureResult("Product not found or unavailable");

                ProductVariation? variation = null;
                int availableStock;
                decimal price;
                string? variationName = null;

                if (variationId.HasValue)
                {
                    variation = product.ProductVariations
                        .FirstOrDefault(v => v.Id == variationId.Value && v.IsActive && !v.IsDeleted);
                    if (variation == null)
                        return CartOperationResult.FailureResult("Product variation not found");

                    availableStock = variation.StockQuantity;
                    price = variation.Price;
                    variationName = $"{variation.Size} {variation.Unit}";
                }
                else
                {
                    availableStock = product.StockQuantity;
                    price = product.Price;
                }

                var cart = await _cartRepository.GetCartAsync();
                var existingItem = cart.FindItem(productId, variationId);
                var totalQuantity = (existingItem?.Quantity ?? 0) + quantity;

                if (totalQuantity > availableStock)
                {
                    return CartOperationResult.FailureResult(
                        $"Only {availableStock} units available. You already have {existingItem?.Quantity ?? 0} in your cart.");
                }

                var cartItem = new CartItem
                {
                    ProductId = productId,
                    ProductVariationId = variationId,
                    ProductName = product.ProductName,
                    VariationName = variationName,
                    UnitPrice = price,
                    Quantity = quantity,
                    ImagePath = product.ImagePath,
                    AvailableStock = availableStock
                };

                cart.AddItem(cartItem);
                await _cartRepository.SaveCartAsync(cart);

                _logger.LogInformation("Added {Quantity}x {ProductName} to cart", quantity, product.ProductName);
                return CartOperationResult.SuccessResult($"{product.ProductName} added to cart", cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product {ProductId} to cart", productId);
                return CartOperationResult.FailureResult("Error adding item to cart");
            }
        }

        public async Task<CartOperationResult> UpdateQuantityAsync(int productId, int? variationId, int quantity)
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                var cartItem = cart.FindItem(productId, variationId);

                if (cartItem == null)
                    return CartOperationResult.FailureResult("Item not found in cart");

                if (quantity <= 0)
                    return await RemoveFromCartAsync(productId, variationId);

                if (quantity > cartItem.AvailableStock)
                    return CartOperationResult.FailureResult($"Only {cartItem.AvailableStock} units available");

                cart.UpdateItemQuantity(productId, quantity, variationId);
                await _cartRepository.SaveCartAsync(cart);

                _logger.LogInformation("Updated {ProductName} quantity to {Quantity}", cartItem.ProductName, quantity);
                return CartOperationResult.SuccessResult("Cart updated", cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart quantity");
                return CartOperationResult.FailureResult("Error updating quantity");
            }
        }

        public async Task<CartOperationResult> RemoveFromCartAsync(int productId, int? variationId)
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                var cartItem = cart.FindItem(productId, variationId);

                if (cartItem == null)
                    return CartOperationResult.FailureResult("Item not found in cart");

                var productName = cartItem.ProductName;
                cart.RemoveItem(productId, variationId);
                await _cartRepository.SaveCartAsync(cart);

                _logger.LogInformation("Removed {ProductName} from cart", productName);
                return CartOperationResult.SuccessResult($"{productName} removed from cart", cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return CartOperationResult.FailureResult("Error removing item");
            }
        }

        public async Task ClearCartAsync()
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                cart.Clear();
                await _cartRepository.SaveCartAsync(cart);
                _logger.LogInformation("Cart cleared");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                throw;
            }
        }

        public async Task<int> GetCartItemCountAsync()
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                return cart.TotalItems;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return 0;
            }
        }

        public async Task<CartValidationResult> ValidateCartAsync()
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                var result = CartValidationResult.Valid();

                if (cart.IsEmpty)
                {
                    result.IsValid = false;
                    result.AddError("Cart is empty");
                    return result;
                }

                foreach (var item in cart.Items)
                {
                    var currentStock = 0;
                    if (item.ProductVariationId.HasValue)
                    {
                        var variation = await _context.ProductVariations
                            .FirstOrDefaultAsync(v => v.Id == item.ProductVariationId.Value);
                        if (variation == null || !variation.IsActive || variation.IsDeleted)
                        {
                            result.AddError($"{item.ProductName} is no longer available");
                            result.IsValid = false;
                            continue;
                        }
                        currentStock = variation.StockQuantity;
                    }
                    else
                    {
                        var product = await _context.Products
                            .FirstOrDefaultAsync(p => p.Id == item.ProductId);
                        if (product == null || !product.IsActive || product.IsDeleted)
                        {
                            result.AddError($"{item.ProductName} is no longer available");
                            result.IsValid = false;
                            continue;
                        }
                        currentStock = product.StockQuantity;
                    }

                    item.AvailableStock = currentStock;

                    if (currentStock == 0)
                        result.AddError($"{item.ProductName} is out of stock");
                    else if (item.Quantity > currentStock)
                        result.AddError($"Only {currentStock} units of {item.ProductName} available");
                    else if (currentStock < 10)
                        result.AddWarning($"Only {currentStock} units of {item.ProductName} remaining");
                }

                if (result.IsValid && !cart.IsEmpty)
                    await _cartRepository.SaveCartAsync(cart);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating cart");
                return CartValidationResult.Invalid("Error validating cart");
            }
        }

        // FINAL: Guest Info using JSON column (Pomelo + MySQL compatible)
        public async Task SaveGuestInfoAsync(GuestCheckoutInfo guestInfo)
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();
                cart.IsGuestCart = true;
                cart.GuestInfoJson = JsonSerializer.Serialize(guestInfo);

                await _cartRepository.SaveCartAsync(cart);
                _logger.LogInformation("Saved guest checkout info for {Email}", guestInfo.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving guest info");
                throw;
            }
        }

        public async Task<GuestCheckoutInfo?> GetGuestInfoAsync()
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();

                if (string.IsNullOrWhiteSpace(cart.GuestInfoJson))
                    return null;

                return JsonSerializer.Deserialize<GuestCheckoutInfo>(cart.GuestInfoJson);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to deserialize guest info - possibly corrupted");
                return null;
            }
        }

        public async Task MergeGuestCartOnLoginAsync(int userId)
        {
            try
            {
                var cart = await _cartRepository.GetCartAsync();

                if (cart.IsEmpty && string.IsNullOrEmpty(cart.GuestInfoJson))
                {
                    _logger.LogInformation("No guest cart to merge for user {UserId}", userId);
                    return;
                }

                _logger.LogInformation("Merging guest cart ({Items} items) for user {UserId}", cart.Items.Count, userId);

                // Mark as authenticated user cart
                cart.IsGuestCart = false;
                cart.GuestSessionId = null;
                cart.GuestInfoJson = null; // Clear guest data after login

                await _cartRepository.SaveCartAsync(cart);
                _logger.LogInformation("Guest cart successfully merged for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error merging guest cart for user {UserId}", userId);
                // Don't throw — login should still succeed
            }
        }
    }
}