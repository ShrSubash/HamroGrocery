using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    public class ProductVariation
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(50)]
        public string Size { get; set; } = string.Empty;

        // ADD MISSING VariationName PROPERTY:
        [StringLength(100)]
        public string? VariationName { get; set; }

        public string? Unit { get; set; }
        public int ReorderPoint { get; set; } = 10;
        public int MaxStockLevel { get; set; } = 100;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; }

        [Required]
        public int StockQuantity { get; set; }

        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Product Product { get; set; } = null!;
        public virtual ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
        public virtual ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // <-- add this
        public bool IsDeleted { get; set; } = false;
    }
}