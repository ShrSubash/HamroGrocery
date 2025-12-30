using System;
using System.Collections.Generic;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Yearly Report
    /// </summary>
    public class YearlyReportViewModel
    {
        public int Year { get; set; }

        // Annual Metrics
        public decimal AnnualRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomers { get; set; }

        // Year-over-Year Comparison
        public decimal PreviousYearRevenue { get; set; }
        public int PreviousYearOrders { get; set; }
        public decimal RevenueGrowthPercentage { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }

        // Monthly Trend (12 months)
        public ChartDataDto MonthlyRevenueChart { get; set; } = new();
        public ChartDataDto MonthlyOrdersChart { get; set; } = new();

        // Top 10 Products of the Year
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Category Performance
        public List<CategorySalesDto> CategoryPerformance { get; set; } = new();
        public ChartDataDto CategoryRevenueChart { get; set; } = new();

        // Quarterly Breakdown
        public List<QuarterlyDataDto> QuarterlyData { get; set; } = new();
    }

    /// <summary>
    /// DTO for Quarterly Data
    /// </summary>
    public class QuarterlyDataDto
    {
        public int Quarter { get; set; }
        public string QuarterName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int Orders { get; set; }
        public decimal AverageOrderValue { get; set; }
    }
}