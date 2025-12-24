// Areas/Admin/Controllers/OrdersController.cs
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Services;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserStatisticsService _userStatisticsService;

        public OrdersController(
       ApplicationDbContext context,
       IUserStatisticsService userStatisticsService) // Add this
        {
            _context = context;
            _userStatisticsService = userStatisticsService;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, OrderStatus newStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var oldStatus = order.Status;
            order.Status = newStatus;
            order.UpdatedAt = DateTime.UtcNow;

            // Update timestamps based on status
            switch (newStatus)
            {
                case OrderStatus.Processing:
                    order.ProcessedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Shipped:
                    order.ShippedAt = DateTime.UtcNow;
                    break;
                case OrderStatus.Delivered:
                    order.DeliveredAt = DateTime.UtcNow;
                    order.ActualDeliveryDate = DateTime.UtcNow;
                    break;
                case OrderStatus.Cancelled:
                    order.CancelledAt = DateTime.UtcNow;
                    break;
            }

            await _context.SaveChangesAsync();

            // ✅ NEW: Update user statistics when order is completed
            if (newStatus == OrderStatus.Delivered && oldStatus != OrderStatus.Delivered)
            {
                if (order.CustomerId > 0)
                {
                    await _userStatisticsService.UpdateUserStatisticsAsync(order.CustomerId);
                }
            }

            TempData["SuccessMessage"] = $"Order status updated to {newStatus}";
            return RedirectToAction(nameof(Details), new { id });
        }
        // GET: Admin/Orders
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariation)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariation)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // GET: Admin/Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            ViewBag.StatusList = Enum.GetValues(typeof(OrderStatus))
                .Cast<OrderStatus>()
                .Select(s => new SelectListItem
                {
                    Value = ((int)s).ToString(),
                    Text = s.ToString(),
                    Selected = s == order.Status
                });

            return View(order);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Order order, string? TrackingNumber)
        {
            if (id != order.Id) return BadRequest();

            var existing = await _context.Orders.FindAsync(id);
            if (existing == null) return NotFound();

            // Only update allowed fields
            existing.Status = order.Status;
            existing.TrackingNumber = string.IsNullOrWhiteSpace(TrackingNumber) ? null : TrackingNumber.Trim();
            existing.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Order #{id} updated → {order.Status}";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch
            {
                // If save fails, repopulate dropdown
                ViewBag.StatusList = Enum.GetValues(typeof(OrderStatus))
                    .Cast<OrderStatus>()
                    .Select(s => new SelectListItem
                    {
                        Value = ((int)s).ToString(),
                        Text = s.ToString(),
                        Selected = s == order.Status
                    });
                return View(order);
            }
        }
    }
}