using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.Entities
{
    /// <summary>
    /// System-wide announcements/notifications shown to users
    /// </summary>
    public class Announcement
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Type of announcement: Info, Warning, Success, Error
        /// </summary>
        [Required]
        public AnnouncementType Type { get; set; } = AnnouncementType.Info;

        /// <summary>
        /// Where to show: HomePage, AllPages, Checkout
        /// </summary>
        [Required]
        public AnnouncementLocation Location { get; set; } = AnnouncementLocation.AllPages;

        public DateTime StartDate { get; set; } = DateTime.UtcNow;

        public DateTime? EndDate { get; set; }

        /// <summary>
        /// Display priority (higher numbers show first)
        /// </summary>
        public int Priority { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsDeleted { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Check if announcement is currently valid
        /// </summary>
        public bool IsCurrentlyActive()
        {
            if (!IsActive || IsDeleted) return false;

            var now = DateTime.UtcNow;
            if (now < StartDate) return false;
            if (EndDate.HasValue && now > EndDate.Value) return false;

            return true;
        }
    }

    public enum AnnouncementType
    {
        Info = 0,      // Blue - general information
        Warning = 1,   // Yellow - warnings, delivery delays
        Success = 2,   // Green - promotions, good news
        Error = 3      // Red - critical issues, service down
    }

    public enum AnnouncementLocation
    {
        HomePage = 0,      // Show only on homepage
        AllPages = 1,      // Show on all pages
        Checkout = 2,      // Show only during checkout
        ProductPages = 3   // Show on product pages
    }
}