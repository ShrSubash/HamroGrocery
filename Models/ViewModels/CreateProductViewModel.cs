using System.ComponentModel.DataAnnotations;
namespace HamroGrocery.Models.ViewModels
{ 

    public class CreateProductViewModel
    {
        [Required]
        [Display(Name = "Product Name")]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Required]
        [Display(Name = "Size/Weight")]
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Unit")]
        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Selling Price")]
        [Range(0.01, 999999.99)]
        public decimal Price { get; set; }

        [Required]
        [Display(Name = "Initial Stock Quantity")]
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        [Required]
        [Display(Name = "Reorder Point")]
        [Range(0, int.MaxValue)]
        public int ReorderPoint { get; set; }

        [Required]
        [Display(Name = "Maximum Stock Level")]
        [Range(1, int.MaxValue)]
        public int MaxStockLevel { get; set; }

        // Purchase Order specific fields
        [Display(Name = "Purchase Order")]
        public int? PurchaseOrderId { get; set; }

        [Display(Name = "Order Quantity")]
        [Range(0, int.MaxValue)]
        public int OrderQuantity { get; set; }

        [Display(Name = "Unit Cost")]
        [Range(0.01, 999999.99)]
        public decimal UnitCost { get; set; }
    }
}