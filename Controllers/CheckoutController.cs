using HamroGrocery.Data;
using HamroGrocery.Models;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using HamroGrocery.Services;
using HamroGrocery.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HamroGrocery.Controllers
{
    /// <summary>
    /// Handles checkout process for both authenticated users and guests
    /// Supports multiple payment methods: Stripe, PayPal, Afterpay, Cash on Delivery
    /// Supports delivery methods: Home Delivery and Store Pickup
    /// </summary>
    public class CheckoutController : Controller
    {
        private readonly IPaymentService _paymentService;
        private readonly ICartService _cartService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CheckoutController> _logger;

        // Shipping & Fee Constants
        private const decimal FREE_SHIPPING_THRESHOLD = 2000m;
        private const decimal STANDARD_SHIPPING_METRO = 9.99m;
        private const decimal STANDARD_SHIPPING_REGIONAL = 14.99m;
        private const decimal EXPRESS_SHIPPING_METRO = 19.99m;
        private const decimal EXPRESS_SHIPPING_REGIONAL = 29.99m;
        private const decimal COD_FEE = 15.00m;

        // Sydney Metro Postcode Range
        private const int SYDNEY_METRO_MIN = 2000;
        private const int SYDNEY_METRO_MAX = 2239;

        public CheckoutController(
            IPaymentService paymentService,
            ICartService cartService,
            ApplicationDbContext context,
            ILogger<CheckoutController> logger)
        {
            _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
            _cartService = cartService ?? throw new ArgumentNullException(nameof(cartService));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Main Checkout Actions

        /// <summary>
        /// GET: /Checkout/Index
        /// Display checkout page with cart summary and delivery options
        /// SUPPORTS BOTH AUTHENTICATED USERS AND GUESTS
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Checkout page accessed by {UserType}",
                User.Identity?.IsAuthenticated == true ? "authenticated user" : "guest");

            // Get cart (works for both guests and authenticated users)
            var cart = await _cartService.GetCartAsync();

            if (cart == null || cart.IsEmpty)
            {
                _logger.LogWarning("Checkout attempted with empty cart");
                TempData["ErrorMessage"] = "Your cart is empty. Please add items before checkout.";
                return RedirectToAction("Index", "Cart");
            }

            _logger.LogInformation("Cart loaded with {ItemCount} items, Total: {Total:C}",
                cart.TotalItems, cart.CartTotal);

            // Create checkout view model
            var model = new CheckoutViewModel
            {
                Cart = cart,
                DeliveryMethod = DeliveryMethod.Delivery,
                PaymentMethod = PaymentMethod.CreditCard,

                // Default pickup location details
                PickupStoreName = "Hamro Grocery - Main Store",
                PickupStoreAddress = "123 Main Street, Sydney NSW 2000",
                EstimatedPickupTime = DateTime.Now.AddHours(2),

                // Initialize amounts
                ShippingCost = 0m,
                TotalWithShipping = cart.CartTotal
            };

            // Pre-fill information based on user type
            if (User.Identity?.IsAuthenticated == true)
            {
                // Authenticated user - pre-fill from user profile
                await PopulateAuthenticatedUserInfoAsync(model);
            }
            else
            {
                // Guest user - pre-fill from saved guest info (if any)
                await PopulateGuestInfoAsync(model);
            }

            return View(model);
        }

        /// <summary>
        /// POST: /Checkout/CalculateShipping
        /// AJAX endpoint to calculate shipping costs based on postcode
        /// </summary>
        [HttpPost]
        [Route("Checkout/CalculateShipping")]
        public async Task<IActionResult> CalculateShipping([FromBody] ShippingCalculationRequest request)
        {
            _logger.LogInformation("Shipping calculation requested for postcode: {Postcode}", request?.Postcode);

            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(request?.Postcode))
            {
                _logger.LogWarning("Invalid shipping calculation request");
                return Json(new ShippingCalculationResponse
                {
                    Success = false,
                    Message = "Please provide a valid postcode"
                });
            }

            var cart = await _cartService.GetCartAsync();

            // Calculate shipping based on location and cart value
            decimal standardShipping = CalculateShipping(
                cart.SubTotal,
                request.Postcode,
                DeliveryMethod.Delivery
            );

            decimal expressShipping = IsSydneyMetro(request.Postcode)
                ? EXPRESS_SHIPPING_METRO
                : EXPRESS_SHIPPING_REGIONAL;

            bool isFree = cart.SubTotal >= FREE_SHIPPING_THRESHOLD;
            bool isMetro = IsSydneyMetro(request.Postcode);
            int estimatedDays = isMetro ? 2 : 5;

            _logger.LogInformation(
                "Shipping calculated - Metro: {IsMetro}, Free: {IsFree}, Standard: {Standard:C}",
                isMetro, isFree, standardShipping);

            return Json(new ShippingCalculationResponse
            {
                Success = true,
                StandardShipping = standardShipping,
                ExpressShipping = expressShipping,
                EstimatedDays = estimatedDays,
                IsFreeShippingEligible = isFree,
                Message = isFree
                    ? "🎉 Free shipping applied!"
                    : $"Standard shipping: ${standardShipping:F2}"
            });
        }

        /// <summary>
        /// POST: /Checkout/Process
        /// Process checkout for BOTH authenticated users and guests
        /// Creates order or redirects to payment gateway
        /// </summary>
        [HttpPost]
        [Route("Checkout/Process")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProcessCheckout([FromBody] CheckoutViewModel model)
        {
            bool isGuest = User.Identity?.IsAuthenticated != true;
            _logger.LogInformation("Processing checkout for {UserType}, Payment: {PaymentMethod}",
                isGuest ? "GUEST" : "authenticated user", model?.PaymentMethod);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage);

                _logger.LogWarning("Checkout validation failed: {Errors}", string.Join(", ", errors));

                return Json(new
                {
                    success = false,
                    message = "Please fill all required fields correctly"
                });
            }

            var cart = await _cartService.GetCartAsync();

            if (cart == null || cart.IsEmpty)
            {
                _logger.LogWarning("Checkout process attempted with empty cart");
                return Json(new { success = false, message = "Your cart is empty" });
            }

            // Validate delivery address for delivery orders
            if (model.DeliveryMethod == DeliveryMethod.Delivery)
            {
                if (string.IsNullOrWhiteSpace(model.StreetAddress) ||
                    string.IsNullOrWhiteSpace(model.City) ||
                    string.IsNullOrWhiteSpace(model.Postcode))
                {
                    _logger.LogWarning("Delivery checkout missing address information");
                    return Json(new
                    {
                        success = false,
                        message = "Please provide complete delivery address"
                    });
                }
            }

            // ✅ GUEST VALIDATION: Require email for guests
            if (isGuest && string.IsNullOrWhiteSpace(model.Email))
            {
                _logger.LogWarning("Guest checkout attempted without email");
                return Json(new
                {
                    success = false,
                    message = "Email is required for guest checkout"
                });
            }

            // Validate Cash on Delivery restrictions
            if (model.PaymentMethod == PaymentMethod.CashOnDelivery &&
                model.DeliveryMethod != DeliveryMethod.Delivery)
            {
                _logger.LogWarning("COD attempted for non-delivery order");
                return Json(new
                {
                    success = false,
                    message = "Cash on Delivery is only available for home delivery"
                });
            }

            // ✅ ENSURE GUEST USER EXISTS (Critical for guest orders)
            if (isGuest)
            {
                bool guestUserExists = await DbSeeder.EnsureGuestUserExistsAsync(_context, _logger);
                if (!guestUserExists)
                {
                    _logger.LogError("Guest User account not found - cannot process guest checkout");
                    return Json(new
                    {
                        success = false,
                        message = "System error: Guest checkout unavailable. Please contact support."
                    });
                }

                // Save guest info for future use
                await _cartService.SaveGuestInfoAsync(new GuestCheckoutInfo
                {
                    Email = model.Email,
                    FullName = model.FullName,
                    PhoneNumber = model.Phone,
                    ShippingAddress = model.StreetAddress,
                    ShippingCity = model.City,
                    ShippingState = model.State,
                    ShippingPostcode = model.Postcode
                });
            }

            // Calculate final amounts
            decimal shipping = CalculateShipping(cart.SubTotal, model.Postcode, model.DeliveryMethod);
            decimal codFee = model.PaymentMethod == PaymentMethod.CashOnDelivery ? COD_FEE : 0m;
            decimal finalTotal = cart.CartTotal + shipping + codFee;

            _logger.LogInformation(
                "Final amounts - Subtotal: {Subtotal:C}, Shipping: {Shipping:C}, COD: {COD:C}, Total: {Total:C}",
                cart.SubTotal, shipping, codFee, finalTotal);

            try
            {
                // Process based on payment method
                if (model.PaymentMethod == PaymentMethod.CashOnDelivery)
                {
                    // Create COD order immediately (works for guests)
                    int orderId = await CreateCodOrderAsync(model, cart, shipping, isGuest);

                    _logger.LogInformation("COD order created successfully: {OrderId} (Guest: {IsGuest})",
                        orderId, isGuest);

                    return Json(new
                    {
                        success = true,
                        paymentMethod = "cod",
                        orderId,
                        redirectUrl = Url.Action("OrderConfirmation", "Checkout", new { orderId })
                    });
                }
                else
                {
                    // Create Stripe checkout session (works for guests)
                    string sessionId = await CreateStripeSessionAsync(cart, model, shipping, isGuest);

                    _logger.LogInformation("Stripe session created: {SessionId} (Guest: {IsGuest})",
                        sessionId, isGuest);

                    return Json(new
                    {
                        success = true,
                        paymentMethod = "stripe",
                        sessionId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing checkout for {UserType}", isGuest ? "guest" : "user");
                return Json(new
                {
                    success = false,
                    message = "An error occurred processing your order. Please try again."
                });
            }
        }

        /// <summary>
        /// GET: /Checkout/Success?session_id={CHECKOUT_SESSION_ID}
        /// Handle successful Stripe payment and create order
        /// WORKS FOR BOTH GUESTS AND AUTHENTICATED USERS
        /// </summary>
        [HttpGet]
        [Route("Checkout/Success")]
        public async Task<IActionResult> Success(string session_id)
        {
            _logger.LogInformation("Payment success callback received for session: {SessionId}", session_id);

            if (string.IsNullOrEmpty(session_id))
            {
                _logger.LogWarning("Success callback without session ID");
                TempData["ErrorMessage"] = "Invalid payment session";
                return RedirectToAction("Index", "Cart");
            }

            try
            {
                // Retrieve Stripe session to verify payment
                var session = await _paymentService.RetrieveSessionAsync(session_id);

                if (session?.PaymentStatus?.ToLower() == "paid")
                {
                    _logger.LogInformation("Payment confirmed for session: {SessionId}", session_id);

                    // Create order from successful payment
                    var orderId = await CreateOrderFromStripeSessionAsync(session);

                    // Clear cart after successful order
                    await _cartService.ClearCartAsync();

                    _logger.LogInformation("Order created from Stripe payment: {OrderId}", orderId);

                    return RedirectToAction("OrderConfirmation", "Checkout", new { orderId });
                }
                else
                {
                    _logger.LogWarning("Payment not confirmed for session: {SessionId}, Status: {Status}",
                        session_id, session?.PaymentStatus);

                    TempData["ErrorMessage"] = "Payment verification failed. Please contact support.";
                    return RedirectToAction("Index", "Cart");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment success callback");
                TempData["ErrorMessage"] = "An error occurred verifying your payment. Please contact support.";
                return RedirectToAction("Index", "Cart");
            }
        }

        /// <summary>
        /// GET: /Checkout/Confirmation/{orderId}
        /// Display order confirmation page
        /// ACCESSIBLE BY BOTH GUESTS AND AUTHENTICATED USERS
        /// </summary>
        [HttpGet]
        [Route("Checkout/Confirmation/{orderId}")]
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            _logger.LogInformation("Order confirmation page accessed for order: {OrderId}", orderId);

            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Include(o => o.Customer)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("Order not found: {OrderId}", orderId);
                    TempData["ErrorMessage"] = "Order not found";
                    return RedirectToAction("Index", "Home");
                }

                // ✅ SECURITY: Verify order belongs to current user (or is guest order)
                bool isGuest = User.Identity?.IsAuthenticated != true;
                bool isGuestOrder = order.CustomerId == 1; // Guest orders use CustomerId = 1

                if (!isGuest && order.CustomerId != GetCurrentUserId())
                {
                    _logger.LogWarning("Unauthorized access to order {OrderId} by user {UserId}",
                        orderId, GetCurrentUserId());
                    TempData["ErrorMessage"] = "You don't have permission to view this order";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("Displaying confirmation for order: {OrderNumber} (Guest: {IsGuest})",
                    order.OrderNumber, isGuestOrder);

                return View(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order confirmation for order: {OrderId}", orderId);
                TempData["ErrorMessage"] = "Error loading order details";
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Calculate shipping cost based on subtotal, location, and delivery method
        /// </summary>
        private decimal CalculateShipping(decimal subTotal, string postcode, DeliveryMethod method)
        {
            // Pickup has no shipping cost
            if (method == DeliveryMethod.Pickup)
            {
                _logger.LogDebug("Pickup selected - no shipping cost");
                return 0m;
            }

            // Free shipping for orders over threshold
            if (subTotal >= FREE_SHIPPING_THRESHOLD)
            {
                _logger.LogInformation("Free shipping applied for order over ${Threshold}", FREE_SHIPPING_THRESHOLD);
                return 0m;
            }

            // Calculate based on Sydney metro area
            bool isMetro = IsSydneyMetro(postcode);
            decimal shipping = isMetro ? STANDARD_SHIPPING_METRO : STANDARD_SHIPPING_REGIONAL;

            _logger.LogDebug("Shipping calculated: {Shipping:C} (Metro: {IsMetro}, Postcode: {Postcode})",
                shipping, isMetro, postcode);

            return shipping;
        }

        /// <summary>
        /// Check if postcode is in Sydney metropolitan area
        /// </summary>
        private bool IsSydneyMetro(string postcode)
        {
            if (string.IsNullOrWhiteSpace(postcode))
                return false;

            return int.TryParse(postcode, out int p) &&
                   p >= SYDNEY_METRO_MIN &&
                   p <= SYDNEY_METRO_MAX;
        }

        /// <summary>
        /// Pre-fill checkout form with authenticated user information
        /// </summary>
        private async Task PopulateAuthenticatedUserInfoAsync(CheckoutViewModel model)
        {
            try
            {
                int userId = GetCurrentUserId();

                if (userId == 0)
                {
                    _logger.LogWarning("Invalid user ID in claims");
                    return;
                }

                var user = await _context.Users.FindAsync(userId);

                if (user != null)
                {
                    model.FullName = $"{user.FirstName} {user.LastName}".Trim();
                    model.Email = user.Email ?? "";
                    model.Phone = user.PhoneNumber ?? "";

                    // Pre-fill address if available
                    if (!string.IsNullOrWhiteSpace(user.AddressLineOne))
                    {
                        model.StreetAddress = user.AddressLineOne;
                        model.City = user.City?.CityName ?? "";
                        model.Postcode = user.PostalCode ?? "";
                    }

                    _logger.LogInformation("User info populated for checkout: {Email}", user.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating user information");
                // Don't throw - just continue with empty fields
            }
        }

        /// <summary>
        /// ✅ FIXED: Pre-fill checkout form with saved guest information
        /// </summary>
        private async Task PopulateGuestInfoAsync(CheckoutViewModel model)
        {
            try
            {
                var guestInfo = await _cartService.GetGuestInfoAsync();

                if (guestInfo != null)
                {
                    model.FullName = guestInfo.FullName ?? "";
                    model.Email = guestInfo.Email ?? "";
                    model.Phone = guestInfo.PhoneNumber ?? "";
                    model.StreetAddress = guestInfo.ShippingAddress ?? "";
                    model.City = guestInfo.ShippingCity ?? "";
                    model.State = guestInfo.ShippingState ?? "";
                    model.Postcode = guestInfo.ShippingPostcode ?? "";

                    _logger.LogInformation("Guest info pre-filled from session for email: {Email}",
                        guestInfo.Email);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating guest information");
                // Don't throw - just continue with empty fields
            }
        }

        /// <summary>
        /// Create Cash on Delivery order for authenticated users or guests
        /// </summary>
        private async Task<int> CreateCodOrderAsync(
            CheckoutViewModel model,
            Cart cart,
            decimal shipping,
            bool isGuest)
        {
            try
            {
                // ✅ GET CUSTOMER ID: Use Guest User (ID=1) for guests, actual user for authenticated
                int customerId = isGuest ? 1 : GetCurrentUserId();

                if (customerId == 0)
                {
                    throw new InvalidOperationException("Cannot determine customer ID for order");
                }

                // Generate unique order number
                var orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}";

                // Prepare shipping address
                string shippingAddress = model.DeliveryMethod == DeliveryMethod.Delivery
                    ? $"{model.StreetAddress}, {model.City} {model.Postcode}"
                    : model.PickupStoreAddress ?? "Pickup at store";

                // Create order entity
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,

                    // Customer details (works for both guests and authenticated users)
                    CustomerId = customerId,
                    CustomerName = model.FullName,
                    CustomerEmail = model.Email,
                    CustomerPhone = model.Phone,

                    // Delivery details
                    DeliveryMethod = model.DeliveryMethod.ToString(),
                    ShippingAddress = shippingAddress,
                    ShippingSuburb = model.Suburb,
                    ShippingCity = model.City,
                    ShippingState = model.State,
                    ShippingPostcode = model.Postcode,
                    DeliveryInstructions = model.DeliveryInstructions,

                    // Payment details
                    PaymentMethod = "Cash on Delivery",
                    PaymentStatus = "Pending",

                    // Amounts
                    SubTotal = cart.SubTotal,
                    TaxAmount = cart.TaxAmount,
                    ShippingAmount = shipping + COD_FEE,
                    TotalAmount = cart.CartTotal + shipping + COD_FEE,

                    // Order items
                    OrderItems = cart.Items.Select(item => new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductVariationId = item.ProductVariationId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        CreatedAt = DateTime.UtcNow
                    }).ToList(),

                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Clear cart after successful order
                await _cartService.ClearCartAsync();

                _logger.LogInformation(
                    "COD Order created: {OrderNumber}, Customer: {CustomerId} (Guest: {IsGuest}), Total: {Total:C}",
                    orderNumber, customerId, isGuest, order.TotalAmount);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating COD order for {UserType}",
                    isGuest ? "guest" : "authenticated user");
                throw;
            }
        }

        /// <summary>
        /// Create Stripe checkout session for online payment (guests and authenticated users)
        /// </summary>
        private async Task<string> CreateStripeSessionAsync(
            Cart cart,
            CheckoutViewModel model,
            decimal shipping,
            bool isGuest)
        {
            try
            {
                // Prepare line items for Stripe
                var items = cart.Items.Select(item => new StripeCheckoutItem
                {
                    Name = item.ProductName,
                    Amount = (long)(item.UnitPrice * 100), // Convert to cents
                    Quantity = item.Quantity,
                    Currency = "aud"
                }).ToList();

                // Add shipping as line item if applicable
                if (shipping > 0)
                {
                    items.Add(new StripeCheckoutItem
                    {
                        Name = "Shipping Fee",
                        Amount = (long)(shipping * 100),
                        Quantity = 1,
                        Currency = "aud"
                    });
                }

                // Prepare URLs
                var successUrl = Url.Action("Success", "Checkout", null, Request.Scheme)!
                    + "?session_id={CHECKOUT_SESSION_ID}";
                var cancelUrl = Url.Action("Index", "Checkout", null, Request.Scheme)!;

                // ✅ METADATA: Store order details for post-payment processing
                var metadata = new Dictionary<string, string>
                {
                    { "IsGuest", isGuest.ToString() },
                    { "CustomerName", model.FullName },
                    { "CustomerEmail", model.Email },
                    { "CustomerPhone", model.Phone },
                    { "DeliveryMethod", model.DeliveryMethod.ToString() },
                    { "ShippingAddress", $"{model.StreetAddress}, {model.City} {model.Postcode}" },
                    { "DeliveryInstructions", model.DeliveryInstructions ?? "" }
                };

                if (!isGuest)
                {
                    metadata["UserId"] = GetCurrentUserId().ToString();
                }

                // Create Stripe session with metadata
                var sessionId = await _paymentService.CreateCheckoutSessionAsync(
                    lineItems: items,
                    successUrl: successUrl,
                    cancelUrl: cancelUrl,
                    customerEmail: model.Email,
                    metadata: metadata
                );

                _logger.LogInformation(
                    "Stripe session created: {SessionId}, Amount: {Amount:C}, Guest: {IsGuest}",
                    sessionId, cart.CartTotal + shipping, isGuest);

                return sessionId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe checkout session for {UserType}",
                    isGuest ? "guest" : "user");
                throw;
            }
        }

        /// <summary>
        /// Create order from successful Stripe payment session
        /// HANDLES BOTH GUEST AND AUTHENTICATED USER ORDERS
        /// </summary>
        private async Task<int> CreateOrderFromStripeSessionAsync(Stripe.Checkout.Session session)
        {
            try
            {
                _logger.LogInformation("Creating order from Stripe session: {SessionId}", session.Id);

                // Extract metadata from session
                var metadata = session.Metadata ?? new Dictionary<string, string>();

                // ✅ DETERMINE IF GUEST ORDER
                bool isGuest = metadata.GetValueOrDefault("IsGuest") == "True";

                // Get customer ID (guest = 1, or parse from metadata)
                int customerId = 1; // Default to guest
                if (!isGuest && metadata.TryGetValue("UserId", out string? userIdStr))
                {
                    if (int.TryParse(userIdStr, out int userId))
                    {
                        customerId = userId;
                    }
                }

                // Generate order number
                var orderNumber = $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}{new Random().Next(100, 999)}";

                // Create order from Stripe session
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Processing,

                    // Customer details from metadata
                    CustomerId = customerId,
                    CustomerName = metadata.GetValueOrDefault("CustomerName") ?? "",
                    CustomerEmail = session.CustomerEmail ?? metadata.GetValueOrDefault("CustomerEmail") ?? "",
                    CustomerPhone = metadata.GetValueOrDefault("CustomerPhone") ?? "",

                    // Delivery details
                    DeliveryMethod = metadata.GetValueOrDefault("DeliveryMethod") ?? "Delivery",
                    ShippingAddress = metadata.GetValueOrDefault("ShippingAddress") ?? "",
                    DeliveryInstructions = metadata.GetValueOrDefault("DeliveryInstructions"),

                    // Payment details
                    PaymentMethod = "Online Payment (Stripe)",
                    PaymentStatus = "Paid",
                    StripeSessionId = session.Id,
                    StripePaymentIntentId = session.PaymentIntentId,

                    // Amounts (convert from cents)
                    TotalAmount = (session.AmountTotal ?? 0) / 100m,

                    ProcessedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Order created from Stripe session: {OrderNumber}, Customer: {CustomerId} (Guest: {IsGuest})",
                    orderNumber, customerId, isGuest);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order from Stripe session");
                throw;
            }
        }

        /// <summary>
        /// Get current authenticated user ID (returns 0 if not authenticated)
        /// </summary>
        private int GetCurrentUserId()
        {
            if (User.Identity?.IsAuthenticated != true)
                return 0;

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        #endregion
    }
}