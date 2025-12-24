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

                // Completed order statuses
                var completedStatuses = new[] { OrderStatus.Completed, OrderStatus.Delivered };

                // Total Revenue (all time)
                viewModel.TotalRevenue = await _context.Orders
                    .Where(o => completedStatuses.Contains(o.Status))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // Today's Revenue
                viewModel.TodayRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Date == today && completedStatuses.Contains(o.Status))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // This Month's Revenue
                viewModel.MonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfMonth && completedStatuses.Contains(o.Status))
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0;

                // Last Month's Revenue (for growth calculation)
                var lastMonthRevenue = await _context.Orders
                    .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate <= endOfLastMonth &&
                               completedStatuses.Contains(o.Status))
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

                // Total Customers - count distinct customer IDs from orders
                viewModel.TotalCustomers = await _context.Orders
                    .Select(o => o.CustomerId)
                    .Distinct()
                    .CountAsync();

                // New Customers This Month - customers who placed their first order this month
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

                // Top Selling Products (Top 5)
                viewModel.TopSellingProducts = await _context.OrderItems
                    .Include(oi => oi.ProductVariation)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Where(oi => completedStatuses.Contains(oi.Order.Status))
                    .GroupBy(oi => new
                    {
                        oi.ProductVariation.ProductId,
                        oi.ProductVariation.Product.ProductName,
                        oi.ProductVariation.Product.ImagePath,
                        CategoryName = oi.ProductVariation.Product.Category.CategoryName
                    })
                    .Select(g => new TopProductViewModel
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.ProductName,
                        ImagePath = g.Key.ImagePath,
                        CategoryName = g.Key.CategoryName,
                        QuantitySold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    })
                    .OrderByDescending(p => p.Revenue)
                    .Take(5)
                    .ToListAsync();

                // Top Categories (Top 5)
                var totalCategoryRevenue = await _context.OrderItems
                    .Include(oi => oi.ProductVariation.Product.Category)
                    .Where(oi => completedStatuses.Contains(oi.Order.Status))
                    .SumAsync(oi => (decimal?)(oi.Quantity * oi.UnitPrice)) ?? 0;

                viewModel.TopCategories = await _context.OrderItems
                    .Include(oi => oi.ProductVariation)
                        .ThenInclude(pv => pv.Product)
                            .ThenInclude(p => p.Category)
                    .Where(oi => completedStatuses.Contains(oi.Order.Status))
                    .GroupBy(oi => new
                    {
                        oi.ProductVariation.Product.Category.Id,
                        oi.ProductVariation.Product.Category.CategoryName,
                        oi.ProductVariation.Product.Category.Icon
                    })
                    .Select(g => new TopCategoryViewModel
                    {
                        CategoryId = g.Key.Id,
                        CategoryName = g.Key.CategoryName,
                        Icon = g.Key.Icon,
                        ProductsSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
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

                // Recent Orders (Last 10) - FIXED: Using Customer instead of User
                viewModel.RecentOrders = await _context.Orders
                    .Include(o => o.Customer)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(10)
                    .Select(o => new RecentOrderViewModel
                    {
                        OrderId = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.Customer != null
                            ? (o.Customer.FullName ?? o.Customer.Email)
                            : (o.CustomerName ?? "Guest Customer"),
                        TotalAmount = o.TotalAmount,
                        Status = o.Status.ToString(),
                        OrderDate = o.OrderDate
                    })
                    .ToListAsync();

                // Daily Sales for Last 7 Days
                var sevenDaysAgo = today.AddDays(-7);
                viewModel.DailySales = await _context.Orders
                    .Where(o => o.OrderDate >= sevenDaysAgo && completedStatuses.Contains(o.Status))
                    .GroupBy(o => o.OrderDate.Date)
                    .Select(g => new SalesDataPoint
                    {
                        Date = g.Key,
                        Label = g.Key.ToString("MMM dd"),
                        Value = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(s => s.Date)
                    .ToListAsync();

                // Fill in missing days with zero values
                var allDates = Enumerable.Range(0, 8)
                    .Select(i => today.AddDays(-i))
                    .OrderBy(d => d)
                    .ToList();

                var filledDailySales = allDates.Select(date => new SalesDataPoint
                {
                    Date = date,
                    Label = date.ToString("MMM dd"),
                    Value = viewModel.DailySales.FirstOrDefault(s => s.Date == date)?.Value ?? 0
                }).ToList();

                viewModel.DailySales = filledDailySales;

                // Monthly Sales for Last 6 Months
                var sixMonthsAgo = today.AddMonths(-6);
                viewModel.MonthlySales = await _context.Orders
                    .Where(o => o.OrderDate >= sixMonthsAgo && completedStatuses.Contains(o.Status))
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .Select(g => new SalesDataPoint
                    {
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        Value = g.Sum(o => o.TotalAmount)
                    })
                    .OrderBy(s => s.Date)
                    .ToListAsync();

                // Inventory Alerts
                viewModel.LowStockCount = await _context.ProductVariations
                    .CountAsync(pv => pv.StockQuantity > 0 && pv.StockQuantity <= pv.ReorderPoint);

                viewModel.OutOfStockCount = await _context.ProductVariations
                    .CountAsync(pv => pv.StockQuantity == 0);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading analytics dashboard");
                TempData["ErrorMessage"] = "An error occurred while loading analytics data.";
                return View(new AnalyticsDashboardViewModel());
            }
        }

        /// <summary>
        /// Get chart data for AJAX requests
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "daily")
        {
            try
            {
                var completedStatuses = new[] { OrderStatus.Completed, OrderStatus.Delivered };

                if (period == "daily")
                {
                    var today = DateTime.Today;
                    var sevenDaysAgo = today.AddDays(-7);

                    var data = await _context.Orders
                        .Where(o => o.OrderDate >= sevenDaysAgo && completedStatuses.Contains(o.Status))
                        .GroupBy(o => o.OrderDate.Date)
                        .Select(g => new
                        {
                            date = g.Key,
                            label = g.Key.ToString("MMM dd"),
                            revenue = g.Sum(o => o.TotalAmount),
                            orders = g.Count()
                        })
                        .OrderBy(s => s.date)
                        .ToListAsync();

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

                    var data = await _context.Orders
                        .Where(o => o.OrderDate >= sixMonthsAgo && completedStatuses.Contains(o.Status))
                        .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                        .Select(g => new
                        {
                            date = new DateTime(g.Key.Year, g.Key.Month, 1),
                            label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                            revenue = g.Sum(o => o.TotalAmount),
                            orders = g.Count()
                        })
                        .OrderBy(s => s.date)
                        .ToListAsync();

                    return Json(data);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data");
                return Json(new { error = "Failed to load chart data" });
            }
        }
    }
}