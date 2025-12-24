using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    public class Product
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        public int CategoryId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ComparePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Weight { get; set; } 

        [Required]
        public int StockQuantity { get; set; }

        public int ReorderPoint { get; set; } = 10;
        public int MaxStockLevel { get; set; } = 100;
        public string? Unit { get; set; }
        public string? SKU { get; set; }
        public string? Barcode { get; set; }
        public string? ImagePath { get; set; }
        public bool IsFeatured { get; set; } = false;
        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<ProductVariation> ProductVariations { get; set; } = new List<ProductVariation>();
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        public virtual ICollection<ProductSpecial> ProductSpecials { get; set; } = new List<ProductSpecial>();
        public bool IsDeleted { get;  set; } = false;
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}