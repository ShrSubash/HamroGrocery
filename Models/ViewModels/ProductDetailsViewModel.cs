using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; } = null!;
        public List<ProductVariation> Variations { get; set; } = new();
        public ProductSpecial? ActiveSpecial { get; set; }
        public decimal DiscountedPrice { get; set; }
        public decimal SavingsAmount { get; set; }
        public int SavingsPercent { get; set; }
        public List<Product> RelatedProducts { get; set; } = new();
        public bool IsInStock => Product?.StockQuantity > 0;
        public bool IsLowStock => Product?.StockQuantity > 0 && Product.StockQuantity <= Product.ReorderPoint;
    }
}