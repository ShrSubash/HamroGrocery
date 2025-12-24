using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    public class Complaint
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        public int? OrderId { get; set; }

        [Required]
        public ComplaintType Type { get; set; }

        [Required]
        [StringLength(200)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(2000)]
        public string Description { get; set; } = string.Empty;

        // Store multiple image paths as JSON
        public string? ImagePaths { get; set; }

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
        public ComplaintPriority Priority { get; set; } = ComplaintPriority.Medium;

        [StringLength(2000)]
        public string? AdminResponse { get; set; }

        public int? ResolvedByUserId { get; set; }
        public DateTime? ResolvedAt { get; set; }

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Order? Order { get; set; }
        public virtual User? ResolvedByUser { get; set; }
    }

    public enum ComplaintType
    {
        ProductQuality = 1,
        DeliveryIssue = 2,
        WrongItem = 3,
        DamagedItem = 4,
        MissingItem = 5,
        LateDelivery = 6,
        PaymentIssue = 7,
        CustomerService = 8,
        Other = 9
    }

    public enum ComplaintStatus
    {
        Pending = 1,
        InProgress = 2,
        Resolved = 3,
        Closed = 4,
        Rejected = 5
    }

    public enum ComplaintPriority
    {
        Low = 1,
        Medium = 2,
        High = 3,
        Urgent = 4
    }
}