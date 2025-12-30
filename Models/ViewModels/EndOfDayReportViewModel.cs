using System;
using System.Collections.Generic;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for End of Day Report
    /// Shows today's sales, orders, and key metrics
    /// </summary>
    public class EndOfDayReportViewModel
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;

        // Key Metrics
        public decimal TodayTotalSales { get; set; }
        public int TodayTotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }

        // Payment Breakdown
        public decimal CashPayments { get; set; }
        public decimal OnlinePayments { get; set; }
        public int CashOrdersCount { get; set; }
        public int OnlineOrdersCount { get; set; }

        // Status Breakdown
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        // Top Products Today
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Low Stock Alerts
        public List<LowStockAlertDto> LowStockItems { get; set; } = new();

        // Chart Data for Payment Methods
        public ChartDataDto PaymentMethodChart { get; set; } = new();

        // Comparison with Yesterday
        public decimal YesterdaySales { get; set; }
        public decimal SalesGrowthPercentage { get; set; }
    }

    /// <summary>
    /// DTO for Top Selling Products
    /// </summary>
    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string ImagePath { get; set; } = "/images/placeholder-product.jpg";
    }

    /// <summary>
    /// DTO for Low Stock Alerts
    /// </summary>
    public class LowStockAlertDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderPoint { get; set; }
        public string StockStatus { get; set; } = string.Empty; // "Low", "Out of Stock"
    }

    /// <summary>
    /// Generic Chart Data DTO for Chart.js
    /// </summary>
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Data { get; set; } = new();
        public List<string> BackgroundColors { get; set; } = new();
        public string ChartType { get; set; } = "pie"; // pie, bar, line, doughnut
    }
}