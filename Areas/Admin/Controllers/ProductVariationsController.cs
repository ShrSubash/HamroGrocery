using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ProductVariationsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductVariationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ProductVariations
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Product Variations";

            var productVariations = await _context.ProductVariations
                .Include(pv => pv.Product)
                .ThenInclude(p => p.Category)
                .OrderBy(pv => pv.Product.ProductName)
                .ToListAsync();

            return View(productVariations);
        }

        // GET: Admin/ProductVariations/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var productVariation = await _context.ProductVariations
                .Include(pv => pv.Product)
                .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productVariation == null) return NotFound();

            ViewData["Title"] = $"Variation Details - {productVariation.Product.ProductName}";
            return View(productVariation);
        }

        // GET: Admin/ProductVariations/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create Product Variation";
            ViewBag.Products = _context.Products.ToList();
            return View();
        }

        // POST: Admin/ProductVariations/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductVariation productVariation)
        {
            if (ModelState.IsValid)
            {
                productVariation.CreatedAt = DateTime.UtcNow;
                _context.Add(productVariation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product variation created successfully!";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = _context.Products.ToList();
            return View(productVariation);
        }

        // GET: Admin/ProductVariations/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var productVariation = await _context.ProductVariations.FindAsync(id);
            if (productVariation == null) return NotFound();

            ViewData["Title"] = "Edit Product Variation";
            ViewBag.Products = _context.Products.ToList();
            return View(productVariation);
        }

        // POST: Admin/ProductVariations/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductVariation productVariation)
        {
            if (id != productVariation.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    productVariation.UpdatedAt = DateTime.UtcNow;
                    _context.Update(productVariation);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Product variation updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductVariationExists(productVariation.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Products = _context.Products.ToList();
            return View(productVariation);
        }

        // GET: Admin/ProductVariations/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var productVariation = await _context.ProductVariations
                .Include(pv => pv.Product)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (productVariation == null) return NotFound();

            ViewData["Title"] = "Delete Product Variation";
            return View(productVariation);
        }

        // POST: Admin/ProductVariations/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var productVariation = await _context.ProductVariations.FindAsync(id);
            if (productVariation != null)
            {
                _context.ProductVariations.Remove(productVariation);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Product variation deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductVariationExists(int id)
        {
            return _context.ProductVariations.Any(e => e.Id == id);
        }
    }
}