// Services/StripePaymentService.cs
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HamroGrocery.Services
{
    /// <summary>
    /// Simplified Stripe service - only calls Stripe when actually needed
    /// </summary>
    public class StripePaymentService : IPaymentService
    {
        private readonly string _secretKey;
        private readonly ILogger<StripePaymentService> _logger;

        public StripePaymentService(IConfiguration configuration, ILogger<StripePaymentService> logger)
        {
            _secretKey = configuration["Stripe:SecretKey"] ?? "";
            _logger = logger;
        }

        /// <summary>
        /// Creates a Stripe checkout session with built-in shipping options
        /// </summary>
        public async Task<string> CreateCheckoutSessionAsync(
            List<StripeCheckoutItem> lineItems,
            string successUrl,
            string cancelUrl,
            string? customerEmail = null,
            Dictionary<string, string>? metadata = null)
        {
            try
            {
                // Validate Stripe key
                if (string.IsNullOrEmpty(_secretKey) || !_secretKey.StartsWith("sk_"))
                {
                    throw new InvalidOperationException("Stripe Secret Key is not configured. Please add it to appsettings.json");
                }

                // Configure Stripe with the secret key
                StripeConfiguration.ApiKey = _secretKey;

                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string> { "card" },
                    Mode = "payment",

                    LineItems = lineItems.Select(item => new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = item.Currency,
                            UnitAmount = item.Amount,
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Name,
                                Description = item.Description,
                                Images = item.ImageUrl != null ? new List<string> { item.ImageUrl } : null
                            }
                        },
                        Quantity = item.Quantity
                    }).ToList(),

                    SuccessUrl = successUrl + "?session_id={CHECKOUT_SESSION_ID}",
                    CancelUrl = cancelUrl,
                    CustomerEmail = customerEmail,

                    // Simple shipping address collection (Stripe handles the UI)
                    ShippingAddressCollection = new SessionShippingAddressCollectionOptions
                    {
                        AllowedCountries = new List<string> { "AU" }
                    },

                    Metadata = metadata ?? new Dictionary<string, string>()
                };

                var service = new SessionService();
                var session = await service.CreateAsync(options);

                _logger.LogInformation("Created Stripe checkout session: {SessionId}", session.Id);

                return session.Id;
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe API error while creating checkout session");
                throw new InvalidOperationException($"Payment service error: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Stripe checkout session");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a completed Stripe session
        /// </summary>
        public async Task<Session> RetrieveSessionAsync(string sessionId)
        {
            try
            {
                if (string.IsNullOrEmpty(_secretKey))
                {
                    throw new InvalidOperationException("Stripe Secret Key is not configured");
                }

                StripeConfiguration.ApiKey = _secretKey;

                var service = new SessionService();
                return await service.GetAsync(sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Stripe session: {SessionId}", sessionId);
                throw;
            }
        }
    }
}