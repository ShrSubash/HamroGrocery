using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace HamroGrocery.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    TotalSales = await CalculateTotalSalesAsync(),
                    SalesGrowthPercentage = await CalculateSalesGrowthAsync(),
                    TotalOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered),
                    OrdersGrowthPercentage = await CalculateOrdersGrowthAsync(),
                    ActiveProducts = await _context.Products.CountAsync(p => p.IsActive && !p.IsDeleted),
                    LowStockCount = await CalculateLowStockCountAsync(),
                    TotalCustomers = await _context.Users.CountAsync(u => u.Role == UserRole.Customer),
                    CustomersGrowthPercentage = await CalculateCustomersGrowthAsync(),
                    RecentOrders = await GetRecentOrdersAsync(),
                    TopProducts = await GetTopProductsAsync(),
                    Alerts = await GenerateAlertsAsync(),

                    // Additional Metrics
                    AverageOrderValue = await CalculateAverageOrderValueAsync(),
                    TodaysSales = await CalculateTodaysSalesAsync(),
                    WeeklySales = await CalculateWeeklySalesAsync(),
                    TotalCategories = await _context.Categories.CountAsync(c => !c.IsDeleted),
                    OutOfStockCount = await _context.ProductVariations.CountAsync(pv => pv.StockQuantity == 0),

                    // Initial Chart Data (Last 30 Days)
                    MonthlySalesData = await GetSalesDataAsync(30, "day"),
                    OrdersByStatus = await _context.Orders
                        .GroupBy(o => o.Status.ToString())
                        .ToDictionaryAsync(g => g.Key, g => g.Count())
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading admin dashboard");
                return View(new DashboardViewModel());
            }
        }

        /// <summary>
        /// API endpoint for fetching filtered chart data dynamically
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "30days", string groupBy = "day")
        {
            try
            {
                int days = period switch
                {
                    "7days" => 7,
                    "30days" => 30,
                    "90days" => 90,
                    "365days" => 365,
                    _ => 30
                };

                var salesData = await GetSalesDataAsync(days, groupBy);
                var ordersData = await GetOrdersDataAsync(days, groupBy);
                var customersData = await GetCustomersDataAsync(days, groupBy);

                var startDate = DateTime.UtcNow.AddDays(-days);
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered && o.OrderDate >= startDate)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                var totalOrders = await _context.Orders
                    .CountAsync(o => o.Status == OrderStatus.Delivered && o.OrderDate >= startDate);

                // Calculate comparison with previous period
                var previousStartDate = startDate.AddDays(-days);
                var previousRevenue = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered && o.OrderDate >= previousStartDate && o.OrderDate < startDate)
                    .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

                var comparisonPercentage = previousRevenue > 0
                    ? Math.Round((totalRevenue - previousRevenue) / previousRevenue * 100, 1)
                    : (totalRevenue > 0 ? 100 : 0);

                var result = new DashboardChartDataDto
                {
                    SalesData = salesData,
                    OrdersData = ordersData,
                    CustomersData = customersData,
                    TotalRevenue = totalRevenue,
                    TotalOrders = totalOrders,
                    AverageOrderValue = totalOrders > 0 ? totalRevenue / totalOrders : 0m,
                    ComparisonPercentage = comparisonPercentage
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching chart data");
                return Json(new DashboardChartDataDto());
            }
        }

        #region Helper Methods

        private async Task<decimal> CalculateTotalSalesAsync()
        {
            return await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        private async Task<decimal> CalculateSalesGrowthAsync()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var current = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            var previous = await _context.Orders
                .Where(o => o.OrderDate >= startOfLastMonth && o.OrderDate < startOfMonth && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;

            if (previous == 0) return current > 0 ? 100 : 0;
            return Math.Round((current - previous) / previous * 100, 1);
        }

        private async Task<decimal> CalculateOrdersGrowthAsync()
        {
            var now = DateTime.UtcNow;
            var thisWeekStart = now.AddDays(-7);
            var lastWeekStart = now.AddDays(-14);

            var thisWeek = await _context.Orders.CountAsync(o => o.OrderDate >= thisWeekStart && o.Status == OrderStatus.Delivered);
            var lastWeek = await _context.Orders.CountAsync(o => o.OrderDate >= lastWeekStart && o.OrderDate < thisWeekStart && o.Status == OrderStatus.Delivered);

            if (lastWeek == 0) return thisWeek > 0 ? 100 : 0;
            return Math.Round((decimal)(thisWeek - lastWeek) / lastWeek * 100, 1);
        }

        private async Task<int> CalculateLowStockCountAsync()
        {
            return await _context.ProductVariations
                .CountAsync(pv => pv.StockQuantity <= pv.ReorderPoint && pv.StockQuantity > 0);
        }

        private async Task<decimal> CalculateCustomersGrowthAsync()
        {
            var today = DateTime.UtcNow;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfLastMonth = startOfMonth.AddMonths(-1);

            var thisMonth = await _context.Users.CountAsync(u => u.Role == UserRole.Customer && u.CreatedAt >= startOfMonth);
            var lastMonth = await _context.Users.CountAsync(u => u.Role == UserRole.Customer && u.CreatedAt >= startOfLastMonth && u.CreatedAt < startOfMonth);

            if (lastMonth == 0) return thisMonth > 0 ? 100 : 0;
            return Math.Round((decimal)(thisMonth - lastMonth) / lastMonth * 100, 1);
        }

        private async Task<decimal> CalculateAverageOrderValueAsync()
        {
            var totalOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Delivered);
            if (totalOrders == 0) return 0m;

            var totalRevenue = await CalculateTotalSalesAsync();
            return Math.Round(totalRevenue / totalOrders, 2);
        }

        private async Task<decimal> CalculateTodaysSalesAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.Orders
                .Where(o => o.OrderDate >= today && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        private async Task<decimal> CalculateWeeklySalesAsync()
        {
            var weekStart = DateTime.UtcNow.AddDays(-7);
            return await _context.Orders
                .Where(o => o.OrderDate >= weekStart && o.Status == OrderStatus.Delivered)
                .SumAsync(o => (decimal?)o.TotalAmount) ?? 0m;
        }

        /// <summary>
        /// Get sales data grouped by day, week, or month
        /// </summary>
        private async Task<Dictionary<string, decimal>> GetSalesDataAsync(int days, string groupBy)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.OrderDate >= startDate)
                .Select(o => new { o.OrderDate, o.TotalAmount })
                .ToListAsync();

            return groupBy.ToLower() switch
            {
                "day" => orders
                    .GroupBy(o => o.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(
                        g => g.Key.ToString("dd MMM"),
                        g => g.Sum(o => o.TotalAmount)
                    ),
                "week" => orders
                    .GroupBy(o => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
                        o.OrderDate, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .OrderBy(g => g.Key)
                    .ToDictionary(
                        g => $"Week {g.Key}",
                        g => g.Sum(o => o.TotalAmount)
                    ),
                "month" => orders
                    .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToDictionary(
                        g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                        g => g.Sum(o => o.TotalAmount)
                    ),
                _ => orders
                    .GroupBy(o => o.OrderDate.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(
                        g => g.Key.ToString("dd MMM"),
                        g => g.Sum(o => o.TotalAmount)
                    )
            };
        }

        /// <summary>
        /// Get orders count grouped by period
        /// </summary>
        private async Task<Dictionary<string, int>> GetOrdersDataAsync(int days, string groupBy)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var orders = await _context.Orders
                .Where(o => o.Status == OrderStatus.Delivered && o.OrderDate >= startDate)
                .Select(o => o.OrderDate)
                .ToListAsync();

            return groupBy.ToLower() switch
            {
                "day" => orders
                    .GroupBy(o => o.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("dd MMM"), g => g.Count()),
                "month" => orders
                    .GroupBy(o => new { o.Year, o.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToDictionary(g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"), g => g.Count()),
                _ => orders
                    .GroupBy(o => o.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("dd MMM"), g => g.Count())
            };
        }

        /// <summary>
        /// Get customer registration data grouped by period
        /// </summary>
        private async Task<Dictionary<string, int>> GetCustomersDataAsync(int days, string groupBy)
        {
            var startDate = DateTime.UtcNow.AddDays(-days);
            var users = await _context.Users
                .Where(u => u.Role == UserRole.Customer && u.CreatedAt >= startDate)
                .Select(u => u.CreatedAt)
                .ToListAsync();

            return groupBy.ToLower() switch
            {
                "day" => users
                    .GroupBy(u => u.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("dd MMM"), g => g.Count()),
                "month" => users
                    .GroupBy(u => new { u.Year, u.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .ToDictionary(g => new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"), g => g.Count()),
                _ => users
                    .GroupBy(u => u.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("dd MMM"), g => g.Count())
            };
        }

        private async Task<List<RecentOrderDto>> GetRecentOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.Customer)
                .Where(o => o.Status == OrderStatus.Delivered && o.Customer != null)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    OrderId = o.Id,
                    OrderNumber = o.OrderNumber ?? $"#ORD-{o.Id:D6}",
                    CustomerName = $"{o.Customer!.FirstName} {o.Customer.LastName}".Trim(),
                    CustomerInitials = $"{o.Customer.FirstName.Substring(0, Math.Min(1, o.Customer.FirstName.Length))}{o.Customer.LastName.Substring(0, Math.Min(1, o.Customer.LastName.Length))}".ToUpper(),
                    OrderDate = o.OrderDate,
                    Status = "Delivered",
                    StatusBadgeClass = "bg-success",
                    TotalAmount = o.TotalAmount
                })
                .ToListAsync();
        }

        private async Task<List<TopProductDto>> GetTopProductsAsync()
        {
            var topProducts = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Where(oi => oi.Order.Status == OrderStatus.Delivered)
                .GroupBy(oi => new
                {
                    oi.Product.Id,
                    oi.Product.ProductName,
                    CategoryName = oi.Product.Category.CategoryName,
                    oi.Product.ImagePath
                })
                .Select(g => new
                {
                    g.Key.Id,
                    g.Key.ProductName,
                    g.Key.CategoryName,
                    g.Key.ImagePath,
                    UnitsSold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToListAsync();

            var result = new List<TopProductDto>();
            foreach (var p in topProducts)
            {
                var stock = await _context.ProductVariations
                    .Where(pv => pv.ProductId == p.Id)
                    .SumAsync(pv => pv.StockQuantity);

                result.Add(new TopProductDto
                {
                    ProductName = p.ProductName,
                    CategoryName = p.CategoryName,
                    ImageUrl = string.IsNullOrEmpty(p.ImagePath) ? "/images/no-image.jpg" : p.ImagePath,
                    UnitsSold = p.UnitsSold,
                    Revenue = p.Revenue,
                    StockQuantity = stock,
                    StockStatus = stock == 0 ? "Out of Stock" : stock < 20 ? $"Low ({stock})" : "In Stock",
                    StockBadgeClass = stock == 0 ? "bg-danger" : stock < 20 ? "bg-warning text-dark" : "bg-success"
                });
            }
            return result;
        }

        private async Task<List<AlertDto>> GenerateAlertsAsync()
        {
            var alerts = new List<AlertDto>();
            var lowStock = await CalculateLowStockCountAsync();
            var outOfStock = await _context.ProductVariations.CountAsync(pv => pv.StockQuantity == 0);
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == OrderStatus.Pending || o.Status == OrderStatus.Processing);

            if (outOfStock > 0)
                alerts.Add(new AlertDto { Title = $"{outOfStock} Products Out of Stock", Description = "Immediate action required", Icon = "fas fa-exclamation-circle", Type = "danger" });

            if (lowStock > 0)
                alerts.Add(new AlertDto { Title = $"{lowStock} Products Low Stock", Description = "Requires restocking soon", Icon = "fas fa-exclamation-triangle", Type = "warning" });

            if (pendingOrders > 0)
                alerts.Add(new AlertDto { Title = $"{pendingOrders} Orders Pending", Description = "Awaiting processing", Icon = "fas fa-clock", Type = "info" });

            if (alerts.Count == 0)
                alerts.Add(new AlertDto { Title = "All Systems Good!", Description = "No critical alerts", Icon = "fas fa-check-circle", Type = "success" });

            return alerts;
        }

        #endregion
    }
}