using System;
using System.Collections.Generic;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Monthly Report
    /// </summary>
    public class MonthlyReportViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthName { get; set; } = string.Empty;

        // Current Month Metrics
        public decimal CurrentMonthSales { get; set; }
        public int CurrentMonthOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }

        // Previous Month Comparison
        public decimal PreviousMonthSales { get; set; }
        public int PreviousMonthOrders { get; set; }
        public decimal SalesGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }

        // Category Breakdown
        public List<CategorySalesDto> CategorySales { get; set; } = new();
        public ChartDataDto CategoryRevenueChart { get; set; } = new();

        // Top 10 Products
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Worst 5 Products
        public List<TopSellingProductDto> WorstProducts { get; set; } = new();

        // Daily Sales Trend
        public ChartDataDto DailySalesChart { get; set; } = new();

        // Customer Acquisition
        public ChartDataDto CustomerAcquisitionChart { get; set; } = new();
    }
}