using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ProductSpecialsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductSpecialsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // Add this method to the ProductSpecialsController class

        // POST: Admin/ProductSpecials/BulkUpload
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUpload(BulkUploadSpecialsViewModel model)
        {
            try
            {
                if (model.CsvFile == null || model.CsvFile.Length == 0)
                {
                    return Json(new { success = false, message = "Please select a CSV file" });
                }

                var results = new List<SpecialUploadResult>();
                var successCount = 0;
                var errorCount = 0;

                using (var reader = new StreamReader(model.CsvFile.OpenReadStream()))
                {
                    var csvContent = await reader.ReadToEndAsync();
                    var lines = csvContent.Split('\n', StringSplitOptions.RemoveEmptyEntries);

                    if (lines.Length < 2)
                    {
                        return Json(new { success = false, message = "CSV file must contain at least a header and one data row" });
                    }

                    // Skip header row
                    for (int i = 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();
                        if (string.IsNullOrEmpty(line)) continue;

                        var result = await ProcessSpecialRow(line, model);
                        results.Add(result);

                        if (result.Success)
                            successCount++;
                        else
                            errorCount++;
                    }
                }

                return Json(new
                {
                    success = true,
                    message = $"Upload completed. {successCount} specials created, {errorCount} errors.",
                    successCount = successCount,
                    errorCount = errorCount,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error processing file: {ex.Message}" });
            }
        }

        private async Task<SpecialUploadResult> ProcessSpecialRow(string csvRow, BulkUploadSpecialsViewModel model)
        {
            try
            {
                var values = ParseCsvRow(csvRow);

                if (values.Count < 3) // ProductName, SpecialName, DiscountValue are required
                {
                    return new SpecialUploadResult
                    {
                        ProductName = values.Count > 0 ? values[0] : "Unknown",
                        SpecialName = values.Count > 1 ? values[1] : "Unknown",
                        Success = false,
                        ErrorMessage = "Insufficient data in row"
                    };
                }

                var productName = values[0].Trim();
                var specialName = values[1].Trim();
                var discountValueStr = values[2].Trim();

                // Find the product
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.ProductName.ToLower() == productName.ToLower());

                if (product == null)
                {
                    return new SpecialUploadResult
                    {
                        ProductName = productName,
                        SpecialName = specialName,
                        Success = false,
                        ErrorMessage = "Product not found"
                    };
                }

                // Parse discount value
                if (!decimal.TryParse(discountValueStr, out decimal discountValue))
                {
                    return new SpecialUploadResult
                    {
                        ProductName = productName,
                        SpecialName = specialName,
                        Success = false,
                        ErrorMessage = "Invalid discount value"
                    };
                }

                // Parse discount type (optional, use default if not provided)
                var discountType = model.DefaultDiscountType;
                if (values.Count > 3 && !string.IsNullOrEmpty(values[3]))
                {
                    if (Enum.TryParse<DiscountType>(values[3], true, out var parsedType))
                    {
                        discountType = parsedType;
                    }
                }

                // Parse dates (optional, use defaults if not provided)
                var startDate = model.DefaultStartDate;
                var endDate = model.DefaultEndDate;

                if (values.Count > 5 && !string.IsNullOrEmpty(values[5]))
                {
                    if (DateTime.TryParse(values[5], out var parsedStartDate))
                        startDate = parsedStartDate;
                }

                if (values.Count > 6 && !string.IsNullOrEmpty(values[6]))
                {
                    if (DateTime.TryParse(values[6], out var parsedEndDate))
                        endDate = parsedEndDate;
                }

                // Parse minimum quantity (optional)
                int? minimumQuantity = null;
                if (values.Count > 7 && !string.IsNullOrEmpty(values[7]))
                {
                    if (int.TryParse(values[7], out var minQty))
                        minimumQuantity = minQty;
                }

                // Validate discount value based on type
                if (discountType == DiscountType.Percentage && discountValue > 100)
                {
                    return new SpecialUploadResult
                    {
                        ProductName = productName,
                        SpecialName = specialName,
                        Success = false,
                        ErrorMessage = "Percentage discount cannot exceed 100%"
                    };
                }

                // Check if special already exists
                var existingSpecial = await _context.ProductSpecials
                    .FirstOrDefaultAsync(ps => ps.ProductId == product.Id &&
                                             ps.SpecialName.ToLower() == specialName.ToLower() &&
                                             ps.IsActive);

                if (existingSpecial != null)
                {
                    return new SpecialUploadResult
                    {
                        ProductName = productName,
                        SpecialName = specialName,
                        Success = false,
                        ErrorMessage = "Special with this name already exists for this product"
                    };
                }

                // Create the special
                var productSpecial = new ProductSpecial
                {
                    ProductId = product.Id,
                    SpecialName = specialName,
                    Description = values.Count > 4 ? values[4] : null,
                    DiscountType = discountType,
                    DiscountValue = discountValue,
                    StartDate = startDate,
                    EndDate = endDate,
                    MinimumQuantity = minimumQuantity,
                    IsActive = model.AutoActivate,
                    IsFeatured = model.MakeFeatured,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductSpecials.Add(productSpecial);
                await _context.SaveChangesAsync();

                return new SpecialUploadResult
                {
                    ProductName = productName,
                    SpecialName = specialName,
                    Success = true,
                    DiscountValue = discountValue,
                    DiscountType = discountType
                };
            }
            catch (Exception ex)
            {
                return new SpecialUploadResult
                {
                    ProductName = "Unknown",
                    SpecialName = "Unknown",
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private List<string> ParseCsvRow(string csvRow)
        {
            var values = new List<string>();
            var inQuotes = false;
            var currentValue = string.Empty;

            for (int i = 0; i < csvRow.Length; i++)
            {
                char c = csvRow[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.Trim());
                    currentValue = string.Empty;
                }
                else
                {
                    currentValue += c;
                }
            }

            // Add the last value
            values.Add(currentValue.Trim());

            return values;
        }

        // GET: Export Product Specials Template
        [HttpGet]
        public IActionResult ExportTemplate()
        {
            var csvContent = new StringBuilder();
            csvContent.AppendLine("ProductName,SpecialName,DiscountValue,DiscountType,Description,StartDate,EndDate,MinimumQuantity");
            csvContent.AppendLine("\"Sample Product\",\"Summer Sale\",15,\"Percentage\",\"15% off summer special\",\"2024-06-01\",\"2024-08-31\",1");
            csvContent.AppendLine("\"Rice 25kg\",\"Rice Special Offer\",15,\"Percentage\",\"Special discount on rice\",\"2024-01-01\",\"2024-01-31\",1");
            csvContent.AppendLine("\"Milk 1L\",\"Milk BOGO\",0,\"BuyOneGetOne\",\"Buy one get one free on milk\",\"2024-01-01\",\"2024-01-15\",2");
            csvContent.AppendLine("\"Sugar 1kg\",\"Sugar Discount\",50,\"FixedAmount\",\"Fixed discount on sugar\",\"2024-01-01\",\"2024-01-20\",1");

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", "product_specials_template.csv");
        }

        // GET: Export Current Specials
        [HttpGet]
        public async Task<IActionResult> ExportSpecials()
        {
            var specials = await _context.ProductSpecials
                .Include(ps => ps.Product)
                .Where(ps => ps.IsActive)
                .ToListAsync();

            var csvContent = new StringBuilder();
            csvContent.AppendLine("ProductName,SpecialName,DiscountValue,DiscountType,Description,StartDate,EndDate,MinimumQuantity,IsActive,IsFeatured");

            foreach (var special in specials)
            {
                csvContent.AppendLine($"\"{special.Product.ProductName}\",\"{special.SpecialName}\",{special.DiscountValue},\"{special.DiscountType}\",\"{special.Description}\",\"{special.StartDate:yyyy-MM-dd}\",\"{special.EndDate:yyyy-MM-dd}\",{special.MinimumQuantity},{special.IsActive},{special.IsFeatured}");
            }

            var bytes = Encoding.UTF8.GetBytes(csvContent.ToString());
            return File(bytes, "text/csv", $"product_specials_export_{DateTime.Now:yyyyMMdd}.csv");
        }


        // GET: Admin/ProductSpecials
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Product Specials";

            var specials = await _context.ProductSpecials
                .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
                .OrderByDescending(ps => ps.CreatedAt)
                .ToListAsync();

            var viewModel = new ProductSpecialsIndexViewModel
            {
                ProductSpecials = specials,
                TotalSpecials = specials.Count,
                ActiveSpecials = specials.Count(s => s.IsActive && s.StartDate <= DateTime.Now && s.EndDate >= DateTime.Now),
                FeaturedSpecials = specials.Count(s => s.IsFeatured && s.IsActive),
                TotalDiscountValue = specials.Where(s => s.IsActive).Sum(s => CalculateDiscountValue(s))
            };

            return View(viewModel);
        }

        // GET: Admin/ProductSpecials/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var productSpecial = await _context.ProductSpecials
                .Include(ps => ps.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productSpecial == null) return NotFound();

            ViewData["Title"] = $"Special Details - {productSpecial.SpecialName}";
            return View(productSpecial);
        }

        // GET: Admin/ProductSpecials/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Product Special";
            ViewBag.Products = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToList();

            var model = new ProductSpecialViewModel
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                DiscountType = DiscountType.Percentage
            };

            return View(model);
        }

        // POST: Admin/ProductSpecials/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductSpecialViewModel model, IFormFile? specialImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Validate dates
                    if (model.EndDate <= model.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        ViewBag.Products = GetActiveProducts();
                        return View(model);
                    }

                    // Validate discount value based on type
                    if (model.DiscountType == DiscountType.Percentage && model.DiscountValue > 100)
                    {
                        ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
                        ViewBag.Products = GetActiveProducts();
                        return View(model);
                    }

                    // Handle image upload
                    string? imagePath = null;
                    if (specialImage != null && specialImage.Length > 0)
                    {
                        imagePath = await SaveSpecialImage(specialImage);
                    }

                    var productSpecial = new ProductSpecial
                    {
                        ProductId = model.ProductId,
                        SpecialName = model.SpecialName,
                        Description = model.Description,
                        DiscountType = model.DiscountType,
                        DiscountValue = model.DiscountValue,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        IsFeatured = model.IsFeatured,
                        MinimumQuantity = model.MinimumQuantity,
                        MaximumQuantity = model.MaximumQuantity,
                        ImagePath = imagePath,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ProductSpecials.Add(productSpecial);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product special created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating special: {ex.Message}");
                }
            }

            ViewBag.Products = GetActiveProducts();
            return View(model);
        }

        // GET: Admin/ProductSpecials/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var productSpecial = await _context.ProductSpecials.FindAsync(id);
            if (productSpecial == null) return NotFound();

            ViewData["Title"] = $"Edit Special - {productSpecial.SpecialName}";
            ViewBag.Products = GetActiveProducts();

            var model = new ProductSpecialViewModel
            {
                ProductId = productSpecial.ProductId,
                SpecialName = productSpecial.SpecialName,
                Description = productSpecial.Description,
                DiscountType = productSpecial.DiscountType,
                DiscountValue = productSpecial.DiscountValue,
                StartDate = productSpecial.StartDate,
                EndDate = productSpecial.EndDate,
                IsFeatured = productSpecial.IsFeatured,
                MinimumQuantity = productSpecial.MinimumQuantity,
                MaximumQuantity = productSpecial.MaximumQuantity
            };

            return View(model);
        }

        // POST: Admin/ProductSpecials/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductSpecialViewModel model, IFormFile? specialImage)
        {
            var productSpecial = await _context.ProductSpecials.FindAsync(id);
            if (productSpecial == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    // Validate dates
                    if (model.EndDate <= model.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "End date must be after start date.");
                        ViewBag.Products = GetActiveProducts();
                        return View(model);
                    }

                    // Validate discount value based on type
                    if (model.DiscountType == DiscountType.Percentage && model.DiscountValue > 100)
                    {
                        ModelState.AddModelError("DiscountValue", "Percentage discount cannot exceed 100%.");
                        ViewBag.Products = GetActiveProducts();
                        return View(model);
                    }

                    // Handle image upload
                    if (specialImage != null && specialImage.Length > 0)
                    {
                        // Delete old image
                        if (!string.IsNullOrEmpty(productSpecial.ImagePath))
                        {
                            DeleteSpecialImage(productSpecial.ImagePath);
                        }

                        productSpecial.ImagePath = await SaveSpecialImage(specialImage);
                    }

                    // Update properties
                    productSpecial.ProductId = model.ProductId;
                    productSpecial.SpecialName = model.SpecialName;
                    productSpecial.Description = model.Description;
                    productSpecial.DiscountType = model.DiscountType;
                    productSpecial.DiscountValue = model.DiscountValue;
                    productSpecial.StartDate = model.StartDate;
                    productSpecial.EndDate = model.EndDate;
                    productSpecial.IsFeatured = model.IsFeatured;
                    productSpecial.MinimumQuantity = model.MinimumQuantity;
                    productSpecial.MaximumQuantity = model.MaximumQuantity;
                    productSpecial.UpdatedAt = DateTime.UtcNow;

                    _context.Update(productSpecial);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product special updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductSpecialExists(productSpecial.Id))
                        return NotFound();
                    throw;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating special: {ex.Message}");
                }
            }

            ViewBag.Products = GetActiveProducts();
            return View(model);
        }

        // POST: Toggle Special Status
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            try
            {
                var productSpecial = await _context.ProductSpecials.FindAsync(id);
                if (productSpecial == null)
                    return Json(new { success = false, message = "Special not found" });

                productSpecial.IsActive = !productSpecial.IsActive;
                productSpecial.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Special {(productSpecial.IsActive ? "activated" : "deactivated")} successfully",
                    isActive = productSpecial.IsActive
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
                var productSpecial = await _context.ProductSpecials.FindAsync(id);
                if (productSpecial == null)
                    return Json(new { success = false, message = "Special not found" });

                productSpecial.IsFeatured = !productSpecial.IsFeatured;
                productSpecial.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = $"Special {(productSpecial.IsFeatured ? "featured" : "unfeatured")} successfully",
                    isFeatured = productSpecial.IsFeatured
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Admin/ProductSpecials/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var productSpecial = await _context.ProductSpecials
                .Include(ps => ps.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productSpecial == null) return NotFound();

            ViewData["Title"] = $"Delete Special - {productSpecial.SpecialName}";
            return View(productSpecial);
        }

        // POST: Admin/ProductSpecials/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productSpecial = await _context.ProductSpecials.FindAsync(id);
            if (productSpecial != null)
            {
                // Delete associated image
                if (!string.IsNullOrEmpty(productSpecial.ImagePath))
                {
                    DeleteSpecialImage(productSpecial.ImagePath);
                }

                _context.ProductSpecials.Remove(productSpecial);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product special deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: Calculate Discounted Price
        [HttpGet]
        public async Task<IActionResult> CalculateDiscountedPrice(int productId, int specialId)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                var special = await _context.ProductSpecials.FindAsync(specialId);

                if (product == null || special == null)
                    return Json(new { success = false, message = "Product or special not found" });

                decimal discountedPrice = CalculateDiscountedPrice(product.Price, special);
                decimal savings = product.Price - discountedPrice;

                return Json(new
                {
                    success = true,
                    originalPrice = product.Price,
                    discountedPrice = discountedPrice,
                    savings = savings,
                    discountPercentage = Math.Round((savings / product.Price) * 100, 2)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Featured Specials API
        [HttpGet]
        public async Task<IActionResult> GetFeaturedSpecials()
        {
            var featuredSpecials = await _context.ProductSpecials
                .Include(ps => ps.Product)
                .Where(ps => ps.IsActive && ps.IsFeatured &&
                           ps.StartDate <= DateTime.Now && ps.EndDate >= DateTime.Now)
                .Select(ps => new
                {
                    Id = ps.Id,
                    SpecialName = ps.SpecialName,
                    ProductName = ps.Product.ProductName,
                    OriginalPrice = ps.Product.Price,
                    DiscountedPrice = CalculateDiscountedPrice(ps.Product.Price, ps),
                    DiscountValue = ps.DiscountValue,
                    DiscountType = ps.DiscountType.ToString(),
                    ImagePath = ps.ImagePath ?? ps.Product.ImagePath,
                    EndDate = ps.EndDate
                })
                .ToListAsync();

            return Json(featuredSpecials);
        }

        private decimal CalculateDiscountedPrice(decimal originalPrice, ProductSpecial special)
        {
            return special.DiscountType switch
            {
                DiscountType.Percentage => originalPrice * (1 - special.DiscountValue / 100),
                DiscountType.FixedAmount => Math.Max(0, originalPrice - special.DiscountValue),
                DiscountType.BuyOneGetOne => originalPrice * 0.5m, // Simplified calculation
                DiscountType.BuyTwoGetOne => originalPrice * (2m / 3m), // Simplified calculation
                _ => originalPrice
            };
        }

        private decimal CalculateDiscountValue(ProductSpecial special)
        {
            var product = _context.Products.Find(special.ProductId);
            if (product == null) return 0;

            return product.Price - CalculateDiscountedPrice(product.Price, special);
        }

        private List<Product> GetActiveProducts()
        {
            return _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .ToList();
        }

        private async Task<string> SaveSpecialImage(IFormFile imageFile)
        {
            var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "specials");

            if (!Directory.Exists(uploadsPath))
                Directory.CreateDirectory(uploadsPath);

            var fileName = $"{Guid.NewGuid()}_{imageFile.FileName}";
            var filePath = Path.Combine(uploadsPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(stream);
            }

            return $"/uploads/specials/{fileName}";
        }

        private void DeleteSpecialImage(string imagePath)
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

        private bool ProductSpecialExists(int id)
        {
            return _context.ProductSpecials.Any(e => e.Id == id);
        }
    }
}