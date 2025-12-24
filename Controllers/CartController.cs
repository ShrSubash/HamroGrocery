// Controllers/CartController.cs
using HamroGrocery.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HamroGrocery.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly ILogger<CartController> _logger;

        public CartController(ICartService cartService, ILogger<CartController> logger)
        {
            _cartService = cartService;
            _logger = logger;
        }

        // GET: /Cart
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var cart = await _cartService.GetCartAsync();
                return View(cart);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cart");
                TempData["ErrorMessage"] = "Error loading your cart. Please try again.";
                return RedirectToAction("Index", "Home");
            }
        }

        // POST: /Cart/AddToCart
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                _logger.LogInformation("AddToCart called: ProductId={ProductId}, Quantity={Quantity}, VariationId={VariationId}",
                    request.ProductId, request.Quantity, request.VariationId);

                // Validate request
                if (request.ProductId <= 0)
                {
                    return Json(new { success = false, message = "Invalid product ID" });
                }

                if (request.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Quantity must be greater than zero" });
                }

                // Add to cart
                var result = await _cartService.AddToCartAsync(
                    request.ProductId,
                    request.VariationId,
                    request.Quantity
                );

                if (result.Success)
                {
                    var itemCount = await _cartService.GetCartItemCountAsync();

                    return Json(new
                    {
                        success = true,
                        message = result.Message ?? "Item added to cart successfully",
                        cartCount = itemCount,
                        cartTotal = result.Cart?.CartTotal ?? 0
                    });
                }
                else
                {
                    _logger.LogWarning("Failed to add to cart: {Message}", result.Message);
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AddToCart: ProductId={ProductId}", request.ProductId);
                return Json(new
                {
                    success = false,
                    message = "An unexpected error occurred. Please try again."
                });
            }
        }

        // POST: /Cart/UpdateQuantity
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest request)
        {
            try
            {
                var result = await _cartService.UpdateQuantityAsync(
                    request.ProductId,
                    request.VariationId,
                    request.Quantity
                );

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.Message,
                        cartCount = result.Cart?.TotalItems ?? 0,
                        cartSubTotal = result.Cart?.SubTotal ?? 0,
                        cartTax = result.Cart?.TaxAmount ?? 0,
                        cartTotal = result.Cart?.CartTotal ?? 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating quantity");
                return Json(new { success = false, message = "An error occurred while updating quantity" });
            }
        }

        // POST: /Cart/RemoveItem
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem([FromBody] RemoveItemRequest request)
        {
            try
            {
                var result = await _cartService.RemoveFromCartAsync(request.ProductId, request.VariationId);

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.Message ?? "Item removed from cart",
                        cartCount = result.Cart?.TotalItems ?? 0,
                        cartTotal = result.Cart?.CartTotal ?? 0
                    });
                }
                else
                {
                    return Json(new { success = false, message = result.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing item from cart");
                return Json(new { success = false, message = "An error occurred while removing item" });
            }
        }

        // POST: /Cart/Clear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            try
            {
                await _cartService.ClearCartAsync();
                return Json(new { success = true, message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return Json(new { success = false, message = "An error occurred while clearing cart" });
            }
        }

        // GET: /Cart/GetCartCount
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                var count = await _cartService.GetCartItemCountAsync();
                return Json(new { count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart count");
                return Json(new { count = 0 });
            }
        }
    }

    // ============================================
    // REQUEST MODELS
    // ============================================
    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
        public int? VariationId { get; set; }
    }

    public class UpdateQuantityRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? VariationId { get; set; }
    }

    public class RemoveItemRequest
    {
        public int ProductId { get; set; }
        public int? VariationId { get; set; }
    }
}