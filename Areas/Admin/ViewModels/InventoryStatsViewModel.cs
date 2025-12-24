using HamroGrocery.Models.Entities;

namespace HamroGrocery.Areas.Admin.Models.ViewModels
{
    /// <summary>
    /// Inventory dashboard statistics
    /// </summary>
    public class InventoryStatsViewModel
    {
        public int TotalProducts { get; set; }
        public int OutOfStock { get; set; }
        public int LowStock { get; set; }
        public int WellStocked { get; set; }
        public int CriticalItems { get; set; }
        public decimal TotalValue { get; set; }

        public decimal OutOfStockPercentage => TotalProducts > 0 ? (OutOfStock * 100.0m / TotalProducts) : 0;
        public decimal LowStockPercentage => TotalProducts > 0 ? (LowStock * 100.0m / TotalProducts) : 0;
        public decimal WellStockedPercentage => TotalProducts > 0 ? (WellStocked * 100.0m / TotalProducts) : 0;
    }

    /// <summary>
    /// Main inventory dashboard view model
    /// </summary>
    public class InventoryDashboardViewModel
    {
        public InventoryStatsViewModel Statistics { get; set; } = new();
        public List<ProductVariation> ProductVariations { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string StockFilter { get; set; } = "all";
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    /// <summary>
    /// Stock movements history view model
    /// </summary>
    public class StockMovementsViewModel
    {
        public List<StockMovement> Movements { get; set; } = new();
        public int? ProductVariationId { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 50;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }
}