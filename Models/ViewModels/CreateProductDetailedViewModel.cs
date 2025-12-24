using DocumentFormat.OpenXml.Spreadsheet;
using HamroGrocery.Areas.Admin.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// Detailed ViewModel for creating and editing products with all features
    /// Used by Admin/Products/Create and Admin/Products/Edit
    /// </summary>
    public class CreateProductDetailedViewModel
    {
        // ========================================
        // BASIC INFORMATION
        // ========================================

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 200 characters")]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        // ========================================
        // PRICING
        // ========================================

        [Required(ErrorMessage = "Price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Price must be between Rs. 0.01 and Rs. 999,999.99")]
        [Display(Name = "Selling Price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Range(0, 999999.99, ErrorMessage = "Compare price must be between 0 and 999,999.99")]
        [Display(Name = "Compare at Price")]
        [DataType(DataType.Currency)]
        public decimal? ComparePrice { get; set; }

        // ========================================
        // INVENTORY MANAGEMENT
        // ========================================

        [Required(ErrorMessage = "Stock quantity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock quantity cannot be negative")]
        [Display(Name = "Initial Stock Quantity")]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Reorder point is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Reorder point cannot be negative")]
        [Display(Name = "Reorder Point")]
        public int ReorderPoint { get; set; } = 10;

        [Required(ErrorMessage = "Max stock level is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Max stock level must be at least 1")]
        [Display(Name = "Maximum Stock Level")]
        public int MaxStockLevel { get; set; } = 100;

        // ========================================
        // PHYSICAL PROPERTIES
        // ========================================

        [StringLength(50, ErrorMessage = "Unit cannot exceed 50 characters")]
        [Display(Name = "Unit of Measure")]
        public string? Unit { get; set; }

        [Range(0.001, 999999.99, ErrorMessage = "Weight must be between 0.001 and 999,999.99 kg")]
        [Display(Name = "Weight (kg)")]
        public decimal? Weight { get; set; }

        // ========================================
        // IDENTIFIERS
        // ========================================

        [StringLength(100, ErrorMessage = "Barcode cannot exceed 100 characters")]
        [Display(Name = "Barcode")]
        public string? Barcode { get; set; }

        [StringLength(100, ErrorMessage = "SKU cannot exceed 100 characters")]
        [Display(Name = "SKU (Stock Keeping Unit)")]
        public string? SKU { get; set; }

        // ========================================
        // PRODUCT SETTINGS & FLAGS
        // ========================================

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Featured")]
        public bool IsFeatured { get; set; } = false;

        [Display(Name = "Create Default Variation")]
        public bool CreateDefaultVariation { get; set; } = true;

        // ========================================
        // SEO & META DATA
        // ========================================

        [StringLength(100, ErrorMessage = "Meta title cannot exceed 100 characters")]
        [Display(Name = "Meta Title")]
        public string? MetaTitle { get; set; }

        [StringLength(500, ErrorMessage = "Meta description cannot exceed 500 characters")]
        [Display(Name = "Meta Description")]
        public string? MetaDescription { get; set; }

        // ========================================
        // VARIATION DEFAULTS
        // ========================================

        [StringLength(50, ErrorMessage = "Default size cannot exceed 50 characters")]
        [Display(Name = "Default Size")]
        public string? DefaultSize { get; set; }

        // ========================================
        // IMAGE HANDLING (For Edit Mode)
        // ========================================

        /// <summary>
        /// Stores the current image path when editing an existing product
        /// </summary>
        public string? CurrentImagePath { get; set; }

        // ========================================
        // CUSTOM VALIDATION
        // ========================================

        /// <summary>
        /// Validates that ComparePrice is greater than Price if provided
        /// </summary>
        public bool IsComparePriceValid()
        {
            if (ComparePrice.HasValue)
            {
                return ComparePrice.Value > Price;
            }
            return true;
        }

        /// <summary>
        /// Validates that ReorderPoint is less than MaxStockLevel
        /// </summary>
        public bool IsReorderPointValid()
        {
            return ReorderPoint < MaxStockLevel;
        }
    }
}
