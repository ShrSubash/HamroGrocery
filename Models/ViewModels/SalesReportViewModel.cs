using System;
using System.Collections.Generic;
using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Sales Report with Custom Date Range
    /// </summary>
    public class SalesReportViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // Summary Metrics
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalItemsSold { get; set; }
        public int UniqueCustomers { get; set; }

        // Payment Method Breakdown
        public decimal CashPayments { get; set; }
        public decimal OnlinePayments { get; set; }
        public int CashOrdersCount { get; set; }
        public int OnlineOrdersCount { get; set; }
        public ChartDataDto PaymentMethodChart { get; set; } = new();

        // Order Status Breakdown
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public ChartDataDto OrderStatusChart { get; set; } = new();

        // Sales Trend (Daily)
        public ChartDataDto DailySalesChart { get; set; } = new();

        // Top Products
        public List<TopSellingProductDto> TopProducts { get; set; } = new();

        // Top Categories
        public List<CategorySalesDto> TopCategories { get; set; } = new();

        // Detailed Order List
        public List<OrderDetailDto> OrderDetails { get; set; } = new();
    }

    /// <summary>
    /// DTO for Order Details in Sales Report
    /// </summary>
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal ShippingAmount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}