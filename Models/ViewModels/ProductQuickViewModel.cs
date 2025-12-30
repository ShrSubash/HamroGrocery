namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Quick View Modal
    /// </summary>
    public class ProductQuickViewModel
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = "/images/no-image.jpg";
        public decimal Price { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal SavingsPercent { get; set; }
        public int StockQuantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public bool IsOnSale { get; set; }
    }
}