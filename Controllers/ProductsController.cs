using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Controllers
{
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductsController> _logger;

        public ProductsController(ApplicationDbContext context, ILogger<ProductsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Products
        public async Task<IActionResult> Index(string? category, string? search, int page = 1)
        {
            try
            {
                // Get all categories for the sidebar
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .Where(c => c.IsActive && !c.IsDeleted)
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.CategoryName)
                    .ToListAsync();

                // Start with base query
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductVariations)
                    .Where(p => p.IsActive && !p.IsDeleted);

                // Filter by category if specified
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category!.CategoryName == category);
                }

                // Filter by search query if specified
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(p =>
                        p.ProductName.Contains(search) ||
                        (p.Description != null && p.Description.Contains(search)));
                }

                // Execute query
                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .ToListAsync();

                // Pass data to view
                ViewBag.Categories = categories;
                ViewBag.CurrentCategory = category;
                ViewBag.SearchQuery = search;

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                return View(new List<Product>());
            }
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            try
            {
                // Load product with all related data
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductVariations.Where(v => v.IsActive && !v.IsDeleted))
                    .Include(p => p.ProductSpecials.Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        s.StartDate <= DateTime.Now &&
                        s.EndDate >= DateTime.Now))
                    .FirstOrDefaultAsync(p => p.Id == id && p.IsActive && !p.IsDeleted);

                if (product == null)
                {
                    return NotFound();
                }

                // Get active special
                var activeSpecial = product.ProductSpecials?.FirstOrDefault();

                // Calculate discounted price
                decimal discountedPrice = product.Price;
                decimal savingsAmount = 0;
                int savingsPercent = 0;

                if (activeSpecial != null)
                {
                    if (activeSpecial.DiscountType == DiscountType.Percentage)
                    {
                        discountedPrice = product.Price * (1 - activeSpecial.DiscountValue / 100);
                        savingsAmount = product.Price - discountedPrice;
                        savingsPercent = (int)activeSpecial.DiscountValue;
                    }
                    else if (activeSpecial.DiscountType == DiscountType.FixedAmount)
                    {
                        discountedPrice = product.Price - activeSpecial.DiscountValue;
                        savingsAmount = activeSpecial.DiscountValue;
                        savingsPercent = (int)((savingsAmount / product.Price) * 100);
                    }

                    // Ensure price doesn't go below zero
                    if (discountedPrice < 0) discountedPrice = 0;
                }

                // Get related products (same category, excluding current product)
                var relatedProducts = await _context.Products
                    .Where(p => p.CategoryId == product.CategoryId &&
                                p.Id != product.Id &&
                                p.IsActive &&
                                !p.IsDeleted &&
                                p.StockQuantity > 0)
                    .OrderBy(p => Guid.NewGuid()) // Random order
                    .Take(4)
                    .ToListAsync();

                // Create view model
                var viewModel = new ProductDetailsViewModel
                {
                    Product = product,
                    Variations = product.ProductVariations?.ToList() ?? new List<ProductVariation>(),
                    ActiveSpecial = activeSpecial,
                    DiscountedPrice = discountedPrice,
                    SavingsAmount = savingsAmount,
                    SavingsPercent = savingsPercent,
                    RelatedProducts = relatedProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading product details for product ID {ProductId}", id);
                return StatusCode(500, "An error occurred while loading the product details.");
            }
        }

        // GET: Products/Search
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return RedirectToAction(nameof(Index));
            }

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive &&
                            !p.IsDeleted &&
                            (p.ProductName.Contains(query) ||
                             (p.Description != null && p.Description.Contains(query))))
                .ToListAsync();

            ViewData["SearchQuery"] = query;

            // Get categories for sidebar
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.CategoryName)
                .ToListAsync();

            ViewBag.Categories = categories;

            return View("Index", products);
        }
    }
}