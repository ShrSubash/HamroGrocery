using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new();
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public decimal TotalValue { get; set; }
    }
}
