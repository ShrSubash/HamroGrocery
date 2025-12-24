using System.ComponentModel.DataAnnotations;
namespace HamroGrocery.Models.ViewModels
{
    public class CreatePurchaseOrderViewModel
    {
        [Required]
        [Display(Name = "PO Number")]
        public string PONumber { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Supplier")]
        public int SupplierId { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        public DateTime OrderDate { get; set; }

        [Display(Name = "Expected Delivery Date")]
        public DateTime? ExpectedDeliveryDate { get; set; }

        [Display(Name = "Notes")]
        public string? Notes { get; set; }

        public List<PurchaseOrderItemViewModel> Items { get; set; } = new();
    }

}