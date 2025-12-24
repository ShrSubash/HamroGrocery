using HamroGrocery.Models.Entities;
using System.ComponentModel.DataAnnotations;
namespace HamroGrocery.Models.ViewModels
{

    public class ProductSpecialViewModel
    {
        [Required]
        [Display(Name = "Product")]
        public int ProductId { get; set; }

        [Required]
        [Display(Name = "Special Name")]
        [StringLength(100)]
        public string SpecialName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Discount Type")]
        public DiscountType DiscountType { get; set; }

        [Required]
        [Display(Name = "Discount Value")]
        [Range(0.01, 999999.99)]
        public decimal DiscountValue { get; set; }

        [Required]
        [Display(Name = "Start Date")]
        public DateTime StartDate { get; set; }

        [Required]
        [Display(Name = "End Date")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Description")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; }

        [Display(Name = "Minimum Quantity")]
        [Range(1, int.MaxValue)]
        public int? MinimumQuantity { get; set; }

        [Display(Name = "Maximum Quantity")]
        [Range(1, int.MaxValue)]
        public int? MaximumQuantity { get; set; }
    }
}