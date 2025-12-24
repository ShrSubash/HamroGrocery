namespace HamroGrocery.Models.Entities
{
    public class SpecialUploadResult
    {
        public string ProductName { get; set; } = string.Empty;
        public string SpecialName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public decimal? DiscountValue { get; set; }
        public DiscountType? DiscountType { get; set; }
    }
}
