using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models;
using HamroGrocery.Models.ViewModels;
using HamroGrocery.Models.Entities;
using System.Diagnostics;

namespace HamroGrocery.Controllers
{
    /// <summary>
    /// Main homepage controller for user-facing store front
    /// </summary>
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Main homepage with featured products and announcements
        /// </summary>
        public async Task<IActionResult> Index()
        {
            

            try
            {
                var now = DateTime.UtcNow;

                // Get active announcements for homepage
                var announcements = await _context.Announcements
                    .Where(a => a.IsActive && !a.IsDeleted
                        && a.StartDate <= now
                        && (!a.EndDate.HasValue || a.EndDate.Value >= now)
                        && (a.Location == AnnouncementLocation.HomePage || a.Location == AnnouncementLocation.AllPages))
                    .OrderByDescending(a => a.Priority)
                    .ThenByDescending(a => a.CreatedAt)
                    .ToListAsync();

                // Get active categories ordered by DisplayOrder (for navigation)
                var categories = await _context.Categories
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .Take(10) // Limit to 10 categories in navigation
                    .ToListAsync();

                // Get featured/special products
                var featuredProducts = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductVariations)
                    .Include(p => p.ProductSpecials.Where(ps =>
                        ps.IsActive && !ps.IsDeleted
                        && ps.StartDate <= now
                        && ps.EndDate >= now))
                    .Where(p => p.IsActive && !p.IsDeleted && p.IsFeatured)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync();

                // If no featured products, get latest products
                if (!featuredProducts.Any())
                {
                    featuredProducts = await _context.Products
                        .Include(p => p.Category)
                        .Include(p => p.ProductVariations)
                        .Where(p => p.IsActive && !p.IsDeleted)
                        .OrderByDescending(p => p.CreatedAt)
                        .Take(8)
                        .ToListAsync();
                }

                // Get statistics
                var stats = new HomeStatistics
                {
                    TotalProducts = await _context.Products.CountAsync(p => p.IsActive && !p.IsDeleted),
                    TotalCategories = await _context.Categories.CountAsync(c => c.IsActive && !c.IsDeleted),
                    TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                    ActiveSpecials = await _context.ProductSpecials.CountAsync(ps =>
                        ps.IsActive && !ps.IsDeleted
                        && ps.StartDate <= now
                        && ps.EndDate >= now)
                };

                var viewModel = new HomePageViewModel
                {
                    Announcements = announcements,
                    FeaturedProducts = featuredProducts,
                    Categories = categories, // NEW: Pass categories to view
                    Statistics = stats
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading homepage");
                return View(new HomePageViewModel());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}