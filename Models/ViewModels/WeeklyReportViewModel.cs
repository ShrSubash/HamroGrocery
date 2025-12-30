using System;
using System.Collections.Generic;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Weekly Report (Last 7 Days)
    /// </summary>
    public class WeeklyReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // This Week Metrics
        public decimal ThisWeekSales { get; set; }
        public int ThisWeekOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }

        // Last Week Comparison
        public decimal LastWeekSales { get; set; }
        public int LastWeekOrders { get; set; }
        public decimal SalesGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }

        // Top Categories
        public List<CategorySalesDto> TopCategories { get; set; } = new();

        // Daily Sales Trend (7 days)
        public ChartDataDto DailySalesChart { get; set; } = new();

        // Top Products This Week
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Payment Method Distribution
        public ChartDataDto PaymentMethodChart { get; set; } = new();
    }

    /// <summary>
    /// DTO for Category Sales
    /// </summary>
    public class CategorySalesDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public int ProductsSold { get; set; }
    }
}