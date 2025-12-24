// Services/IPaymentService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using Stripe.Checkout;  // Required for Session

namespace HamroGrocery.Services
{
    public interface IPaymentService
    {
        Task<string> CreateCheckoutSessionAsync(
            List<StripeCheckoutItem> lineItems,
            string successUrl,
            string cancelUrl,
            string customerEmail = null,
            Dictionary<string, string>? metadata = null);

        Task<Session> RetrieveSessionAsync(string sessionId); 
    }

    public class StripeCheckoutItem
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public long Amount { get; set; } // in cents
        public int Quantity { get; set; } = 1;
        public string Currency { get; set; } = "aud";
        public string? ImageUrl { get; set; }
    }
}