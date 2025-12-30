using ClosedXML.Excel;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using HamroGrocery.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace HamroGrocery.Services
{
    /// <summary>
    /// Report Service Implementation
    /// Generates comprehensive reports and analytics for the admin panel
    /// </summary>
    public class ReportService : IReportService
    {
        private readonly ApplicationDbContext _context;

        public ReportService(ApplicationDbContext context)
        {
            _context = context;
        }

        #region End of Day Report

        /// <summary>
        /// Generate End of Day Report
        /// </summary>
        public async Task<EndOfDayReportViewModel> GetEndOfDayReportAsync(DateTime? reportDate = null)
        {
            var date = reportDate ?? DateTime.Today;
            var startOfDay = date.Date;
            var endOfDay = date.Date.AddDays(1).AddTicks(-1);

            // Get yesterday's date for comparison
            var yesterdayStart = date.AddDays(-1).Date;
            var yesterdayEnd = yesterdayStart.AddDays(1).AddTicks(-1);

            var report = new EndOfDayReportViewModel
            {
                ReportDate = date
            };

            // Get today's orders
            var todayOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfDay && o.OrderDate <= endOfDay)
                .ToListAsync();

            // Get yesterday's orders for comparison
            var yesterdayOrders = await _context.Orders
                .Where(o => o.OrderDate >= yesterdayStart && o.OrderDate <= yesterdayEnd)
                .ToListAsync();

            // Calculate key metrics
            report.TodayTotalSales = todayOrders.Sum(o => o.TotalAmount);
            report.TodayTotalOrders = todayOrders.Count;
            report.AverageOrderValue = report.TodayTotalOrders > 0
                ? report.TodayTotalSales / report.TodayTotalOrders
                : 0;

            // Get total items sold today
            report.TotalItemsSold = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startOfDay && oi.Order.OrderDate <= endOfDay)
                .SumAsync(oi => oi.Quantity);

            // Payment method breakdown
            report.CashPayments = todayOrders
                .Where(o => o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalAmount);
            report.OnlinePayments = todayOrders
                .Where(o => !o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalAmount);
            report.CashOrdersCount = todayOrders
                .Count(o => o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase));
            report.OnlineOrdersCount = todayOrders.Count - report.CashOrdersCount;

            // Order status breakdown
            report.PendingOrders = todayOrders.Count(o => o.Status == OrderStatus.Pending);
            report.ProcessingOrders = todayOrders.Count(o => o.Status == OrderStatus.Processing);
            report.CompletedOrders = todayOrders.Count(o => o.Status == OrderStatus.Delivered);
            report.CancelledOrders = todayOrders.Count(o => o.Status == OrderStatus.Cancelled);

            // Top 5 products sold today
            report.TopProducts = await GetTopSellingProductsAsync(startOfDay, endOfDay, 5);

            // Low stock alerts
            report.LowStockItems = await GetLowStockItemsAsync();

            // Payment method chart data
            report.PaymentMethodChart = new ChartDataDto
            {
                Labels = new List<string> { "Cash on Delivery", "Online Payment" },
                Data = new List<decimal> { report.CashPayments, report.OnlinePayments },
                BackgroundColors = new List<string> { "#28a745", "#007bff" },
                ChartType = "doughnut"
            };

            // Yesterday comparison
            report.YesterdaySales = yesterdayOrders.Sum(o => o.TotalAmount);
            report.SalesGrowthPercentage = report.YesterdaySales > 0
                ? ((report.TodayTotalSales - report.YesterdaySales) / report.YesterdaySales) * 100
                : 0;

            return report;
        }

        #endregion

        #region Weekly Report

        /// <summary>
        /// Generate Weekly Report (Last 7 Days)
        /// </summary>
        public async Task<WeeklyReportViewModel> GetWeeklyReportAsync(DateTime? endDate = null)
        {
            var end = endDate ?? DateTime.Today;
            var start = end.AddDays(-6); // Last 7 days including today

            var report = new WeeklyReportViewModel
            {
                StartDate = start,
                EndDate = end
            };

            // Get this week's orders
            var thisWeekOrders = await _context.Orders
                .Where(o => o.OrderDate >= start && o.OrderDate <= end.AddDays(1).AddTicks(-1))
                .Include(o => o.Customer)
                .ToListAsync();

            // Get last week's orders for comparison
            var lastWeekStart = start.AddDays(-7);
            var lastWeekEnd = end.AddDays(-7);
            var lastWeekOrders = await _context.Orders
                .Where(o => o.OrderDate >= lastWeekStart && o.OrderDate <= lastWeekEnd.AddDays(1).AddTicks(-1))
                .ToListAsync();

            // This week metrics
            report.ThisWeekSales = thisWeekOrders.Sum(o => o.TotalAmount);
            report.ThisWeekOrders = thisWeekOrders.Count;
            report.AverageOrderValue = report.ThisWeekOrders > 0
                ? report.ThisWeekSales / report.ThisWeekOrders
                : 0;

            // Customer metrics
            report.TotalCustomers = thisWeekOrders.Select(o => o.CustomerId).Distinct().Count();

            // New customers (registered this week)
            report.NewCustomers = await _context.Users
                .Where(u => u.CreatedAt >= start && u.CreatedAt <= end.AddDays(1).AddTicks(-1))
                .CountAsync();

            // Last week comparison
            report.LastWeekSales = lastWeekOrders.Sum(o => o.TotalAmount);
            report.LastWeekOrders = lastWeekOrders.Count;
            report.SalesGrowthPercentage = report.LastWeekSales > 0
                ? ((report.ThisWeekSales - report.LastWeekSales) / report.LastWeekSales) * 100
                : 0;
            report.OrdersGrowthPercentage = report.LastWeekOrders > 0
                ? ((report.ThisWeekOrders - report.LastWeekOrders) / (decimal)report.LastWeekOrders) * 100
                : 0;

            // Top categories
            report.TopCategories = await GetTopCategoriesAsync(start, end.AddDays(1).AddTicks(-1), 5);

            // Daily sales chart (7 days)
            report.DailySalesChart = await GetDailySalesChartAsync(start, end);

            // Top products
            report.TopProducts = await GetTopSellingProductsAsync(start, end.AddDays(1).AddTicks(-1), 5);

            // Payment method chart
            var cashPayments = thisWeekOrders
                .Where(o => o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalAmount);
            var onlinePayments = report.ThisWeekSales - cashPayments;

            report.PaymentMethodChart = new ChartDataDto
            {
                Labels = new List<string> { "Cash on Delivery", "Online Payment" },
                Data = new List<decimal> { cashPayments, onlinePayments },
                BackgroundColors = new List<string> { "#28a745", "#007bff" },
                ChartType = "pie"
            };

            return report;
        }

        #endregion

        #region Monthly Report

        /// <summary>
        /// Generate Monthly Report
        /// </summary>
        public async Task<MonthlyReportViewModel> GetMonthlyReportAsync(int? year = null, int? month = null)
        {
            var currentYear = year ?? DateTime.Now.Year;
            var currentMonth = month ?? DateTime.Now.Month;

            var startDate = new DateTime(currentYear, currentMonth, 1);
            var endDate = startDate.AddMonths(1).AddTicks(-1);

            // Previous month
            var prevMonthStart = startDate.AddMonths(-1);
            var prevMonthEnd = startDate.AddTicks(-1);

            var report = new MonthlyReportViewModel
            {
                Year = currentYear,
                Month = currentMonth,
                MonthName = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(currentMonth)
            };

            // Current month orders
            var currentOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Include(o => o.Customer)
                .ToListAsync();

            // Previous month orders
            var previousOrders = await _context.Orders
                .Where(o => o.OrderDate >= prevMonthStart && o.OrderDate <= prevMonthEnd)
                .ToListAsync();

            // Current month metrics
            report.CurrentMonthSales = currentOrders.Sum(o => o.TotalAmount);
            report.CurrentMonthOrders = currentOrders.Count;
            report.AverageOrderValue = report.CurrentMonthOrders > 0
                ? report.CurrentMonthSales / report.CurrentMonthOrders
                : 0;

            // Customer metrics
            report.TotalCustomers = currentOrders.Select(o => o.CustomerId).Distinct().Count();
            report.NewCustomers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();

            // Previous month comparison
            report.PreviousMonthSales = previousOrders.Sum(o => o.TotalAmount);
            report.PreviousMonthOrders = previousOrders.Count;
            report.SalesGrowthPercentage = report.PreviousMonthSales > 0
                ? ((report.CurrentMonthSales - report.PreviousMonthSales) / report.PreviousMonthSales) * 100
                : 0;
            report.OrdersGrowthPercentage = report.PreviousMonthOrders > 0
                ? ((report.CurrentMonthOrders - report.PreviousMonthOrders) / (decimal)report.PreviousMonthOrders) * 100
                : 0;

            // Category breakdown
            report.CategorySales = await GetTopCategoriesAsync(startDate, endDate, 10);

            // Category revenue chart
            report.CategoryRevenueChart = new ChartDataDto
            {
                Labels = report.CategorySales.Select(c => c.CategoryName).ToList(),
                Data = report.CategorySales.Select(c => c.Revenue).ToList(),
                BackgroundColors = GenerateColors(report.CategorySales.Count),
                ChartType = "bar"
            };

            // Top 10 and worst 5 products
            report.TopProducts = await GetTopSellingProductsAsync(startDate, endDate, 10);
            report.WorstProducts = await GetWorstSellingProductsAsync(startDate, endDate, 5);

            // Daily sales trend
            report.DailySalesChart = await GetDailySalesChartAsync(startDate, endDate);

            return report;
        }

        #endregion

        #region Yearly Report

        /// <summary>
        /// Generate Yearly Report
        /// </summary>
        public async Task<YearlyReportViewModel> GetYearlyReportAsync(int? year = null)
        {
            var currentYear = year ?? DateTime.Now.Year;
            var startDate = new DateTime(currentYear, 1, 1);
            var endDate = new DateTime(currentYear, 12, 31, 23, 59, 59);

            // Previous year
            var prevYearStart = new DateTime(currentYear - 1, 1, 1);
            var prevYearEnd = new DateTime(currentYear - 1, 12, 31, 23, 59, 59);

            var report = new YearlyReportViewModel
            {
                Year = currentYear
            };

            // Current year orders
            var currentYearOrders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                .Include(o => o.Customer)
                .ToListAsync();

            // Previous year orders
            var previousYearOrders = await _context.Orders
                .Where(o => o.OrderDate >= prevYearStart && o.OrderDate <= prevYearEnd)
                .ToListAsync();

            // Annual metrics
            report.AnnualRevenue = currentYearOrders.Sum(o => o.TotalAmount);
            report.TotalOrders = currentYearOrders.Count;
            report.AverageOrderValue = report.TotalOrders > 0
                ? report.AnnualRevenue / report.TotalOrders
                : 0;

            // Customer metrics
            report.TotalCustomers = currentYearOrders.Select(o => o.CustomerId).Distinct().Count();
            report.NewCustomers = await _context.Users
                .Where(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate)
                .CountAsync();

            // Year-over-year comparison
            report.PreviousYearRevenue = previousYearOrders.Sum(o => o.TotalAmount);
            report.PreviousYearOrders = previousYearOrders.Count;
            report.RevenueGrowthPercentage = report.PreviousYearRevenue > 0
                ? ((report.AnnualRevenue - report.PreviousYearRevenue) / report.PreviousYearRevenue) * 100
                : 0;
            report.OrdersGrowthPercentage = report.PreviousYearOrders > 0
                ? ((report.TotalOrders - report.PreviousYearOrders) / (decimal)report.PreviousYearOrders) * 100
                : 0;

            // Monthly revenue chart (12 months)
            report.MonthlyRevenueChart = await GetMonthlyRevenueChartAsync(currentYear);

            // Monthly orders chart
            report.MonthlyOrdersChart = await GetMonthlyOrdersChartAsync(currentYear);

            // Top 10 products of the year
            report.TopProducts = await GetTopSellingProductsAsync(startDate, endDate, 10);

            // Category performance
            report.CategoryPerformance = await GetTopCategoriesAsync(startDate, endDate, 10);

            // Category revenue chart
            report.CategoryRevenueChart = new ChartDataDto
            {
                Labels = report.CategoryPerformance.Select(c => c.CategoryName).ToList(),
                Data = report.CategoryPerformance.Select(c => c.Revenue).ToList(),
                BackgroundColors = GenerateColors(report.CategoryPerformance.Count),
                ChartType = "doughnut"
            };

            // Quarterly breakdown
            report.QuarterlyData = await GetQuarterlyDataAsync(currentYear);

            return report;
        }

        #endregion

        #region Inventory Report

        /// <summary>
        /// Generate Inventory Report
        /// </summary>
        public async Task<InventoryReportViewModel> GetInventoryReportAsync()
        {
            var report = new InventoryReportViewModel
            {
                ReportDate = DateTime.Today
            };

            // Get all products with their variations
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductVariations)
                .ToListAsync();

            // Overall statistics
            report.TotalProducts = products.Count;
            report.ActiveProducts = products.Count(p => p.IsActive);
            report.InactiveProducts = products.Count(p => !p.IsActive);

            // Calculate total stock value
            report.TotalStockValue = products
                .Where(p => p.IsActive)
                .Sum(p => p.Price * p.StockQuantity);

            // Stock status summary
            report.InStockProducts = products.Count(p => p.StockQuantity > p.ReorderPoint);
            report.LowStockProducts = products.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.ReorderPoint);
            report.OutOfStockProducts = products.Count(p => p.StockQuantity == 0);
            report.OverstockProducts = products.Count(p => p.StockQuantity > p.MaxStockLevel);

            // Category-wise inventory
            report.CategoryInventory = await GetCategoryInventoryAsync();

            // Category stock value chart
            report.CategoryStockValueChart = new ChartDataDto
            {
                Labels = report.CategoryInventory.Select(c => c.CategoryName).ToList(),
                Data = report.CategoryInventory.Select(c => c.TotalValue).ToList(),
                BackgroundColors = GenerateColors(report.CategoryInventory.Count),
                ChartType = "bar"
            };

            // Low stock items
            report.LowStockItems = await GetLowStockInventoryItemsAsync();

            // Out of stock items
            report.OutOfStockItems = await GetOutOfStockItemsAsync();

            // Overstock items
            report.OverstockItems = await GetOverstockItemsAsync();

            // Top value products
            report.TopValueProducts = await GetTopValueProductsAsync(10);

            // Stock level distribution chart
            report.StockLevelChart = new ChartDataDto
            {
                Labels = new List<string> { "In Stock", "Low Stock", "Out of Stock", "Overstock" },
                Data = new List<decimal>
                {
                    report.InStockProducts,
                    report.LowStockProducts,
                    report.OutOfStockProducts,
                    report.OverstockProducts
                },
                BackgroundColors = new List<string> { "#28a745", "#ffc107", "#dc3545", "#17a2b8" },
                ChartType = "doughnut"
            };

            return report;
        }

        #endregion

        #region Sales Report

        /// <summary>
        /// Generate Sales Report for Custom Date Range
        /// </summary>
        public async Task<SalesReportViewModel> GetSalesReportAsync(DateTime startDate, DateTime endDate)
        {
            // Ensure end date includes the entire day
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);

            var report = new SalesReportViewModel
            {
                StartDate = startDate,
                EndDate = endDate
            };

            // Get orders in date range
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= adjustedEndDate)
                .Include(o => o.Customer)
                .Include(o => o.OrderItems)
                .ToListAsync();

            // Summary metrics
            report.TotalRevenue = orders.Sum(o => o.TotalAmount);
            report.TotalOrders = orders.Count;
            report.AverageOrderValue = report.TotalOrders > 0
                ? report.TotalRevenue / report.TotalOrders
                : 0;

            // Total items sold
            report.TotalItemsSold = orders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity);

            // Unique customers
            report.UniqueCustomers = orders.Select(o => o.CustomerId).Distinct().Count();

            // Payment method breakdown
            report.CashPayments = orders
                .Where(o => o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase))
                .Sum(o => o.TotalAmount);
            report.OnlinePayments = report.TotalRevenue - report.CashPayments;
            report.CashOrdersCount = orders
                .Count(o => o.PaymentMethod.Contains("Cash", StringComparison.OrdinalIgnoreCase));
            report.OnlineOrdersCount = report.TotalOrders - report.CashOrdersCount;

            // Payment method chart
            report.PaymentMethodChart = new ChartDataDto
            {
                Labels = new List<string> { "Cash on Delivery", "Online Payment" },
                Data = new List<decimal> { report.CashPayments, report.OnlinePayments },
                BackgroundColors = new List<string> { "#28a745", "#007bff" },
                ChartType = "pie"
            };

            // Order status breakdown
            report.PendingOrders = orders.Count(o => o.Status == OrderStatus.Pending);
            report.ProcessingOrders = orders.Count(o => o.Status == OrderStatus.Processing);
            report.ShippedOrders = orders.Count(o => o.Status == OrderStatus.Shipped);
            report.DeliveredOrders = orders.Count(o => o.Status == OrderStatus.Delivered);
            report.CancelledOrders = orders.Count(o => o.Status == OrderStatus.Cancelled);

            // Order status chart
            report.OrderStatusChart = new ChartDataDto
            {
                Labels = new List<string> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" },
                Data = new List<decimal>
                {
                    report.PendingOrders,
                    report.ProcessingOrders,
                    report.ShippedOrders,
                    report.DeliveredOrders,
                    report.CancelledOrders
                },
                BackgroundColors = new List<string> { "#ffc107", "#17a2b8", "#6f42c1", "#28a745", "#dc3545" },
                ChartType = "bar"
            };

            // Daily sales chart
            report.DailySalesChart = await GetDailySalesChartAsync(startDate, endDate);

            // Top products
            report.TopProducts = await GetTopSellingProductsAsync(startDate, adjustedEndDate, 10);

            // Top categories
            report.TopCategories = await GetTopCategoriesAsync(startDate, adjustedEndDate, 5);

            // Detailed order list
            report.OrderDetails = orders.Select(o => new OrderDetailDto
            {
                OrderId = o.Id,
                OrderNumber = o.OrderNumber,
                OrderDate = o.OrderDate,
                CustomerName = o.CustomerName,
                PaymentMethod = o.PaymentMethod,
                Status = o.Status.ToString(),
                SubTotal = o.SubTotal,
                TaxAmount = o.TaxAmount,
                ShippingAmount = o.ShippingAmount,
                TotalAmount = o.TotalAmount
            })
            .OrderByDescending(o => o.OrderDate)
            .ToList();

            return report;
        }

        #endregion

        #region Excel Export Methods

        /// <summary>
        /// Export Sales Report to Excel
        /// </summary>
        public async Task<byte[]> ExportSalesReportToExcelAsync(DateTime startDate, DateTime endDate)
        {
            var report = await GetSalesReportAsync(startDate, endDate);

            using (var workbook = new XLWorkbook())
            {
                // Summary Sheet
                var summarySheet = workbook.Worksheets.Add("Summary");
                summarySheet.Cell("A1").Value = "Sales Report Summary";
                summarySheet.Cell("A1").Style.Font.Bold = true;
                summarySheet.Cell("A1").Style.Font.FontSize = 16;

                summarySheet.Cell("A3").Value = "Period:";
                summarySheet.Cell("B3").Value = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}";

                summarySheet.Cell("A5").Value = "Total Revenue:";
                summarySheet.Cell("B5").Value = report.TotalRevenue;
                summarySheet.Cell("B5").Style.NumberFormat.Format = "$#,##0.00";

                summarySheet.Cell("A6").Value = "Total Orders:";
                summarySheet.Cell("B6").Value = report.TotalOrders;

                summarySheet.Cell("A7").Value = "Average Order Value:";
                summarySheet.Cell("B7").Value = report.AverageOrderValue;
                summarySheet.Cell("B7").Style.NumberFormat.Format = "$#,##0.00";

                summarySheet.Cell("A8").Value = "Total Items Sold:";
                summarySheet.Cell("B8").Value = report.TotalItemsSold;

                summarySheet.Cell("A9").Value = "Unique Customers:";
                summarySheet.Cell("B9").Value = report.UniqueCustomers;

                // Orders Detail Sheet
                var ordersSheet = workbook.Worksheets.Add("Orders");
                ordersSheet.Cell("A1").Value = "Order Number";
                ordersSheet.Cell("B1").Value = "Date";
                ordersSheet.Cell("C1").Value = "Customer";
                ordersSheet.Cell("D1").Value = "Payment Method";
                ordersSheet.Cell("E1").Value = "Status";
                ordersSheet.Cell("F1").Value = "Subtotal";
                ordersSheet.Cell("G1").Value = "Tax";
                ordersSheet.Cell("H1").Value = "Shipping";
                ordersSheet.Cell("I1").Value = "Total";

                ordersSheet.Row(1).Style.Font.Bold = true;
                ordersSheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

                int row = 2;
                foreach (var order in report.OrderDetails)
                {
                    ordersSheet.Cell($"A{row}").Value = order.OrderNumber;
                    ordersSheet.Cell($"B{row}").Value = order.OrderDate;
                    ordersSheet.Cell($"C{row}").Value = order.CustomerName;
                    ordersSheet.Cell($"D{row}").Value = order.PaymentMethod;
                    ordersSheet.Cell($"E{row}").Value = order.Status;
                    ordersSheet.Cell($"F{row}").Value = order.SubTotal;
                    ordersSheet.Cell($"G{row}").Value = order.TaxAmount;
                    ordersSheet.Cell($"H{row}").Value = order.ShippingAmount;
                    ordersSheet.Cell($"I{row}").Value = order.TotalAmount;
                    row++;
                }

                ordersSheet.Columns().AdjustToContents();

                // Top Products Sheet
                var productsSheet = workbook.Worksheets.Add("Top Products");
                productsSheet.Cell("A1").Value = "Product Name";
                productsSheet.Cell("B1").Value = "Category";
                productsSheet.Cell("C1").Value = "Quantity Sold";
                productsSheet.Cell("D1").Value = "Revenue";

                productsSheet.Row(1).Style.Font.Bold = true;
                productsSheet.Row(1).Style.Fill.BackgroundColor = XLColor.LightGray;

                row = 2;
                foreach (var product in report.TopProducts)
                {
                    productsSheet.Cell($"A{row}").Value = product.ProductName;
                    productsSheet.Cell($"B{row}").Value = product.CategoryName;
                    productsSheet.Cell($"C{row}").Value = product.QuantitySold;
                    productsSheet.Cell($"D{row}").Value = product.Revenue;
                    row++;
                }

                productsSheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        /// <summary>
        /// Export Inventory Report to Excel
        /// </summary>
        public async Task<byte[]> ExportInventoryReportToExcelAsync()
        {
            var report = await GetInventoryReportAsync();

            using (var workbook = new XLWorkbook())
            {
                // Summary Sheet
                var summarySheet = workbook.Worksheets.Add("Summary");
                summarySheet.Cell("A1").Value = "Inventory Report";
                summarySheet.Cell("A1").Style.Font.Bold = true;
                summarySheet.Cell("A1").Style.Font.FontSize = 16;

                summarySheet.Cell("A3").Value = "Report Date:";
                summarySheet.Cell("B3").Value = report.ReportDate.ToString("yyyy-MM-dd");

                summarySheet.Cell("A5").Value = "Total Products:";
                summarySheet.Cell("B5").Value = report.TotalProducts;

                summarySheet.Cell("A6").Value = "In Stock:";
                summarySheet.Cell("B6").Value = report.InStockProducts;

                summarySheet.Cell("A7").Value = "Low Stock:";
                summarySheet.Cell("B7").Value = report.LowStockProducts;

                summarySheet.Cell("A8").Value = "Out of Stock:";
                summarySheet.Cell("B8").Value = report.OutOfStockProducts;

                summarySheet.Cell("A9").Value = "Total Stock Value:";
                summarySheet.Cell("B9").Value = report.TotalStockValue;
                summarySheet.Cell("B9").Style.NumberFormat.Format = "$#,##0.00";

                // Low Stock Items
                var lowStockSheet = workbook.Worksheets.Add("Low Stock Items");
                lowStockSheet.Cell("A1").Value = "Product Name";
                lowStockSheet.Cell("B1").Value = "Category";
                lowStockSheet.Cell("C1").Value = "Current Stock";
                lowStockSheet.Cell("D1").Value = "Reorder Point";

                lowStockSheet.Row(1).Style.Font.Bold = true;
                lowStockSheet.Row(1).Style.Fill.BackgroundColor = XLColor.Orange;

                int row = 2;
                foreach (var item in report.LowStockItems)
                {
                    lowStockSheet.Cell($"A{row}").Value = item.ProductName;
                    lowStockSheet.Cell($"B{row}").Value = item.CategoryName;
                    lowStockSheet.Cell($"C{row}").Value = item.CurrentStock;
                    lowStockSheet.Cell($"D{row}").Value = item.ReorderPoint;
                    row++;
                }

                lowStockSheet.Columns().AdjustToContents();

                // Out of Stock Items
                var outOfStockSheet = workbook.Worksheets.Add("Out of Stock");
                outOfStockSheet.Cell("A1").Value = "Product Name";
                outOfStockSheet.Cell("B1").Value = "Category";
                outOfStockSheet.Cell("C1").Value = "Reorder Point";

                outOfStockSheet.Row(1).Style.Font.Bold = true;
                outOfStockSheet.Row(1).Style.Fill.BackgroundColor = XLColor.Red;

                row = 2;
                foreach (var item in report.OutOfStockItems)
                {
                    outOfStockSheet.Cell($"A{row}").Value = item.ProductName;
                    outOfStockSheet.Cell($"B{row}").Value = item.CategoryName;
                    outOfStockSheet.Cell($"C{row}").Value = item.ReorderPoint;
                    row++;
                }

                outOfStockSheet.Columns().AdjustToContents();

                using (var stream = new System.IO.MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Get top selling products for a date range
        /// </summary>
        private async Task<List<TopSellingProductDto>> GetTopSellingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int count)
        {
            var topProducts = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.ProductName,
                    oi.CategoryName,
                    oi.Product.ImagePath
                })
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName ?? "Uncategorized",
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    ImagePath = string.IsNullOrEmpty(g.Key.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : g.Key.ImagePath
                })
                .OrderByDescending(p => p.Revenue)
                .Take(count)
                .ToListAsync();

            return topProducts;
        }

        /// <summary>
        /// Get worst selling products for a date range
        /// </summary>
        private async Task<List<TopSellingProductDto>> GetWorstSellingProductsAsync(
            DateTime startDate,
            DateTime endDate,
            int count)
        {
            var worstProducts = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .GroupBy(oi => new
                {
                    oi.ProductId,
                    oi.ProductName,
                    oi.CategoryName,
                    oi.Product.ImagePath
                })
                .Select(g => new TopSellingProductDto
                {
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName ?? "Uncategorized",
                    QuantitySold = g.Sum(oi => oi.Quantity),
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    ImagePath = string.IsNullOrEmpty(g.Key.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : g.Key.ImagePath
                })
                .OrderBy(p => p.Revenue)
                .Take(count)
                .ToListAsync();

            return worstProducts;
        }

        /// <summary>
        /// Get top categories by revenue
        /// </summary>
        private async Task<List<CategorySalesDto>> GetTopCategoriesAsync(
            DateTime startDate,
            DateTime endDate,
            int count)
        {
            var categories = await _context.OrderItems
                .Where(oi => oi.Order.OrderDate >= startDate && oi.Order.OrderDate <= endDate)
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .GroupBy(oi => new
                {
                    CategoryId = oi.Product.CategoryId,
                    CategoryName = oi.Product.Category.CategoryName
                })
                .Select(g => new CategorySalesDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    Revenue = g.Sum(oi => oi.TotalPrice),
                    OrderCount = g.Select(oi => oi.OrderId).Distinct().Count(),
                    ProductsSold = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(c => c.Revenue)
                .Take(count)
                .ToListAsync();

            return categories;
        }

        /// <summary>
        /// Get low stock alerts for End of Day Report (LowStockAlertDto)
        /// </summary>
        private async Task<List<LowStockAlertDto>> GetLowStockItemsAsync()
        {
            var lowStockItems = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= p.ReorderPoint)
                .Select(p => new LowStockAlertDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    StockStatus = "Low Stock"
                })
                .OrderBy(p => p.CurrentStock)
                .Take(10)
                .ToListAsync();

            return lowStockItems;
        }

        /// <summary>
        /// Get low stock items for Inventory Report (InventoryItemDto)
        /// </summary>
        private async Task<List<InventoryItemDto>> GetLowStockInventoryItemsAsync()
        {
            var lowStockItems = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0 && p.StockQuantity <= p.ReorderPoint)
                .Select(p => new InventoryItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    SKU = p.SKU ?? "N/A",
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    MaxStockLevel = p.MaxStockLevel,
                    UnitPrice = p.Price,
                    TotalValue = p.Price * p.StockQuantity,
                    StockStatus = "Low Stock",
                    ImagePath = string.IsNullOrEmpty(p.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : p.ImagePath
                })
                .OrderBy(p => p.CurrentStock)
                .ToListAsync();

            return lowStockItems;
        }

        /// <summary>
        /// Get out of stock items
        /// </summary>
        private async Task<List<InventoryItemDto>> GetOutOfStockItemsAsync()
        {
            var outOfStockItems = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity == 0)
                .Select(p => new InventoryItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    SKU = p.SKU ?? "N/A",
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    MaxStockLevel = p.MaxStockLevel,
                    UnitPrice = p.Price,
                    TotalValue = 0,
                    StockStatus = "Out of Stock",
                    ImagePath = string.IsNullOrEmpty(p.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : p.ImagePath
                })
                .OrderBy(p => p.ProductName)
                .ToListAsync();

            return outOfStockItems;
        }

        /// <summary>
        /// Get overstock items (above max stock level)
        /// </summary>
        private async Task<List<InventoryItemDto>> GetOverstockItemsAsync()
        {
            var overstockItems = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > p.MaxStockLevel)
                .Select(p => new InventoryItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    SKU = p.SKU ?? "N/A",
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    MaxStockLevel = p.MaxStockLevel,
                    UnitPrice = p.Price,
                    TotalValue = p.Price * p.StockQuantity,
                    StockStatus = "Overstock",
                    ImagePath = string.IsNullOrEmpty(p.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : p.ImagePath
                })
                .OrderByDescending(p => p.CurrentStock - p.MaxStockLevel)
                .ToListAsync();

            return overstockItems;
        }

        /// <summary>
        /// Get top value products (by total stock value)
        /// </summary>
        private async Task<List<InventoryItemDto>> GetTopValueProductsAsync(int count)
        {
            var topValueProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive && p.StockQuantity > 0)
                .Select(p => new InventoryItemDto
                {
                    ProductId = p.Id,
                    ProductName = p.ProductName,
                    CategoryName = p.Category.CategoryName,
                    SKU = p.SKU ?? "N/A",
                    CurrentStock = p.StockQuantity,
                    ReorderPoint = p.ReorderPoint,
                    MaxStockLevel = p.MaxStockLevel,
                    UnitPrice = p.Price,
                    TotalValue = p.Price * p.StockQuantity,
                    StockStatus = p.StockQuantity <= p.ReorderPoint ? "Low Stock" : "In Stock",
                    ImagePath = string.IsNullOrEmpty(p.ImagePath)
                        ? "/images/placeholder-product.jpg"
                        : p.ImagePath
                })
                .OrderByDescending(p => p.TotalValue)
                .Take(count)
                .ToListAsync();

            return topValueProducts;
        }

        /// <summary>
        /// Get category-wise inventory summary
        /// </summary>
        private async Task<List<CategoryInventoryDto>> GetCategoryInventoryAsync()
        {
            var categoryInventory = await _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive)
                .GroupBy(p => new
                {
                    p.CategoryId,
                    p.Category.CategoryName
                })
                .Select(g => new CategoryInventoryDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.CategoryName,
                    ProductCount = g.Count(),
                    TotalStock = g.Sum(p => p.StockQuantity),
                    TotalValue = g.Sum(p => p.Price * p.StockQuantity),
                    LowStockCount = g.Count(p => p.StockQuantity > 0 && p.StockQuantity <= p.ReorderPoint),
                    OutOfStockCount = g.Count(p => p.StockQuantity == 0)
                })
                .OrderByDescending(c => c.TotalValue)
                .ToListAsync();

            return categoryInventory;
        }

        /// <summary>
        /// Get daily sales chart data for date range
        /// </summary>
        private async Task<ChartDataDto> GetDailySalesChartAsync(DateTime startDate, DateTime endDate)
        {
            var adjustedEndDate = endDate.Date.AddDays(1).AddTicks(-1);
            var days = (endDate.Date - startDate.Date).Days + 1;

            var dailySales = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate <= adjustedEndDate)
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Sales = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Date)
                .ToListAsync();

            // Create complete date range with zero values for missing days
            var labels = new List<string>();
            var data = new List<decimal>();

            for (int i = 0; i < days; i++)
            {
                var date = startDate.AddDays(i).Date;
                labels.Add(date.ToString("MMM dd"));

                var daySale = dailySales.FirstOrDefault(ds => ds.Date == date);
                data.Add(daySale?.Sales ?? 0);
            }

            return new ChartDataDto
            {
                Labels = labels,
                Data = data,
                BackgroundColors = new List<string> { "#007bff" },
                ChartType = "line"
            };
        }

        /// <summary>
        /// Get monthly revenue chart for a year
        /// </summary>
        private async Task<ChartDataDto> GetMonthlyRevenueChartAsync(int year)
        {
            var monthlyRevenue = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                labels.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month));

                var monthData = monthlyRevenue.FirstOrDefault(mr => mr.Month == month);
                data.Add(monthData?.Revenue ?? 0);
            }

            return new ChartDataDto
            {
                Labels = labels,
                Data = data,
                BackgroundColors = GenerateColors(12),
                ChartType = "bar"
            };
        }

        /// <summary>
        /// Get monthly orders chart for a year
        /// </summary>
        private async Task<ChartDataDto> GetMonthlyOrdersChartAsync(int year)
        {
            var monthlyOrders = await _context.Orders
                .Where(o => o.OrderDate.Year == year)
                .GroupBy(o => o.OrderDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Orders = g.Count()
                })
                .ToListAsync();

            var labels = new List<string>();
            var data = new List<decimal>();

            for (int month = 1; month <= 12; month++)
            {
                labels.Add(CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(month));

                var monthData = monthlyOrders.FirstOrDefault(mo => mo.Month == month);
                data.Add(monthData?.Orders ?? 0);
            }

            return new ChartDataDto
            {
                Labels = labels,
                Data = data,
                BackgroundColors = new List<string> { "#28a745" },
                ChartType = "line"
            };
        }

        /// <summary>
        /// Get quarterly data for a year
        /// </summary>
        private async Task<List<QuarterlyDataDto>> GetQuarterlyDataAsync(int year)
        {
            var quarterlyData = new List<QuarterlyDataDto>();

            for (int quarter = 1; quarter <= 4; quarter++)
            {
                var startMonth = (quarter - 1) * 3 + 1;
                var startDate = new DateTime(year, startMonth, 1);
                var endDate = startDate.AddMonths(3).AddTicks(-1);

                var orders = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.OrderDate <= endDate)
                    .ToListAsync();

                var revenue = orders.Sum(o => o.TotalAmount);
                var orderCount = orders.Count;

                quarterlyData.Add(new QuarterlyDataDto
                {
                    Quarter = quarter,
                    QuarterName = $"Q{quarter} {year}",
                    Revenue = revenue,
                    Orders = orderCount,
                    AverageOrderValue = orderCount > 0 ? revenue / orderCount : 0
                });
            }

            return quarterlyData;
        }

        /// <summary>
        /// Generate color palette for charts
        /// </summary>
        private List<string> GenerateColors(int count)
        {
            var colors = new List<string>
            {
                "#007bff", "#28a745", "#dc3545", "#ffc107", "#17a2b8",
                "#6f42c1", "#e83e8c", "#fd7e14", "#20c997", "#6610f2",
                "#343a40", "#f8f9fa", "#495057", "#6c757d", "#adb5bd"
            };

            // If we need more colors, repeat the palette
            while (colors.Count < count)
            {
                colors.AddRange(colors.Take(Math.Min(colors.Count, count - colors.Count)));
            }

            return colors.Take(count).ToList();
        }

        #endregion
    }
}