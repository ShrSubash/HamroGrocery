using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AnnouncementsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnnouncementsController> _logger;

        public AnnouncementsController(ApplicationDbContext context, ILogger<AnnouncementsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Admin/Announcements
        public async Task<IActionResult> Index()
        {
            var announcements = await _context.Announcements
                .Where(a => !a.IsDeleted)
                .OrderByDescending(a => a.IsActive)
                .ThenByDescending(a => a.Priority)
                .ThenByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(announcements);
        }

        // GET: Admin/Announcements/Create
        public IActionResult Create()
        {
            var model = new Announcement
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(7)
            };
            return View(model);
        }

        // POST: Admin/Announcements/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Announcement announcement)
        {
            if (ModelState.IsValid)
            {
                announcement.CreatedBy = User.Identity?.Name ?? "System";
                announcement.CreatedAt = DateTime.UtcNow;

                _context.Add(announcement);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Announcement created successfully!";
                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }

        // GET: Admin/Announcements/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null) return NotFound();

            return View(announcement);
        }

        // POST: Admin/Announcements/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Announcement announcement)
        {
            if (id != announcement.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    announcement.UpdatedAt = DateTime.UtcNow;
                    _context.Update(announcement);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Announcement updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnnouncementExists(announcement.Id))
                        return NotFound();
                    throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(announcement);
        }

        // POST: Admin/Announcements/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement != null)
            {
                announcement.IsDeleted = true;
                announcement.IsActive = false;
                announcement.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Announcement deleted successfully!";
            return RedirectToAction(nameof(Index));
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.Id == id);
        }
    }
}