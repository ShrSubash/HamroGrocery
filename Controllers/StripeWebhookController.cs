// Controllers/StripeWebhookController.cs
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Controllers
{
    /// <summary>
    /// Handles Stripe webhook events for payment processing
    /// </summary>
    [ApiController]
    [Route("api/stripe")]
    public class StripeWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StripeWebhookController> _logger;

        public StripeWebhookController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StripeWebhookController> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Stripe webhook endpoint
        /// Configure in Stripe Dashboard: https://dashboard.stripe.com/webhooks
        /// </summary>
        [HttpPost("webhook")]
        public async Task<IActionResult> HandleWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                // Verify webhook signature
                var stripeEvent = EventUtility.ConstructEvent(
                    json,
                    Request.Headers["Stripe-Signature"],
                    _configuration["Stripe:WebhookSecret"]
                );

                _logger.LogInformation("Processing Stripe webhook event: {EventType}", stripeEvent.Type);

                // Handle different event types using Stripe.Events constants
                switch (stripeEvent.Type)
                {
                    case "checkout.session.completed":
                        await HandleCheckoutSessionCompleted(stripeEvent);
                        break;

                    case "payment_intent.succeeded":
                        await HandlePaymentIntentSucceeded(stripeEvent);
                        break;

                    case "payment_intent.payment_failed":
                        await HandlePaymentIntentFailed(stripeEvent);
                        break;

                    case "charge.refunded":
                        await HandleChargeRefunded(stripeEvent);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event type: {EventType}", stripeEvent.Type);
                        break;
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe webhook signature verification failed");
                return BadRequest("Invalid signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, "Webhook processing error");
            }
        }

        /// <summary>
        /// Handle successful checkout session completion
        /// </summary>
        private async Task HandleCheckoutSessionCompleted(Event stripeEvent)
        {
            var session = stripeEvent.Data.Object as Session;
            if (session == null)
            {
                _logger.LogWarning("CheckoutSession data is null");
                return;
            }

            _logger.LogInformation("Checkout session completed: {SessionId}", session.Id);

            // Check if order already exists (idempotency)
            var existingOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.StripeSessionId == session.Id);

            if (existingOrder != null)
            {
                _logger.LogInformation("Order already exists for session {SessionId}", session.Id);
                return;
            }

            // Get user ID from metadata or use guest
            var metadata = session.Metadata;
            int customerId = 1; // Default guest user

            if (metadata.TryGetValue("UserId", out string? userIdStr) &&
                int.TryParse(userIdStr, out int userId) && userId > 0)
            {
                customerId = userId;
            }

            // Create order from session
            var order = new Order
            {
                OrderNumber = metadata.GetValueOrDefault("OrderId") ?? GenerateOrderNumber(),
                OrderDate = DateTime.UtcNow,
                Status = OrderStatus.Processing,

                // Customer
                CustomerId = customerId,
                CustomerName = metadata.GetValueOrDefault("FullName") ?? "",
                CustomerEmail = metadata.GetValueOrDefault("Email") ?? session.CustomerEmail ?? "",
                CustomerPhone = metadata.GetValueOrDefault("Phone") ?? "",

                // Delivery
                DeliveryMethod = metadata.GetValueOrDefault("DeliveryMethod") ?? "Delivery",
                ShippingAddress = metadata.GetValueOrDefault("DeliveryAddress") ?? "",
                DeliveryInstructions = metadata.GetValueOrDefault("DeliveryInstructions"),
                PickupLocation = metadata.GetValueOrDefault("PickupLocation"),

                // Payment
                PaymentMethod = "Online Payment",
                PaymentStatus = "Paid",
                StripeSessionId = session.Id,
                StripePaymentIntentId = session.PaymentIntentId,

                // Amounts
                TotalAmount = (session.AmountTotal ?? 0) / 100m,

                ProcessedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Order created from webhook: {OrderNumber}", order.OrderNumber);
        }

        /// <summary>
        /// Handle successful payment intent
        /// </summary>
        private async Task HandlePaymentIntentSucceeded(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("PaymentIntent data is null");
                return;
            }

            _logger.LogInformation("Payment succeeded: {PaymentIntentId}, Amount: {Amount}",
                paymentIntent.Id, paymentIntent.Amount / 100m);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.PaymentStatus = "Paid";
                order.Status = OrderStatus.Processing;
                order.ProcessedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} payment confirmed", order.OrderNumber);
            }
        }

        /// <summary>
        /// Handle failed payment
        /// </summary>
        private async Task HandlePaymentIntentFailed(Event stripeEvent)
        {
            var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
            if (paymentIntent == null)
            {
                _logger.LogWarning("PaymentIntent data is null");
                return;
            }

            _logger.LogWarning("Payment failed: {PaymentIntentId}, Reason: {FailureMessage}",
                paymentIntent.Id, paymentIntent.LastPaymentError?.Message);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == paymentIntent.Id);

            if (order != null)
            {
                order.PaymentStatus = "Failed";
                order.Status = OrderStatus.Failed;
                order.Notes = $"Payment failed: {paymentIntent.LastPaymentError?.Message}";

                await _context.SaveChangesAsync();

                _logger.LogWarning("Order {OrderNumber} payment failed", order.OrderNumber);
            }
        }

        /// <summary>
        /// Handle refund
        /// </summary>
        private async Task HandleChargeRefunded(Event stripeEvent)
        {
            var charge = stripeEvent.Data.Object as Charge;
            if (charge == null)
            {
                _logger.LogWarning("Charge data is null");
                return;
            }

            _logger.LogInformation("Charge refunded: {ChargeId}, Amount: {Amount}",
                charge.Id, charge.AmountRefunded / 100m);

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.StripePaymentIntentId == charge.PaymentIntentId);

            if (order != null)
            {
                order.PaymentStatus = "Refunded";
                order.Status = OrderStatus.Refunded;
                order.Notes = $"Refunded: ${charge.AmountRefunded / 100m:N2}";

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderNumber} refunded", order.OrderNumber);
            }
        }

        private string GenerateOrderNumber()
        {
            return $"ORD{DateTime.UtcNow:yyyyMMddHHmmss}";
        }
    }
}