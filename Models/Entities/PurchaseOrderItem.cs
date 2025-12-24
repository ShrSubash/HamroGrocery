using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    /// <summary>
    /// Purchase Order Item - individual line item in a purchase order
    /// </summary>
    public class PurchaseOrderItem
    {
        public int Id { get; set; }

        [Required]
        public int PurchaseOrderId { get; set; }

        [Required]
        public int ProductVariationId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Received quantity cannot be negative")]
        public int ReceivedQuantity { get; set; } = 0;

        public DateTime? ReceivedDate { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Unit price must be greater than 0")]
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Alias for UnitPrice - provides compatibility with code that uses UnitCost
        /// This is NOT stored in the database
        /// </summary>
        [NotMapped]
        public decimal UnitCost
        {
            get => UnitPrice;
            set => UnitPrice = value;
        }

        /// <summary>
        /// Calculated property: Quantity * UnitPrice
        /// This is computed at runtime and NOT stored in the database
        /// </summary>
        [NotMapped]
        public decimal TotalCost => Quantity * UnitPrice;

        [StringLength(200)]
        public string? Notes { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual PurchaseOrder PurchaseOrder { get; set; } = null!;

        public virtual ProductVariation ProductVariation { get; set; } = null!;
    }
}