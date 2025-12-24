using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using HamroGrocery.Models.RequestModels;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class PurchaseOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public PurchaseOrdersController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin/PurchaseOrders
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Purchase Orders";

            var purchaseOrders = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Items)
                    .ThenInclude(poi => poi.ProductVariation)
                    .ThenInclude(pv => pv.Product)
                .OrderByDescending(po => po.CreatedAt)
                .ToListAsync();

            var viewModel = new PurchaseOrderIndexViewModel
            {
                PurchaseOrders = purchaseOrders,
                TotalOrders = purchaseOrders.Count,
                PendingOrders = purchaseOrders.Count(po => po.Status == PurchaseOrderStatus.Pending),
                CompletedOrders = purchaseOrders.Count(po => po.Status == PurchaseOrderStatus.Completed),
                TotalValue = purchaseOrders.Sum(po => po.TotalAmount)
            };

            return View(viewModel);
        }

        // GET: Admin/PurchaseOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var purchaseOrder = await _context.PurchaseOrders
                .Include(po => po.Supplier)
                .Include(po => po.Items)
                    .ThenInclude(poi => poi.ProductVariation)
                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (purchaseOrder == null) return NotFound();

            ViewData["Title"] = $"Purchase Order Details - {purchaseOrder.PONumber}";
            return View(purchaseOrder);
        }

        // GET: Admin/PurchaseOrders/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Purchase Order";
            ViewBag.Suppliers = _context.Suppliers.Where(s => s.IsActive).ToList();
            ViewBag.ProductVariations = _context.ProductVariations
                .Include(pv => pv.Product)
                .Where(pv => pv.IsActive)
                .ToList();

            var model = new CreatePurchaseOrderViewModel
            {
                PONumber = GeneratePONumber(),
                OrderDate = DateTime.Now,
                Items = new List<PurchaseOrderItemViewModel>()
            };

            return View(model);
        }

        // POST: Admin/PurchaseOrders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreatePurchaseOrderViewModel model)
        {
            if (ModelState.IsValid && model.Items.Any())
            {
                var purchaseOrder = new PurchaseOrder
                {
                    PONumber = model.PONumber,
                    SupplierId = model.SupplierId,
                    OrderDate = model.OrderDate,
                    ExpectedDeliveryDate = model.ExpectedDeliveryDate,
                    Notes = model.Notes,
                    Status = PurchaseOrderStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };

                foreach (var item in model.Items.Where(i => i.Quantity > 0))
                {
                    var poItem = new PurchaseOrderItem
                    {
                        ProductVariationId = item.ProductVariationId,
                        Quantity = item.Quantity,
                        UnitCost = item.UnitCost,
                       
                    };
                    purchaseOrder.Items.Add(poItem);
                }

                purchaseOrder.TotalAmount = purchaseOrder.Items.Sum(i => i.TotalCost);

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Purchase order created successfully!";
                return RedirectToAction(nameof(Details), new { id = purchaseOrder.Id });
            }

            ViewBag.Suppliers = _context.Suppliers.Where(s => s.IsActive).ToList();
            ViewBag.ProductVariations = _context.ProductVariations
                .Include(pv => pv.Product)
                .Where(pv => pv.IsActive)
                .ToList();

            return View(model);
        }

        // POST: Receive Purchase Order and Update Stock
        [HttpPost]
        public async Task<IActionResult> ReceivePurchaseOrder(int id, [FromBody] ReceivePurchaseOrderRequest request)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders
                    .Include(po => po.Items)
                        .ThenInclude(poi => poi.ProductVariation)
                    .FirstOrDefaultAsync(po => po.Id == id);

                if (purchaseOrder == null)
                    return Json(new { success = false, message = "Purchase order not found" });

                if (purchaseOrder.Status != PurchaseOrderStatus.Pending)
                    return Json(new { success = false, message = "Purchase order is not in pending status" });

                // Update stock quantities
                foreach (var receivedItem in request.ReceivedItems)
                {
                    var poItem = purchaseOrder.Items.FirstOrDefault(i => i.Id == receivedItem.ItemId);
                    if (poItem != null)
                    {
                        poItem.ReceivedQuantity = receivedItem.ReceivedQuantity;
                        poItem.ProductVariation.StockQuantity += receivedItem.ReceivedQuantity;
                        poItem.ProductVariation.UpdatedAt = DateTime.UtcNow;
                    }
                }

                purchaseOrder.Status = PurchaseOrderStatus.Completed;
                purchaseOrder.ReceivedDate = DateTime.UtcNow;
                purchaseOrder.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Purchase order received successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Create Purchase Order from Reorder
        [HttpPost]
        public async Task<IActionResult> CreateFromReorder([FromBody] CreatePurchaseOrderRequest request)
        {
            try
            {
                var productVariation = await _context.ProductVariations
                    .Include(pv => pv.Product)
                    .FirstOrDefaultAsync(pv => pv.Id == request.ProductVariationId);

                if (productVariation == null)
                    return Json(new { success = false, message = "Product variation not found" });

                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.IsActive);
                if (supplier == null)
                    return Json(new { success = false, message = "No active suppliers available" });

                var purchaseOrder = new PurchaseOrder
                {
                    PONumber = GeneratePONumber(),
                    SupplierId = supplier.Id,
                    OrderDate = DateTime.Now,
                    IsAutoGenerated = true,
                    Status = PurchaseOrderStatus.Draft,
                    CreatedAt = DateTime.UtcNow
                };

                var orderQuantity = Math.Max(
                    productVariation.MaxStockLevel - productVariation.StockQuantity,
                    productVariation.ReorderPoint * 2
                );

                var poItem = new PurchaseOrderItem
                {
                    ProductVariationId = productVariation.Id,
                    Quantity = orderQuantity,
                    UnitCost = productVariation.Price * 0.6m,
                   
                };

                purchaseOrder.Items.Add(poItem);
                purchaseOrder.TotalAmount = poItem.TotalCost;

                _context.PurchaseOrders.Add(purchaseOrder);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Purchase order created successfully",
                    purchaseOrderId = purchaseOrder.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Create Product from Purchase Order
        public IActionResult CreateProduct(int? purchaseOrderId)
        {
            ViewData["Title"] = "Add New Product";
            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            ViewBag.Suppliers = _context.Suppliers.Where(s => s.IsActive).ToList();
            ViewBag.PurchaseOrderId = purchaseOrderId;

            return View(new CreateProductViewModel());
        }

        // POST: Create Product from Purchase Order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateProduct(CreateProductViewModel model, IFormFile? productImage)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle image upload
                    string? imagePath = null;
                    if (productImage != null && productImage.Length > 0)
                    {
                        imagePath = await SaveProductImage(productImage);
                    }

                    // Create Product
                    var product = new Product
                    {
                        ProductName = model.ProductName,
                        Description = model.Description,
                        CategoryId = model.CategoryId,
                        Price = model.Price,
                        StockQuantity = model.StockQuantity,
                        ReorderPoint = model.ReorderPoint,
                        MaxStockLevel = model.MaxStockLevel,
                        ImagePath = imagePath,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Products.Add(product);
                    await _context.SaveChangesAsync();

                    // Create Product Variation
                    var productVariation = new ProductVariation
                    {
                        ProductId = product.Id,
                        Size = model.Size,
                        Unit = model.Unit,
                        Price = model.Price,
                        StockQuantity = model.StockQuantity,
                        ReorderPoint = model.ReorderPoint,
                        MaxStockLevel = model.MaxStockLevel,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.ProductVariations.Add(productVariation);
                    await _context.SaveChangesAsync();

                    // Add to Purchase Order if specified
                    if (model.PurchaseOrderId.HasValue)
                    {
                        var purchaseOrder = await _context.PurchaseOrders
                            .Include(po => po.Items)
                            .FirstOrDefaultAsync(po => po.Id == model.PurchaseOrderId.Value);

                        if (purchaseOrder != null)
                        {
                            var poItem = new PurchaseOrderItem
                            {
                                PurchaseOrderId = purchaseOrder.Id,
                                ProductVariationId = productVariation.Id,
                                Quantity = model.OrderQuantity,
                                UnitCost = model.UnitCost,

                            };

                            _context.PurchaseOrderItems.Add(poItem);

                            // Update purchase order total
                            purchaseOrder.TotalAmount += poItem.TotalCost;
                            purchaseOrder.UpdatedAt = DateTime.UtcNow;

                            await _context.SaveChangesAsync();
                        }
                    }

                    TempData["SuccessMessage"] = "Product created and added to purchase order successfully!";

                    if (model.PurchaseOrderId.HasValue)
                        return RedirectToAction(nameof(Details), new { id = model.PurchaseOrderId.Value });
                    else
                        return RedirectToAction("Index", "Products");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }

            ViewBag.Categories = _context.Categories.Where(c => c.IsActive).ToList();
            ViewBag.Suppliers = _context.Suppliers.Where(s => s.IsActive).ToList();
            return View(model);
        }

        // GET: Product Variations API for Purchase Orders
        [HttpGet]
        public async Task<IActionResult> GetProductVariations()
        {
            var variations = await _context.ProductVariations
                .Include(pv => pv.Product)
                .Where(pv => pv.IsActive)
                .Select(pv => new
                {
                    Id = pv.Id,
                    ProductName = pv.Product.ProductName,
                    Size = pv.Size,
                    Unit = pv.Unit,
                    CurrentStock = pv.StockQuantity,
                    ReorderPoint = pv.ReorderPoint,
                    Price = pv.Price,
                    DisplayName = $"{pv.Product.ProductName} - {pv.Size} {pv.Unit}"
                })
                .ToListAsync();

            return Json(variations);
        }

        // POST: Update Purchase Order Status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, PurchaseOrderStatus status)
        {
            try
            {
                var purchaseOrder = await _context.PurchaseOrders.FindAsync(id);
                if (purchaseOrder == null)
                    return Json(new { success = false, message = "Purchase order not found" });

                purchaseOrder.Status = status;
                purchaseOrder.UpdatedAt = DateTime.UtcNow;

                if (status == PurchaseOrderStatus.Approved)
                {
                    purchaseOrder.ApprovedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Status updated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        private string GeneratePONumber()
        {
            return $"PO{DateTime.Now:yyyyMMdd}{DateTime.Now.Ticks.ToString().Substring(10)}";
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
    }

    // Request/Response Models


    

  
}