using System.ComponentModel.DataAnnotations;
namespace HamroGrocery.Models.ViewModels
{
    public class PurchaseOrderItemViewModel
    {
        public int ProductVariationId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public decimal TotalCost => Quantity * UnitCost;
    }

}