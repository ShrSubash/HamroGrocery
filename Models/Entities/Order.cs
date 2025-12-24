// Models/Entities/Order.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models.Entities
{
    /// <summary>
    /// Enhanced Order entity with checkout fields
    /// Compatible with existing BaseEntity and relationships
    /// </summary>
    public class Order : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        // Order Amounts (existing)
        [Column(TypeName = "decimal(10,2)")]
        public decimal SubTotal { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TaxAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ShippingAmount { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalAmount { get; set; }

        [Required]
        public OrderStatus Status { get; set; } = OrderStatus.Pending;

        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        // ============= NEW CHECKOUT FIELDS =============

        // Customer Contact Details
        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? CustomerEmail { get; set; }

        [Required]
        [StringLength(20)]
        public string CustomerPhone { get; set; } = string.Empty;

        // Delivery Method
        [Required]
        [StringLength(50)]
        public string DeliveryMethod { get; set; } = "Delivery"; // "Delivery" or "Pickup"

        // Shipping Address (existing + enhanced)
        [StringLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? ShippingSuburb { get; set; }

        [StringLength(100)]
        public string ShippingCity { get; set; } = string.Empty;

        [StringLength(50)]
        public string? ShippingState { get; set; }

        [StringLength(10)]
        public string? ShippingPostcode { get; set; }

        [StringLength(20)]
        public string? ShippingPhone { get; set; }

        [StringLength(500)]
        public string? DeliveryInstructions { get; set; }

        // Pickup Details
        [StringLength(200)]
        public string? PickupLocation { get; set; }

        public DateTime? EstimatedPickupTime { get; set; }

        // Delivery Tracking
        public DateTime? EstimatedDeliveryDate { get; set; }

        public DateTime? ActualDeliveryDate { get; set; }

        // Payment Information
        [Required]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = "Online Payment";

        [Required]
        [StringLength(50)]
        public string PaymentStatus { get; set; } = "Pending";

        [StringLength(100)]
        public string? StripeSessionId { get; set; }

        [StringLength(100)]
        public string? StripePaymentIntentId { get; set; }

        // Additional Info
        [StringLength(500)]
        public string? Notes { get; set; }

        // Status Timestamps
        public DateTime? ProcessedAt { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime? CancelledAt { get; set; }

        [StringLength(500)]
        public string? CancellationReason { get; set; }

        // ============= NAVIGATION PROPERTIES =============

        [ForeignKey(nameof(CustomerId))]
        public virtual User? Customer { get; set; }

        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

        public virtual Shipment? Shipment { get; set; }

        // ============= COMPUTED PROPERTIES =============

        [NotMapped]
        public int TotalItems => OrderItems?.Sum(i => i.Quantity) ?? 0;

        [NotMapped]
        public bool IsDelivery => DeliveryMethod == "Delivery";

        [NotMapped]
        public bool IsPickup => DeliveryMethod == "Pickup";

        [NotMapped]
        public bool IsCashOnDelivery => PaymentMethod == "Cash on Delivery";

        [NotMapped]
        public bool IsOnlinePayment => PaymentMethod == "Online Payment";

        [NotMapped]
        public bool IsPaid => PaymentStatus == "Paid";

        [NotMapped]
        public string FullShippingAddress => IsDelivery
            ? $"{ShippingAddress}, {ShippingSuburb}, {ShippingCity}, {ShippingState} {ShippingPostcode}"
            : PickupLocation ?? "Store Pickup";

        // Alias for compatibility with checkout controller
        [NotMapped]
        public decimal ShippingCost
        {
            get => ShippingAmount;
            set => ShippingAmount = value;
        }
        public string? TrackingNumber { get; set; }
    }

    /// <summary>
    /// Enhanced OrderStatus enumeration
    /// </summary>
    public enum OrderStatus
    {
        Pending = 1,
        Confirmed = 2,
        Processing = 3,
        Shipped = 4,
        Delivered = 5,
        Cancelled = 6,
        Failed = 7,      // NEW - Payment failed
        Refunded = 8,
        Completed=9// NEW - Order refunded
    }
}