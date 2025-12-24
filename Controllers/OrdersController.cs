using HamroGrocery.Data;
using HamroGrocery.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Controllers
{
    [Authorize] // Only logged-in users can see their orders
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrdersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Orders/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userId = User.GetUserIdInt(); // assuming your extension method returns int version

            var orders = await _context.Orders
                .Where(o => o.CustomerId == userId)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariation)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Optional: Details page for a specific order
        // GET: /Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.GetUserIdInt();

            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.ProductVariation)
                .FirstOrDefaultAsync(o => o.Id == id && o.CustomerId == userId);

            if (order == null) return NotFound();

            return View(order);
        }
    }
}