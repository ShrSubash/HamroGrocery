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
        public async Task<IActionResult> Index(string? category, string? search, string? sortBy, int page = 1, int pageSize = 24)
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
                    .Include(p => p.ProductSpecials.Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        s.StartDate <= DateTime.Now &&
                        s.EndDate >= DateTime.Now))
                    .Where(p => p.IsActive && !p.IsDeleted);

                // Filter by category if specified
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category!.CategoryName == category);
                }

                // Filter by search query if specified
                if (!string.IsNullOrEmpty(search))
                {
                    var searchTerm = search.ToLower();
                    query = query.Where(p =>
                        p.ProductName.ToLower().Contains(searchTerm) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                        (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm)));
                }

                // Apply sorting
                query = sortBy switch
                {
                    "price-low" => query.OrderBy(p => p.Price),
                    "price-high" => query.OrderByDescending(p => p.Price),
                    "name" => query.OrderBy(p => p.ProductName),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    _ => query.OrderByDescending(p => p.IsFeatured)
                              .ThenByDescending(p => p.CreatedAt)
                };

                // Get total count before pagination
                var totalProducts = await query.CountAsync();

                // Execute query with pagination
                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Pass data to view
                ViewBag.Categories = categories;
                ViewBag.CurrentCategory = category;
                ViewBag.SearchQuery = search;
                ViewBag.SortBy = sortBy;
                ViewBag.CurrentPage = page;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.TotalPages = (int)Math.Ceiling(totalProducts / (double)pageSize);

                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products list");
                TempData["ErrorMessage"] = "An error occurred while loading products.";
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
                    .Include(p => p.Category)
                    .Include(p => p.ProductSpecials.Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        s.StartDate <= DateTime.Now &&
                        s.EndDate >= DateTime.Now))
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

            var searchTerm = query.ToLower();
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductSpecials.Where(s =>
                    s.IsActive &&
                    !s.IsDeleted &&
                    s.StartDate <= DateTime.Now &&
                    s.EndDate >= DateTime.Now))
                .Where(p => p.IsActive &&
                            !p.IsDeleted &&
                            (p.ProductName.ToLower().Contains(searchTerm) ||
                             (p.Description != null && p.Description.ToLower().Contains(searchTerm)) ||
                             (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm))))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
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
            ViewBag.SearchQuery = query;

            return View("Index", products);
        }

        // ============================================
        // NEW: QUICK VIEW MODAL (AJAX)
        // ============================================
        /// <summary>
        /// Returns partial view for quick view modal
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> QuickView(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
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

                // Calculate discounted price
                var activeSpecial = product.ProductSpecials?.FirstOrDefault();
                decimal discountedPrice = product.Price;
                decimal savingsPercent = 0;

                if (activeSpecial != null)
                {
                    if (activeSpecial.DiscountType == DiscountType.Percentage)
                    {
                        discountedPrice = product.Price * (1 - activeSpecial.DiscountValue / 100);
                        savingsPercent = activeSpecial.DiscountValue;
                    }
                    else if (activeSpecial.DiscountType == DiscountType.FixedAmount)
                    {
                        discountedPrice = product.Price - activeSpecial.DiscountValue;
                        savingsPercent = (activeSpecial.DiscountValue / product.Price) * 100;
                    }

                    if (discountedPrice < 0) discountedPrice = 0;
                }

                var viewModel = new ProductQuickViewModel
                {
                    Id = product.Id,
                    ProductName = product.ProductName,
                    Description = product.Description ?? "",
                    ImagePath = product.ImagePath ?? "/images/no-image.jpg",
                    Price = product.Price,
                    DiscountedPrice = discountedPrice,
                    SavingsPercent = savingsPercent,
                    StockQuantity = product.StockQuantity,
                    CategoryName = product.Category?.CategoryName ?? "General",
                    IsOnSale = activeSpecial != null
                };

                return PartialView("_QuickViewModal", viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading quick view for product {ProductId}", id);
                return StatusCode(500, "Failed to load product details");
            }
        }

        // ============================================
        // NEW: SEARCH SUGGESTIONS (AJAX)
        // ============================================
        /// <summary>
        /// Returns search suggestions for autocomplete
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new List<object>());
            }

            try
            {
                var searchTerm = query.ToLower();

                var suggestions = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive &&
                               !p.IsDeleted &&
                               p.StockQuantity > 0 &&
                               (p.ProductName.ToLower().Contains(searchTerm) ||
                                (p.Category != null && p.Category.CategoryName.ToLower().Contains(searchTerm))))
                    .OrderByDescending(p => p.IsFeatured)
                    .Take(8)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        category = p.Category != null ? p.Category.CategoryName : "General",
                        price = p.Price,
                        image = p.ImagePath ?? "/images/no-image.jpg",
                        url = Url.Action("Details", "Products", new { id = p.Id })
                    })
                    .ToListAsync();

                return Json(suggestions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching search suggestions for query: {Query}", query);
                return Json(new List<object>());
            }
        }

        // ============================================
        // NEW: GET PRODUCTS BY CATEGORY (AJAX)
        // ============================================
        /// <summary>
        /// Returns products filtered by category for dynamic loading
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(string category, int page = 1, int pageSize = 12)
        {
            try
            {
                var query = _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSpecials.Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        s.StartDate <= DateTime.Now &&
                        s.EndDate >= DateTime.Now))
                    .Where(p => p.IsActive && !p.IsDeleted);

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(p => p.Category!.CategoryName == category);
                }

                var products = await query
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.Price,
                        image = p.ImagePath ?? "/images/no-image.jpg",
                        category = p.Category != null ? p.Category.CategoryName : "General",
                        stockQuantity = p.StockQuantity,
                        isOnSale = p.ProductSpecials.Any(),
                        url = Url.Action("Details", "Products", new { id = p.Id })
                    })
                    .ToListAsync();

                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching products by category: {Category}", category);
                return Json(new { success = false, message = "Failed to load products" });
            }
        }

        // ============================================
        // NEW: GET FEATURED PRODUCTS (AJAX)
        // ============================================
        /// <summary>
        /// Returns featured products for homepage or promotional sections
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFeaturedProducts(int count = 8)
        {
            try
            {
                var products = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSpecials.Where(s =>
                        s.IsActive &&
                        !s.IsDeleted &&
                        s.StartDate <= DateTime.Now &&
                        s.EndDate >= DateTime.Now))
                    .Where(p => p.IsActive && !p.IsDeleted && p.IsFeatured && p.StockQuantity > 0)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(count)
                    .Select(p => new
                    {
                        id = p.Id,
                        name = p.ProductName,
                        price = p.Price,
                        image = p.ImagePath ?? "/images/no-image.jpg",
                        category = p.Category != null ? p.Category.CategoryName : "General",
                        stockQuantity = p.StockQuantity,
                        isOnSale = p.ProductSpecials.Any(),
                        discount = p.ProductSpecials.FirstOrDefault() != null
                            ? p.ProductSpecials.First().DiscountValue
                            : 0
                    })
                    .ToListAsync();

                return Json(new { success = true, products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching featured products");
                return Json(new { success = false, message = "Failed to load featured products" });
            }
        }
    }
}