using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using  HamroGrocery.Models.RequestModels;


namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/Products
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Products";

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => !p.IsDeleted) // Add this if you have soft delete
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                TotalProducts = products.Count,
                ActiveProducts = products.Count(p => p.IsActive),
                LowStockProducts = products.Count(p => p.StockQuantity <= p.ReorderPoint),
                OutOfStockProducts = products.Count(p => p.StockQuantity == 0),
                TotalValue = products.Where(p => p.IsActive).Sum(p => p.Price * p.StockQuantity)
            };

            // IMPORTANT: Pass categories for filter dropdown
            ViewBag.Categories = await _context.Categories
                .Where(c => c.IsActive && !c.IsDeleted)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            return View(viewModel);
        }

        // GET: Admin/Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            ViewData["Title"] = $"Product Details - {product.ProductName}";
            return View(product);
        }

        // GET: Admin/Products/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Product";
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();

            var model = new CreateProductDetailedViewModel
            {
                IsActive = true,
                ReorderPoint = 10,
                MaxStockLevel = 100
            };

            return View(model);
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateProductDetailedViewModel model, IFormFile? productImage)
        {
            // Comprehensive debugging
            Console.WriteLine("=== FORM SUBMISSION DEBUG ===");
            Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
            Console.WriteLine($"Model received: {model != null}");

            if (model != null)
            {
                Console.WriteLine($"ProductName: '{model.ProductName}'");
                Console.WriteLine($"Price: {model.Price}");
                Console.WriteLine($"CategoryId: {model.CategoryId}");
                Console.WriteLine($"StockQuantity: {model.StockQuantity}");
                Console.WriteLine($"ReorderPoint: {model.ReorderPoint}");
                Console.WriteLine($"MaxStockLevel: {model.MaxStockLevel}");
                Console.WriteLine($"IsActive: {model.IsActive}");
                Console.WriteLine($"IsFeatured: {model.IsFeatured}");
                Console.WriteLine($"CreateDefaultVariation: {model.CreateDefaultVariation}");
            }

            Console.WriteLine($"ProductImage: {productImage?.FileName ?? "null"}");
            Console.WriteLine($"Categories available: {_context.Categories.Where(c => c.IsActive).Count()}");

            // Check each ModelState error in detail
            if (!ModelState.IsValid)
            {
                Console.WriteLine("=== VALIDATION ERRORS ===");
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}");
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"  Error: {err.ErrorMessage}");
                        if (!string.IsNullOrEmpty(err.Exception?.Message))
                            Console.WriteLine($"  Exception: {err.Exception.Message}");
                    }
                }
                Console.WriteLine("=== END VALIDATION ERRORS ===");
            }

            if (ModelState.IsValid)
            {
                Console.WriteLine("=== ATTEMPTING TO CREATE PRODUCT ===");
                try
                {
                    // Handle image upload
                    string? imagePath = null;
                    if (productImage != null && productImage.Length > 0)
                    {
                        Console.WriteLine($"Processing image: {productImage.FileName}");
                        imagePath = await SaveProductImage(productImage);
                        Console.WriteLine($"Image saved to: {imagePath}");
                    }

                    var product = new Product
                    {
                        ProductName = model.ProductName,
                        Description = model.Description,
                        CategoryId = model.CategoryId,
                        Price = model.Price,
                        StockQuantity = model.StockQuantity,
                        ReorderPoint = model.ReorderPoint,
                        MaxStockLevel = model.MaxStockLevel,
                        Unit = model.Unit,
                        Weight = model.Weight,
                        Barcode = model.Barcode,
                        SKU = model.SKU ?? GenerateSKU(model.ProductName),
                        ImagePath = imagePath,
                        IsActive = model.IsActive,
                        IsFeatured = model.IsFeatured,
                        MetaTitle = model.MetaTitle,
                        MetaDescription = model.MetaDescription,
                        CreatedAt = DateTime.UtcNow
                    };

                    Console.WriteLine("Product object created, adding to context...");
                    _context.Add(product);

                    Console.WriteLine("Saving changes...");
                    await _context.SaveChangesAsync();
                    Console.WriteLine($"Product saved with ID: {product.Id}");

                    // Create default product variation
                    if (model.CreateDefaultVariation)
                    {
                        Console.WriteLine("Creating default variation...");
                        var variation = new ProductVariation
                        {
                            ProductId = product.Id,
                            Size = model.DefaultSize ?? "Standard",
                            Unit = model.Unit,
                            Price = model.Price,
                            StockQuantity = model.StockQuantity,
                            ReorderPoint = model.ReorderPoint,
                            MaxStockLevel = model.MaxStockLevel,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ProductVariations.Add(variation);
                        await _context.SaveChangesAsync();
                        Console.WriteLine("Default variation created");
                    }

                    Console.WriteLine("=== PRODUCT CREATION SUCCESSFUL ===");
                    TempData["SuccessMessage"] = "Product created successfully!";
                    return RedirectToAction(nameof(Details), new { id = product.Id });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"=== EXCEPTION DURING CREATION ===");
                    Console.WriteLine($"Exception: {ex.Message}");
                    Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    }
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }

            Console.WriteLine("=== RETURNING TO VIEW ===");
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(model);
        }

        // GET: Admin/Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            ViewData["Title"] = $"Edit Product - {product.ProductName}";
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();

            var model = new CreateProductDetailedViewModel
            {
                ProductName = product.ProductName,
                Description = product.Description,
                CategoryId = product.CategoryId,
                Price = product.Price,
                StockQuantity = product.StockQuantity,
                ReorderPoint = product.ReorderPoint,
                MaxStockLevel = product.MaxStockLevel,
                Unit = product.Unit,
                Weight = product.Weight,
                Barcode = product.Barcode,
                SKU = product.SKU,
                IsActive = product.IsActive,
                IsFeatured = product.IsFeatured,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                CurrentImagePath = product.ImagePath
            };

            return View(model);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CreateProductDetailedViewModel model, IFormFile? productImage)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    if (productImage != null && productImage.Length > 0)
                    {
                        // Delete old image
                        if (!string.IsNullOrEmpty(product.ImagePath))
                        {
                            DeleteProductImage(product.ImagePath);
                        }

                        product.ImagePath = await SaveProductImage(productImage);
                    }

                    // Update product properties
                    product.ProductName = model.ProductName;
                    product.Description = model.Description;
                    product.CategoryId = model.CategoryId;
                    product.Price = model.Price;
                    product.StockQuantity = model.StockQuantity;
                    product.ReorderPoint = model.ReorderPoint;
                    product.MaxStockLevel = model.MaxStockLevel;
                    product.Unit = model.Unit;
                    product.Weight = model.Weight;
                    product.Barcode = model.Barcode;
                    product.SKU = model.SKU ?? product.SKU;
                    product.IsActive = model.IsActive;
                    product.IsFeatured = model.IsFeatured;
                    product.MetaTitle = model.MetaTitle;
                    product.MetaDescription = model.MetaDescription;
                    product.UpdatedAt = DateTime.UtcNow;

                    _context.Update(product);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = product.Id });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductExists(product.Id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            return View(model);
        }

        // POST: Toggle Product Status
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                product.IsActive = !product.IsActive;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Product {(product.IsActive ? "activated" : "deactivated")} successfully",
                    isActive = product.IsActive
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Toggle Featured Status
        [HttpPost]
        public async Task<IActionResult> ToggleFeatured(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                product.IsFeatured = !product.IsFeatured;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Product {(product.IsFeatured ? "featured" : "unfeatured")} successfully",
                    isFeatured = product.IsFeatured
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Bulk Update Stock
        [HttpPost]
        public async Task<IActionResult> BulkUpdateStock([FromBody] List<StockUpdateModel> updates)
        {
            try
            {
                foreach (var update in updates)
                {
                    var product = await _context.Products.FindAsync(update.ProductId);
                    if (product != null)
                    {
                        product.StockQuantity = update.NewStock;
                        product.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = $"Updated stock for {updates.Count} products" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Low Stock Products
        [HttpGet]
        public async Task<IActionResult> GetLowStockProducts()
        {
            var lowStockProducts = await _context.Products
                .Where(p => p.IsActive && p.StockQuantity <= p.ReorderPoint)
                .Select(p => new
                {
                    Id = p.Id,
                    ProductName = p.ProductName,
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    MaxStockLevel = p.MaxStockLevel,
                    SuggestedOrder = p.MaxStockLevel - p.StockQuantity
                })
                .ToListAsync();

            return Json(lowStockProducts);
        }

        // POST: Create Reorder Suggestion
        [HttpPost]
        public async Task<IActionResult> CreateReorderSuggestion(int productId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                    return Json(new { success = false, message = "Product not found" });

                var variation = await _context.ProductVariations
                    .FirstOrDefaultAsync(pv => pv.ProductId == productId && pv.IsActive);

                if (variation == null)
                    return Json(new { success = false, message = "No active variation found for this product" });

                // Create purchase order request
                var request = new CreatePurchaseOrderRequest { ProductVariationId = variation.Id };

                // Redirect to purchase orders controller
                return Json(new
                {
                    success = true,
                    message = "Redirecting to create purchase order...",
                    redirectUrl = Url.Action("CreateFromReorder", "PurchaseOrders", request)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (product == null) return NotFound();

            ViewData["Title"] = $"Delete Product - {product.ProductName}";
            return View(product);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null)
            {
                // Delete associated image
                if (!string.IsNullOrEmpty(product.ImagePath))
                {
                    DeleteProductImage(product.ImagePath);
                }

                // Check for dependencies (orders, variations, etc.)
                var hasVariations = await _context.ProductVariations.AnyAsync(pv => pv.ProductId == id);
                var hasSpecials = await _context.ProductSpecials.AnyAsync(ps => ps.ProductId == id);

                if (hasVariations || hasSpecials)
                {
                    TempData["ErrorMessage"] = "Cannot delete product that has variations or active specials. Please remove them first.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task<string> SaveProductImage(IFormFile imageFile)
        {
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "products");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/uploads/products/{fileName}";
        }

        private void DeleteProductImage(string imagePath)
        {
            try
            {
                var fullPath = Path.Combine(_webHostEnvironment.WebRootPath, imagePath.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
            catch (Exception ex)
            {
                // Log error but don't throw
                Console.WriteLine($"Error deleting image: {ex.Message}");
            }
        }

        private string GenerateSKU(string productName)
        {
            var prefix = string.Join("", productName.Split(' ').Take(2).Select(w => w.Substring(0, Math.Min(2, w.Length)).ToUpper()));
            var timestamp = DateTime.Now.ToString("yyyyMMdd");
            var random = new Random().Next(100, 999);
            return $"{prefix}-{timestamp}-{random}";
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.Id == id);
        }
        [HttpGet]
        public async Task<IActionResult> LiveSearch(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return PartialView("_LiveSearchResults", new List<dynamic>());
            }

            var products = await _context.Products
                .Where(p => p.IsActive &&
                           !p.IsDeleted &&
                           p.ProductName.Contains(query))
                .OrderBy(p => p.ProductName)
                .Take(6)
                .Select(p => new
                {
                    p.Id,
                    p.ProductName,
                    p.ImagePath,
                    p.Price
                })
                .ToListAsync();

            ViewBag.Query = query;
            return PartialView("_LiveSearchResults", products);
        }

    }

    // Additional models for the enhanced functionality
    

    
   

}