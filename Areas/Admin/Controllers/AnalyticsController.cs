using HamroGrocery.Areas.Admin.Models.ViewModels;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AnalyticsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AnalyticsController> _logger;

        public AnalyticsController(ApplicationDbContext context, ILogger<AnalyticsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                ViewData["Title"] = "Analytics Dashboard";

                var viewModel = new AnalyticsDashboardViewModel();

                // Date ranges
                var today = DateTime.Today;
                var startOfMonth = new DateTime(today.Year, today.Month, 1);
                var startOfLastMonth = startOfMonth.AddMonths(-1);
                var endOfLastMonth = startOfMonth.AddDays(-1);

                // Total Revenue
                viewModel.TotalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // Today's Revenue
                viewModel.TodayRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Date == today &&
                           (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // This Month's Revenue
                viewModel.MonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfMonth &&
                           (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // Last Month's Revenue
                var lastMonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth &&
                           (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // Calculate Revenue Growth
                if (lastMonthRevenue > 0)
                {
                    viewModel.RevenueGrowth = ((viewModel.MonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100;
                }

                // Total Orders
                viewModel.TotalOrders = await _context.Orders.CountAsync();

                // Today's Orders
                viewModel.TodayOrders = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == today);

                // Pending Orders
                viewModel.PendingOrders = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);

                // Average Order Value
                if (viewModel.TotalOrders > 0)
                {
                    viewModel.AverageOrderValue = viewModel.TotalRevenue / viewModel.TotalOrders;
                }

                // Total Customers
                viewModel.TotalCustomers = await _context.Orders
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .CountAsync();

                // New Customers This Month
                var customersThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= startOfMonth)
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .ToListAsync();

                var customersBeforeThisMonth = await _context.Orders
                    .Where(o => o.OrderDate < startOfMonth)
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .ToListAsync();

                viewModel.NewCustomersThisMonth = customersThisMonth
                    .Except(customersBeforeThisMonth)
                    .Count();

                // Get completed order IDs
                var completedOrderIds = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)
                    .Select(o => o.Id)
                    .ToListAsync();

                // Top Selling Products
                viewModel.TopSellingProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                    .Where(oi => completedOrderIds.Contains(oi.OrderId))
                    .GroupBy(oi => new
                    {
                        oi.ProductId,
                        oi.ProductName,
                        oi.Product.ImagePath,
                        CategoryName = oi.CategoryName ?? "Uncategorized"
                    })
                    .Select(g => new TopProductViewModel
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        ImagePath = g.Key.ImagePath ?? "/images/placeholder-product.jpg",
                        CategoryName = g.Key.CategoryName,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToListAsync();

                // Total Category Revenue
                var totalCategoryRevenue = await _context.OrderItems
                    .Where(oi => completedOrderIds.Contains(oi.OrderId))
                    .SumAsync(oi => (decimal?)oi.TotalPrice) ?? 0;

                // Top Categories
                viewModel.TopCategories = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                    .Where(oi => completedOrderIds.Contains(oi.OrderId) && oi.CategoryName != null)
                    .GroupBy(oi => new
                    {
                        CategoryId = oi.Product.CategoryId,
                        CategoryName = oi.CategoryName,
                        Icon = oi.Product.Category.Icon
                    })
                    .Select(g => new TopCategoryViewModel
                    {
                        CategoryId = g.Key.CategoryId,
                        CategoryName = g.Key.CategoryName ?? "Uncategorized",
                        Icon = g.Key.Icon,
                        ProductsSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.TotalPrice)
                    })
                    .OrderByDescending(c => c.Revenue)
                    .Take(5)
                    .ToListAsync();

                // Calculate percentages for categories
                foreach (var category in viewModel.TopCategories)
                {
                    category.Percentage = totalCategoryRevenue > 0
                        ? (category.Revenue / totalCategoryRevenue) * 100
                        : 0;
                }

                // Recent Orders
                viewModel.RecentOrders = await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.CustomerName ?? "Guest Customer",
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        OrderDate = o.OrderDate
                    })
                    .ToListAsync();

                // ✅ FIXED: Daily Sales for Last 7 Days - Added parentheses
                var sevenDaysAgo = today.AddDays(-7);
                var dailySalesData = await _context.Orders
                    .Where(o => o.OrderDate >= sevenDaysAgo &&
                           (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)) // ← Parentheses added
                    .Select(o => new { o.OrderDate.Date, o.TotalAmount })
                    .ToListAsync();

                var dailyGrouped = dailySalesData
                    .GroupBy(o => o.Date)
                    .Select(g => new SalesDataPoint
                    {
                        Date = g.Key,
                        Label = g.Key.ToString("MMM dd"),
                        Value = g.Sum(o => o.TotalAmount)
                    })
                    .ToList();

                // Fill in missing days
                var allDates = Enumerable.Range(0, 8)
                    .Select(i => today.AddDays(-i))
                    .OrderBy(d => d)
                    .ToList();

                viewModel.DailySales = allDates.Select(date => new SalesDataPoint
                {
                    Date = date,
                    Label = date.ToString("MMM dd"),
                    Value = dailyGrouped.FirstOrDefault(s => s.Date == date)?.Value ?? 0
                }).ToList();

                // ✅ FIXED: Monthly Sales for Last 6 Months - Added parentheses & ToListAsync
                var sixMonthsAgo = today.AddMonths(-6);
                var monthlyOrdersData = await _context.Orders
                    .Where(o => o.OrderDate >= sixMonthsAgo &&
                           (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered)) // ← Parentheses added
                    .Select(o => new
                    {
                        o.OrderDate.Year,
                        o.OrderDate.Month,
                        o.TotalAmount
                    })
                    .ToListAsync(); // ← Get data first

                viewModel.MonthlySales = monthlyOrdersData
                    .GroupBy(o => new { o.Year, o.Month })
                    .Select(g => new SalesDataPoint
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Value = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(s => s.Date)
                    .ToList();

                // Inventory Alerts
                viewModel.LowStockCount = await _context.Products
                    .CountAsync(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= p.ReorderPoint);

                viewModel.OutOfStockCount = await _context.Products
                    .CountAsync(p => p.IsActive && p.StockQuantity == 0);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard: {Message}", ex.Message);
                TempData["ErrorMessage"] = $"An error occurred while loading analytics data: {ex.Message}";
                return View(new AnalyticsDashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "daily")
        {
            try
            {
                if (period == "daily")
                {
                    var today = DateTime.Today;
                    var sevenDaysAgo = today.AddDays(-7);

                    // ✅ FIXED: Added parentheses around OR condition
                    var ordersData = await _context.Orders
                        .Where(o => o.OrderDate >= sevenDaysAgo &&
                               (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                        .Select(o => new { o.OrderDate.Date, o.TotalAmount })
                        .ToListAsync();

                    var data = ordersData
                        .GroupBy(o => o.Date)
                        .Select(g => new
                        {
                            date = g.Key,
                            label = g.Key.ToString("MMM dd"),
                            revenue = g.Sum(o => o.TotalAmount),
                            orders = g.Count()
                        })
                        .OrderBy(s => s.date)
                        .ToList();

                    // Fill in missing days
                    var allDates = Enumerable.Range(0, 8)
                        .Select(i => today.AddDays(-i))
                        .OrderBy(d => d)
                        .ToList();

                    var filledData = allDates.Select(date => new
                    {
                        date = date,
                        label = date.ToString("MMM dd"),
                        revenue = data.FirstOrDefault(d => d.date == date)?.revenue ?? 0,
                        orders = data.FirstOrDefault(d => d.date == date)?.orders ?? 0
                    }).ToList();

                    return Json(filledData);
                }
                else // monthly
                {
                    var today = DateTime.Today;
                    var sixMonthsAgo = today.AddMonths(-6);

                    // ✅ FIXED: Added parentheses and ToListAsync
                    var ordersData = await _context.Orders
                        .Where(o => o.OrderDate >= sixMonthsAgo &&
                               (o.Status == OrderStatus.Completed || o.Status == OrderStatus.Delivered))
                        .Select(o => new
                        {
                            o.OrderDate.Year,
                            o.OrderDate.Month,
                            o.TotalAmount
                        })
                        .ToListAsync();

                    var data = ordersData
                        .GroupBy(o => new { o.Year, o.Month })
                        .Select(g => new
                        {
                            date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            revenue = g.Sum(o => o.TotalAmount),
                            orders = g.Count()
                        })
                        .OrderBy(s => s.date)
                        .ToList();

                    return Json(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data: {Message}", ex.Message);
                return Json(new { error = "Failed to load chart data", message = ex.Message });
            }
        }
    }
}