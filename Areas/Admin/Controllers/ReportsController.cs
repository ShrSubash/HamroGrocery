using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Services; // ← ADD THIS
using Microsoft.Extensions.Logging; // ← ADD THIS
using System;
using System.Threading.Tasks;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IReportService _reportService; // ← ADD THIS
        private readonly ILogger<ReportsController> _logger; // ← ADD THIS

        // ← UPDATE CONSTRUCTOR
        public ReportsController(
            ApplicationDbContext context,
            IReportService reportService,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _reportService = reportService;
            _logger = logger;
        }

        // ========================================
        // YOUR EXISTING METHODS (Keep or Replace)
        // ========================================

        // GET: Admin/Reports
        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["Title"] = "Reports & Analytics";

                // Get quick stats for dashboard
                var endOfDayReport = await _reportService.GetEndOfDayReportAsync();

                return View(endOfDayReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reports dashboard");
                ViewData["Title"] = "Reports & Analytics";
                return View();
            }
        }

        // GET: Admin/Reports/Sales - UPDATED VERSION
        public async Task<IActionResult> Sales(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                ViewData["Title"] = "Sales Reports";

                // Default to last 30 days
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                var report = await _reportService.GetSalesReportAsync(start, end);

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Sales report");
                TempData["ErrorMessage"] = "Failed to generate Sales report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Reports/Inventory - UPDATED VERSION
        public async Task<IActionResult> Inventory()
        {
            try
            {
                ViewData["Title"] = "Inventory Reports";

                var report = await _reportService.GetInventoryReportAsync();

                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Inventory report");
                TempData["ErrorMessage"] = "Failed to generate Inventory report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Reports/Customers - KEEP AS IS (or enhance later)
        public async Task<IActionResult> Customers()
        {
            ViewData["Title"] = "Customer Reports";
            return View();
        }

        // ========================================
        // NEW METHODS - ADD THESE
        // ========================================

        // GET: Admin/Reports/EndOfDay
        public async Task<IActionResult> EndOfDay(DateTime? date)
        {
            try
            {
                ViewData["Title"] = "End of Day Report";
                var report = await _reportService.GetEndOfDayReportAsync(date);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating End of Day report");
                TempData["ErrorMessage"] = "Failed to generate End of Day report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Reports/Weekly
        public async Task<IActionResult> Weekly(DateTime? endDate)
        {
            try
            {
                ViewData["Title"] = "Weekly Report";
                var report = await _reportService.GetWeeklyReportAsync(endDate);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Weekly report");
                TempData["ErrorMessage"] = "Failed to generate Weekly report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Reports/Monthly
        public async Task<IActionResult> Monthly(int? year, int? month)
        {
            try
            {
                ViewData["Title"] = "Monthly Report";
                var report = await _reportService.GetMonthlyReportAsync(year, month);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Monthly report");
                TempData["ErrorMessage"] = "Failed to generate Monthly report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Admin/Reports/Yearly
        public async Task<IActionResult> Yearly(int? year)
        {
            try
            {
                ViewData["Title"] = "Yearly Report";
                var report = await _reportService.GetYearlyReportAsync(year);
                return View(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating Yearly report");
                TempData["ErrorMessage"] = "Failed to generate Yearly report.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ========================================
        // EXCEL EXPORT METHODS
        // ========================================

        [HttpGet]
        public async Task<IActionResult> ExportSalesReport(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today;

                var excelData = await _reportService.ExportSalesReportToExcelAsync(start, end);
                var fileName = $"SalesReport_{start:yyyyMMdd}_to_{end:yyyyMMdd}.xlsx";

                return File(
                    excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Sales report");
                TempData["ErrorMessage"] = "Failed to export Sales report.";
                return RedirectToAction(nameof(Sales));
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportInventoryReport()
        {
            try
            {
                var excelData = await _reportService.ExportInventoryReportToExcelAsync();
                var fileName = $"InventoryReport_{DateTime.Today:yyyyMMdd}.xlsx";

                return File(
                    excelData,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    fileName
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting Inventory report");
                TempData["ErrorMessage"] = "Failed to export Inventory report.";
                return RedirectToAction(nameof(Inventory));
            }
        }
    }
}