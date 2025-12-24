using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    public class ProductSpecialsIndexViewModel
    {
        public List<ProductSpecial> ProductSpecials { get; set; } = new();
        public int TotalSpecials { get; set; }
        public int ActiveSpecials { get; set; }
        public int FeaturedSpecials { get; set; }
        public decimal TotalDiscountValue { get; set; }
    }
}
