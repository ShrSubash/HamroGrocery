using System;
using System.Collections.Generic;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for Inventory Report
    /// </summary>
    public class InventoryReportViewModel
    {
        public DateTime ReportDate { get; set; } = DateTime.Today;

        // Overall Statistics
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public decimal TotalStockValue { get; set; }

        // Stock Status Summary
        public int InStockProducts { get; set; }
        public int LowStockProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int OverstockProducts { get; set; }

        // Category-wise Inventory
        public List<CategoryInventoryDto> CategoryInventory { get; set; } = new();
        public ChartDataDto CategoryStockValueChart { get; set; } = new();

        // Low Stock Items (Need Reorder)
        public List<InventoryItemDto> LowStockItems { get; set; } = new();

        // Out of Stock Items
        public List<InventoryItemDto> OutOfStockItems { get; set; } = new();

        // Overstock Items (Above MaxStockLevel)
        public List<InventoryItemDto> OverstockItems { get; set; } = new();

        // Top Value Products
        public List<InventoryItemDto> TopValueProducts { get; set; } = new();

        // Stock Level Distribution Chart
        public ChartDataDto StockLevelChart { get; set; } = new();
    }

    /// <summary>
    /// DTO for Category Inventory
    /// </summary>
    public class CategoryInventoryDto
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int ProductCount { get; set; }
        public int TotalStock { get; set; }
        public decimal TotalValue { get; set; }
        public int LowStockCount { get; set; }
        public int OutOfStockCount { get; set; }
    }

    /// <summary>
    /// DTO for Individual Inventory Items
    /// </summary>
    public class InventoryItemDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int ReorderPoint { get; set; }
        public int MaxStockLevel { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalValue { get; set; }
        public string StockStatus { get; set; } = string.Empty;
        public string ImagePath { get; set; } = "/images/placeholder-product.jpg";
    }
}