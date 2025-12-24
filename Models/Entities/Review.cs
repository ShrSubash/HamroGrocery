using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int ProductId { get; set; }

        public int? OrderId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(100)]
        public string? Title { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }

        // Store multiple image paths as JSON
        public string? ImagePaths { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;
        public bool IsApproved { get; set; } = false;
        public bool IsFeatured { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        public int HelpfulCount { get; set; } = 0;
        public int UnhelpfulCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties
        public virtual User User { get; set; } = null!;
        public virtual Product Product { get; set; } = null!;
        public virtual Order? Order { get; set; }
    }
}