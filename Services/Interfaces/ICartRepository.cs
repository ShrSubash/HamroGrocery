// Services/Interfaces/ICartRepository.cs
using HamroGrocery.Models;

namespace HamroGrocery.Services.Interfaces
{
    /// <summary>
    /// Repository interface for cart persistence operations
    /// Follows Repository Pattern for data access abstraction
    /// </summary>
    public interface ICartRepository
    {
        /// <summary>
        /// Retrieves the current user's cart
        /// </summary>
        /// <returns>Cart object, never null (returns empty cart if none exists)</returns>
        Task<Cart> GetCartAsync();

        /// <summary>
        /// Saves the cart to storage
        /// </summary>
        /// <param name="cart">Cart to save</param>
        Task SaveCartAsync(Cart cart);

        /// <summary>
        /// Clears the cart from storage
        /// </summary>
        Task ClearCartAsync();

        /// <summary>
        /// Gets the cart item count for display (e.g., navbar badge)
        /// </summary>
        Task<int> GetCartItemCountAsync();
    }
}