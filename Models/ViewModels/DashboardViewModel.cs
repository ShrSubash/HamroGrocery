using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Admin Dashboard with real-time statistics
    /// </summary>
    public class DashboardViewModel
    {
        // Chart Data
        public Dictionary<string, decimal> MonthlySalesData { get; set; } = new();  // e.g., {"Jan": 1500, "Feb": 2000}
        public Dictionary<string, int> UserGrowthData { get; set; } = new();  // e.g., {"Jan": 50, "Feb": 75}
        public Dictionary<string, int> OrdersByStatus { get; set; } = new();

        // Key Metrics
        public decimal TotalSales { get; set; }
        public decimal SalesGrowthPercentage { get; set; }
        public string SalesGrowthPeriod { get; set; } = "from last month";

        public int TotalOrders { get; set; }
        public decimal OrdersGrowthPercentage { get; set; }
        public string OrdersGrowthPeriod { get; set; } = "from last week";

        public int ActiveProducts { get; set; }
        public int LowStockCount { get; set; }

        public int TotalCustomers { get; set; }
        public decimal CustomersGrowthPercentage { get; set; }
        public string CustomersGrowthPeriod { get; set; } = "this month";

        // Additional Metrics (NEW - for enhanced dashboard)
        public decimal AverageOrderValue { get; set; }
        public decimal TodaysSales { get; set; }
        public decimal WeeklySales { get; set; }
        public int TotalCategories { get; set; }
        public int OutOfStockCount { get; set; }

        // Recent Orders
        public List<RecentOrderDto> RecentOrders { get; set; } = new();

        // Top Products
        public List<TopProductDto> TopProducts { get; set; } = new();

        // Alerts
        public List<AlertDto> Alerts { get; set; } = new();
    }

    /// <summary>
    /// DTO for Recent Order display
    /// </summary>
    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerInitials { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusBadgeClass { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    /// <summary>
    /// DTO for Top Selling Products
    /// </summary>
    public class TopProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = "/images/placeholder-product.jpg";
        public int UnitsSold { get; set; }
        public decimal Revenue { get; set; }
        public int StockQuantity { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string StockBadgeClass { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for Dashboard Alerts
    /// </summary>
    public class AlertDto
    {
        public string Icon { get; set; } = "fas fa-info-circle";
        public string IconColor { get; set; } = "#008060";
        public string IconBackground { get; set; } = "rgba(0, 128, 96, 0.12)";
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // success, warning, danger, info
    }

    /// <summary>
    /// DTO for chart data API responses with filtering (NEW - for dynamic chart updates)
    /// Used by the GetChartData endpoint to return filtered sales/orders data
    /// </summary>
    public class DashboardChartDataDto
    {
        public Dictionary<string, decimal> SalesData { get; set; } = new();
        public Dictionary<string, int> OrdersData { get; set; } = new();
        public Dictionary<string, int> CustomersData { get; set; } = new();
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ComparisonPercentage { get; set; }
    }
}