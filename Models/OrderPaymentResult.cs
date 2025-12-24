// Models/OrderPaymentResult.cs
namespace HamroGrocery.Models
{
    public class OrderPaymentResult
    {
        public bool Success { get; set; }
        public string? SessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal AmountPaid { get; set; }
        public string Currency { get; set; } = "aud";
        public DateTime PaidAt { get; set; }
    }
}