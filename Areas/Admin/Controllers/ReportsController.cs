using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Reports
        public async Task<IActionResult> Index()
        {
            ViewData["Title"] = "Reports & Analytics";

            // Simple approach - just return the view without complex data for now
            return View();
        }

        // GET: Admin/Reports/Sales
        public async Task<IActionResult> Sales()
        {
            ViewData["Title"] = "Sales Reports";
            return View();
        }

        // GET: Admin/Reports/Inventory
        public async Task<IActionResult> Inventory()
        {
            ViewData["Title"] = "Inventory Reports";
            return View();
        }

        // GET: Admin/Reports/Customers
        public async Task<IActionResult> Customers()
        {
            ViewData["Title"] = "Customer Reports";
            return View();
        }
    }
}