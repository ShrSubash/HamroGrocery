using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Areas.Admin.Models.ViewModels;
using System.Linq.Dynamic.Core;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class InventoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InventoryController> _logger;

        public InventoryController(ApplicationDbContext context, ILogger<InventoryController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Main inventory dashboard with all stock levels and filtering capabilities
        /// </summary>
        public async Task<IActionResult> Index(string searchTerm, string stockFilter = "all", int pageNumber = 1, int pageSize = 20)
        {
            try
            {
                ViewData["Title"] = "Inventory Management";

                // Build base query
                var query = _context.ProductVariations
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Category)
                    .AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.ToLower();
                    query = query.Where(pv =>
                        pv.Product.ProductName.ToLower().Contains(searchTerm) ||
                        pv.VariationName.ToLower().Contains(searchTerm) ||
                        pv.SKU.ToLower().Contains(searchTerm) ||
                        pv.Product.Category.CategoryName.ToLower().Contains(searchTerm)
                    );
                }

                // Apply stock filter
                query = stockFilter switch
                {
                    "outofstock" => query.Where(pv => pv.StockQuantity == 0),
                    "lowstock" => query.Where(pv => pv.StockQuantity > 0 && pv.StockQuantity <= pv.ReorderPoint),
                    "wellstocked" => query.Where(pv => pv.StockQuantity > pv.ReorderPoint),
                    "critical" => query.Where(pv => pv.StockQuantity <= pv.ReorderPoint), // Out of stock + Low stock
                    _ => query // "all"
                };

                // Calculate statistics
                var allVariations = await _context.ProductVariations.ToListAsync();
                var stats = new InventoryStatsViewModel
                {
                    TotalProducts = allVariations.Count,
                    OutOfStock = allVariations.Count(pv => pv.StockQuantity == 0),
                    LowStock = allVariations.Count(pv => pv.StockQuantity > 0 && pv.StockQuantity <= pv.ReorderPoint),
                    WellStocked = allVariations.Count(pv => pv.StockQuantity > pv.ReorderPoint),
                    TotalValue = allVariations.Sum(pv => pv.StockQuantity * pv.Price),
                    CriticalItems = allVariations.Count(pv => pv.StockQuantity <= pv.ReorderPoint)
                };

                // Get paginated results
                var totalItems = await query.CountAsync();
                var items = await query
                    .OrderBy(pv => pv.StockQuantity)
                    .ThenBy(pv => pv.Product.ProductName)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var viewModel = new InventoryDashboardViewModel
                {
                    Statistics = stats,
                    ProductVariations = items,
                    SearchTerm = searchTerm,
                    StockFilter = stockFilter,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading inventory dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading inventory data.";
                return View(new InventoryDashboardViewModel());
            }
        }

        /// <summary>
        /// Get stock movements history with filtering
        /// </summary>
        public async Task<IActionResult> StockMovements(int? productVariationId, DateTime? startDate, DateTime? endDate, int pageNumber = 1, int pageSize = 50)
        {
            try
            {
                ViewData["Title"] = "Stock Movements History";

                var query = _context.StockMovements
                    .Include(sm => sm.ProductVariation)
                        .ThenInclude(pv => pv.Product)
                    .AsQueryable();

                // Apply filters
                if (productVariationId.HasValue)
                {
                    query = query.Where(sm => sm.ProductVariationId == productVariationId.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(sm => sm.CreatedAt >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    var endDateTime = endDate.Value.AddDays(1).AddTicks(-1);
                    query = query.Where(sm => sm.CreatedAt <= endDateTime);
                }

                var totalItems = await query.CountAsync();
                var movements = await query
                    .OrderByDescending(sm => sm.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var viewModel = new StockMovementsViewModel
                {
                    Movements = movements,
                    ProductVariationId = productVariationId,
                    StartDate = startDate,
                    EndDate = endDate,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    TotalItems = totalItems,
                    TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock movements");
                TempData["ErrorMessage"] = "An error occurred while loading stock movements.";
                return View(new StockMovementsViewModel());
            }
        }

        /// <summary>
        /// Display stock adjustment form
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> AdjustStock(int id)
        {
            try
            {
                var productVariation = await _context.ProductVariations
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(pv => pv.Id == id);

                if (productVariation == null)
                {
                    TempData["ErrorMessage"] = "Product variation not found.";
                    return RedirectToAction(nameof(Index));
                }

                ViewData["Title"] = $"Adjust Stock - {productVariation.Product.ProductName}";
                return View(productVariation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading stock adjustment form for variation {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while loading the stock adjustment form.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Process stock adjustment with validation and audit trail
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AdjustStock(int id, int adjustmentQuantity, string reason)
        {
            try
            {
                var productVariation = await _context.ProductVariations
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == id);

                if (productVariation == null)
                {
                    TempData["ErrorMessage"] = "Product variation not found.";
                    return RedirectToAction(nameof(Index));
                }

                // Validate adjustment quantity
                if (adjustmentQuantity == 0)
                {
                    ModelState.AddModelError("", "Adjustment quantity cannot be zero.");
                    ViewData["Title"] = $"Adjust Stock - {productVariation.Product.ProductName}";
                    return View(productVariation);
                }

                // Validate reason
                if (string.IsNullOrWhiteSpace(reason))
                {
                    ModelState.AddModelError("", "Please provide a reason for this adjustment.");
                    ViewData["Title"] = $"Adjust Stock - {productVariation.Product.ProductName}";
                    return View(productVariation);
                }

                var oldQuantity = productVariation.StockQuantity;
                var newQuantity = oldQuantity + adjustmentQuantity;

                // Prevent negative stock
                if (newQuantity < 0)
                {
                    ModelState.AddModelError("", $"Insufficient stock. Current quantity is {oldQuantity}. Cannot reduce by {Math.Abs(adjustmentQuantity)}.");
                    ViewData["Title"] = $"Adjust Stock - {productVariation.Product.ProductName}";
                    return View(productVariation);
                }

                // Update stock quantity
                productVariation.StockQuantity = newQuantity;

                // Create audit trail
                var stockMovement = new StockMovement
                {
                    ProductVariationId = id,
                    MovementType = adjustmentQuantity > 0 ? StockMovementType.StockIn : StockMovementType.StockOut,
                    Quantity = Math.Abs(adjustmentQuantity),
                    PreviousQuantity = oldQuantity,
                    NewQuantity = newQuantity,
                    Reason = reason.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = User.Identity?.Name ?? "System"
                };

                _context.StockMovements.Add(stockMovement);
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "Stock adjusted for {ProductName} - {VariationName}: {OldQuantity} → {NewQuantity} by {User}",
                    productVariation.Product.ProductName,
                    productVariation.VariationName,
                    oldQuantity,
                    newQuantity,
                    User.Identity?.Name
                );

                TempData["SuccessMessage"] = $"Stock adjusted successfully! {productVariation.Product.ProductName} - {productVariation.VariationName}: {oldQuantity} → {newQuantity} units";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adjusting stock for variation {Id}", id);
                TempData["ErrorMessage"] = "An error occurred while adjusting stock.";
                return RedirectToAction(nameof(Index));
            }
        }
        /// <summary>
        /// Auto-generate purchase order for low stock item
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePurchaseOrder(int productVariationId)
        {
            try
            {
                var productVariation = await _context.ProductVariations
                    .Include(pv => pv.Product)
                        .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(pv => pv.Id == productVariationId);

                if (productVariation == null)
                {
                    return Json(new { success = false, message = "Product variation not found." });
                }

                // Find preferred or first available supplier
                var supplier = await _context.Suppliers
                    .Where(s => s.IsActive && !s.IsDeleted)
                    .OrderByDescending(s => s.IsPreferred)
                    .ThenBy(s => s.SupplierName)
                    .FirstOrDefaultAsync();

                if (supplier == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No active suppliers available. Please add a supplier first."
                    });
                }

                // Calculate optimal order quantity
                var orderQuantity = CalculateOptimalOrderQuantity(productVariation);

                // Calculate unit cost (estimated cost price)
                var unitCost = CalculateUnitCost(productVariation.Price);

                // Calculate total cost for this item
                var totalItemCost = orderQuantity * unitCost;

                // Create purchase order item
                var poItem = new PurchaseOrderItem
                {
                    ProductVariationId = productVariation.Id,
                    Quantity = orderQuantity,
                    UnitCost = unitCost,  // This sets UnitPrice internally via the property
                    Notes = $"Auto-generated from low stock alert",
                    ReceivedQuantity = 0,
                    IsActive = true,
                    IsDeleted = false,
                    CreatedAt = DateTime.UtcNow
                    // TotalCost is calculated automatically: Quantity * UnitPrice
                };

                // Calculate expected delivery date (assume 7 business days)
                var expectedDeliveryDate = CalculateExpectedDeliveryDate(DateTime.UtcNow, 7);

                // Create purchase order
                var purchaseOrder = new PurchaseOrder
                {
                    PONumber = GeneratePONumber(),
                    SupplierId = supplier.Id,
                    OrderDate = DateTime.UtcNow,
                    ExpectedDeliveryDate = expectedDeliveryDate,
                    Status = PurchaseOrderStatus.Draft,
                    TotalAmount = totalItemCost,  // Set the calculated total
                    Notes = $"Auto-generated for low stock alert{Environment.NewLine}" +
                            $"Product: {productVariation.Product.ProductName} ({productVariation.VariationName}){Environment.NewLine}" +
                            $"Current Stock: {productVariation.StockQuantity} | Reorder Point: {productVariation.ReorderPoint}",
                    IsAutoGenerated = true,
                    CreatedBy = User.Identity?.Name ?? "System",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false,
                    Items = new List<PurchaseOrderItem> { poItem }
                };

                // Add to database
                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                // Log the successful creation
                _logger.LogInformation(
                    "Purchase order {PONumber} created automatically | " +
                    "Product: {ProductName} - {VariationName} | " +
                    "Supplier: {SupplierName} | " +
                    "Quantity: {Quantity} | " +
                    "Unit Cost: NPR {UnitCost:N2} | " +
                    "Total: NPR {Total:N2} | " +
                    "Expected Delivery: {ExpectedDate:yyyy-MM-dd}",
                    purchaseOrder.PONumber,
                    productVariation.Product.ProductName,
                    productVariation.VariationName,
                    supplier.SupplierName,
                    orderQuantity,
                    unitCost,
                    totalItemCost,
                    expectedDeliveryDate
                );

                return Json(new
                {
                    success = true,
                    message = $"Purchase order {purchaseOrder.PONumber} created successfully for {orderQuantity} units!",
                    data = new
                    {
                        poId = purchaseOrder.Id,
                        poNumber = purchaseOrder.PONumber,
                        supplierId = supplier.Id,
                        supplierName = supplier.SupplierName,
                        productName = $"{productVariation.Product.ProductName} - {productVariation.VariationName}",
                        quantity = orderQuantity,
                        unitCost = unitCost,
                        totalCost = totalItemCost,
                        orderDate = purchaseOrder.OrderDate.ToString("yyyy-MM-dd"),
                        expectedDeliveryDate = expectedDeliveryDate.ToString("yyyy-MM-dd"),
                        status = purchaseOrder.Status.ToString()
                    }
                });
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error creating purchase order for variation {Id}", productVariationId);
                return Json(new
                {
                    success = false,
                    message = "Database error occurred. Please check if all required data is valid and try again."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error creating purchase order for variation {Id}", productVariationId);
                return Json(new
                {
                    success = false,
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Get real-time inventory statistics
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStockStats()
        {
            try
            {
                var variations = await _context.ProductVariations.ToListAsync();

                var stats = new
                {
                    totalProducts = variations.Count,
                    outOfStock = variations.Count(pv => pv.StockQuantity == 0),
                    lowStock = variations.Count(pv => pv.StockQuantity > 0 && pv.StockQuantity <= pv.ReorderPoint),
                    wellStocked = variations.Count(pv => pv.StockQuantity > pv.ReorderPoint),
                    criticalItems = variations.Count(pv => pv.StockQuantity <= pv.ReorderPoint),
                    totalValue = variations.Sum(pv => pv.StockQuantity * pv.Price),
                    averageStockLevel = variations.Any() ? variations.Average(pv => pv.StockQuantity) : 0
                };

                return Json(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock statistics");
                return Json(new { error = "Failed to retrieve statistics" });
            }
        }

        #region Helper Methods

        /// <summary>
        /// Generate unique purchase order number
        /// Format: PO-YYYYMMDD-XXXXX (where XXXXX is a random 5-digit number)
        /// </summary>
        private string GeneratePONumber()
        {
            var datePart = DateTime.Now.ToString("yyyyMMdd");
            var randomPart = Random.Shared.Next(10000, 99999);
            return $"PO-{datePart}-{randomPart}";
        }

        /// <summary>
        /// Calculate optimal reorder quantity based on max stock level, current stock, and reorder point
        /// </summary>
        private int CalculateOptimalOrderQuantity(ProductVariation productVariation)
        {
            // Strategy: Order enough to reach max stock level, or at least double the reorder point
            var toMaxStock = productVariation.MaxStockLevel - productVariation.StockQuantity;
            var minOrder = productVariation.ReorderPoint * 2;

            // Use the larger of the two values to ensure adequate stock
            var optimalQuantity = Math.Max(toMaxStock, minOrder);

            // Ensure minimum order quantity is at least 1
            return Math.Max(optimalQuantity, 1);
        }

        /// <summary>
        /// Calculate estimated unit cost from selling price
        /// Assumes a 40% markup: Cost = Price / 1.4
        /// </summary>
        /// <param name="sellingPrice">The retail selling price</param>
        /// <returns>Estimated unit cost for purchasing</returns>
        private decimal CalculateUnitCost(decimal sellingPrice)
        {
            // Business assumption: 40% markup on cost price
            // Selling Price = Cost + (Cost * 0.40)
            // Selling Price = Cost * 1.40
            // Therefore: Cost = Selling Price / 1.40

            const decimal markupFactor = 1.4m;
            return Math.Round(sellingPrice / markupFactor, 2);
        }
        /// <summary>
        /// Calculate expected delivery date by adding business days
        /// </summary>
        /// <param name="startDate">Starting date</param>
        /// <param name="businessDays">Number of business days to add</param>
        /// <returns>Expected delivery date</returns>
        private DateTime CalculateExpectedDeliveryDate(DateTime startDate, int businessDays)
        {
            var currentDate = startDate;
            var daysAdded = 0;

            while (daysAdded < businessDays)
            {
                currentDate = currentDate.AddDays(1);

                // Skip weekends (Saturday = 6, Sunday = 0)
                if (currentDate.DayOfWeek != DayOfWeek.Saturday &&
                    currentDate.DayOfWeek != DayOfWeek.Sunday)
                {
                    daysAdded++;
                }
            }

            return currentDate;
        }

        #endregion
    }
}