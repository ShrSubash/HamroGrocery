// Models/Entities/OrderItem.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    /// <summary>
    /// Enhanced OrderItem with additional fields for checkout
    /// </summary>
    public class OrderItem : BaseEntity
    {
        [Required]
        public int OrderId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? ProductVariationId { get; set; }

        [Required]
        public int Quantity { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalPrice { get; set; }

        // NEW FIELDS for better order tracking
        [Required]
        [StringLength(200)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? SKU { get; set; }

        [StringLength(100)]
        public string? VariationDetails { get; set; }

        [StringLength(100)]
        public string? CategoryName { get; set; }

        // Navigation Properties
        [ForeignKey(nameof(OrderId))]
        public virtual Order Order { get; set; } = null!;

        [ForeignKey(nameof(ProductId))]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey(nameof(ProductVariationId))]
        public virtual ProductVariation? ProductVariation { get; set; }
    }
}