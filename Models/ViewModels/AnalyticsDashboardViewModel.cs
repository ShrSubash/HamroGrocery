namespace HamroGrocery.Areas.Admin.Models.ViewModels
{
    public class AnalyticsDashboardViewModel
    {
        // Overview Statistics
        public decimal TotalRevenue { get; set; }
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int TodayOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal AverageOrderValue { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }

        // Growth Percentages
        public decimal RevenueGrowth { get; set; }
        public decimal OrdersGrowth { get; set; }
        public decimal CustomersGrowth { get; set; }

        // Top Products
        public List<TopProductViewModel> TopSellingProducts { get; set; } = new();

        // Top Categories
        public List<TopCategoryViewModel> TopCategories { get; set; } = new();

        // Recent Orders
        public List<RecentOrderViewModel> RecentOrders { get; set; } = new();

        // Sales Data for Charts
        public List<SalesDataPoint> DailySales { get; set; } = new();
        public List<SalesDataPoint> MonthlySales { get; set; } = new();

        // Inventory Alerts
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    public class TopProductViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ImagePath { get; set; }
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public string CategoryName { get; set; }
    }

    public class TopCategoryViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string Icon { get; set; }
        public int ProductsSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RecentOrderViewModel
    {
        public int OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string CustomerName { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
    }

    public class SalesDataPoint
    {
        public string Label { get; set; }
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }
}