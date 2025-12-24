using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class SuppliersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SuppliersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Suppliers
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Suppliers";

            var suppliers = await _context.Suppliers
                .OrderBy(s => s.SupplierName)
                .ToListAsync();

            return View(suppliers);
        }

        // GET: Admin/Suppliers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null) return NotFound();

            ViewData["Title"] = $"Supplier Details - {supplier.SupplierName}";
            return View(supplier);
        }

        // GET: Admin/Suppliers/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Create New Supplier";
            return View();
        }

        // POST: Admin/Suppliers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Supplier supplier)
        {
            if (ModelState.IsValid)
            {
                supplier.CreatedAt = DateTime.UtcNow;
                _context.Add(supplier);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Supplier created successfully!";
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Admin/Suppliers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier == null) return NotFound();

            ViewData["Title"] = $"Edit Supplier - {supplier.SupplierName}";
            return View(supplier);
        }

        // POST: Admin/Suppliers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Supplier supplier)
        {
            if (id != supplier.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    supplier.UpdatedAt = DateTime.UtcNow;
                    _context.Update(supplier);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Supplier updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplierExists(supplier.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(supplier);
        }

        // GET: Admin/Suppliers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var supplier = await _context.Suppliers
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supplier == null) return NotFound();

            ViewData["Title"] = $"Delete Supplier - {supplier.SupplierName}";
            return View(supplier);
        }

        // POST: Admin/Suppliers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supplier = await _context.Suppliers.FindAsync(id);
            if (supplier != null)
            {
                _context.Suppliers.Remove(supplier);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Supplier deleted successfully!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool SupplierExists(int id)
        {
            return _context.Suppliers.Any(e => e.Id == id);
        }
    }
}