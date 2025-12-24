using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// View model for homepage
    /// </summary>
    public class HomePageViewModel
    {
        public List<Announcement> Announcements { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public HomeStatistics Statistics { get; set; } = new();

        public List<Category> Categories { get; set; } = new();
    }

    /// <summary>
    /// Homepage statistics
    /// </summary>
    public class HomeStatistics
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalCustomers { get; set; }
        public int ActiveSpecials { get; set; }
    }

    
}