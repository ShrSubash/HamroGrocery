// Services/Interfaces/ICartService.cs
using HamroGrocery.Models;

namespace HamroGrocery.Services.Interfaces
{
    public interface ICartService
    {
        // Core Cart Operations
        Task<Cart> GetCartAsync();
        Task<CartOperationResult> AddToCartAsync(int productId, int? variationId, int quantity);
        Task<CartOperationResult> UpdateQuantityAsync(int productId, int? variationId, int quantity);
        Task<CartOperationResult> RemoveFromCartAsync(int productId, int? variationId);
        Task ClearCartAsync();

        // Cart Information
        Task<int> GetCartItemCountAsync();
        Task<CartValidationResult> ValidateCartAsync();

        // Guest Cart Operations
        Task SaveGuestInfoAsync(GuestCheckoutInfo guestInfo);
        Task<GuestCheckoutInfo?> GetGuestInfoAsync();
        Task MergeGuestCartOnLoginAsync(int userId);
    }

    /// <summary>
    /// Result of a cart operation
    /// </summary>
    public class CartOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Cart? Cart { get; set; }

        public static CartOperationResult SuccessResult(string message, Cart? cart = null) =>
            new() { Success = true, Message = message, Cart = cart };

        public static CartOperationResult FailureResult(string message) =>
            new() { Success = false, Message = message };
    }

    /// <summary>
    /// Result of cart validation
    /// </summary>
    public class CartValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();

        public static CartValidationResult Valid() =>
            new() { IsValid = true };

        public static CartValidationResult Invalid(params string[] errors) =>
            new() { IsValid = false, Errors = errors.ToList() };

        public void AddError(string error) => Errors.Add(error);
        public void AddWarning(string warning) => Warnings.Add(warning);
    }
}