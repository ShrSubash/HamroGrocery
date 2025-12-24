// Models/PaymentIntentRequest.cs (Optional if using Payment Element later)
namespace HamroGrocery.Models
{
    public class PaymentIntentRequest
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "aud";
        public string ReceiptEmail { get; set; } = string.Empty;
    }
}