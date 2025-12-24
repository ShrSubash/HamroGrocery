using HamroGrocery.Models.Entities;
using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Areas.Admin.ViewModels
{
    public class CreateProductViewModel
    {
        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Price { get; set; }

        [Required]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderPoint { get; set; } = 10;

        [Range(0, int.MaxValue)]
        public int MaxStockLevel { get; set; } = 100;

        [StringLength(50)]
        public string? Size { get; set; } = "Standard";

        [StringLength(50)]
        public string? Unit { get; set; }

        // For purchase order integration
        public int? PurchaseOrderId { get; set; }
        public int OrderQuantity { get; set; }
        public decimal UnitCost { get; set; }
    }
}