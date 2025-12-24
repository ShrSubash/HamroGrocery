// Models/Cart.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HamroGrocery.Models
{
    /// <summary>
    /// Shopping cart supporting both authenticated users and guest checkout
    /// </summary>
    /// 
    [Table("Carts")]
    public class Cart
    {
        [Key]                            // ← ADD
        public int Id { get; set; }      // ← ADD

        public int? UserId { get; set; }

        public Cart()
        {
            Items = new List<CartItem>();
            CreatedAt = DateTime.UtcNow;
            IsGuestCart = false;
            TaxRate = 0.10m; // 10% GST for Australia
        }

        // Cart Items
        public List<CartItem> Items { get; set; }

        // Cart Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Guest Cart Properties
        public bool IsGuestCart { get; set; }
        public string? GuestSessionId { get; set; }
        public string? GuestInfoJson { get; set; }

        // Tax Configuration
        public decimal TaxRate { get; set; }

        // Calculated Properties
        public decimal SubTotal => Items.Sum(item => item.TotalPrice);
        public decimal TaxAmount => SubTotal * TaxRate;
        public decimal CartTotal => SubTotal + TaxAmount;
        public int TotalItems => Items.Sum(item => item.Quantity);
        public bool IsEmpty => !Items.Any();

        // Cart Operations
        public CartItem? FindItem(int productId, int? variationId = null)
        {
            return Items.FirstOrDefault(item =>
                item.ProductId == productId &&
                item.ProductVariationId == variationId);
        }

        public void AddItem(CartItem newItem)
        {
            var existingItem = FindItem(newItem.ProductId, newItem.ProductVariationId);

            if (existingItem != null)
            {
                existingItem.Quantity += newItem.Quantity;
                existingItem.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                Items.Add(newItem);
            }

            Touch();
        }

        public bool UpdateItemQuantity(int productId, int quantity, int? variationId = null)
        {
            var item = FindItem(productId, variationId);
            if (item == null) return false;

            if (quantity <= 0)
            {
                Items.Remove(item);
            }
            else if (quantity <= item.AvailableStock)
            {
                item.Quantity = quantity;
                item.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                return false; // Exceeds available stock
            }

            Touch();
            return true;
        }

        public bool RemoveItem(int productId, int? variationId = null)
        {
            var item = FindItem(productId, variationId);
            if (item == null) return false;

            Items.Remove(item);
            Touch();
            return true;
        }

        public void Clear()
        {
            Items.Clear();
            GuestInfoJson = null;
            Touch();
        }

        public void Touch()
        {
            UpdatedAt = DateTime.UtcNow;
        }

        public ValidationResult Validate()
        {
            var result = new ValidationResult { IsValid = true };

            if (IsEmpty)
            {
                result.IsValid = false;
                result.Errors.Add("Cart is empty");
                return result;
            }

            foreach (var item in Items)
            {
                if (!item.IsInStock)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{item.ProductName} is out of stock");
                }
                else if (item.Quantity > item.AvailableStock)
                {
                    result.IsValid = false;
                    result.Errors.Add($"{item.ProductName}: Only {item.AvailableStock} available (requested {item.Quantity})");
                }
            }

            return result;
        }
    }

    /// <summary>
    /// Individual item in shopping cart
    /// </summary>
    /// [Table("CartItems")]
    /// 
    [Table("CartItems")]
    public class CartItem
    {
        [Key]                            // ← ADD
        public int Id { get; set; }      // ← ADD

        public int CartId { get; set; }  // ← ADD (foreign key)
        public Cart Cart { get; set; } = null!;
        public int ProductId { get; set; }
        public int? ProductVariationId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? VariationName { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string? ImagePath { get; set; }
        public int AvailableStock { get; set; }
        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Additional properties for view compatibility
        public string? SKU { get; set; }
        public string? CategoryName { get; set; }

        // Property aliases for backward compatibility
        public decimal Price => UnitPrice;

        // Calculated Properties
        public decimal TotalPrice => UnitPrice * Quantity;
        public bool IsInStock => AvailableStock >= Quantity;
        public bool IsLowStock => AvailableStock > 0 && AvailableStock < 10;
    }

    /// <summary>
    /// Guest checkout information collected during anonymous checkout
    /// </summary>
    public class GuestCheckoutInfo
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [RegularExpression(@"^(\+61|0)[2-478](?:[ -]?[0-9]){8}$", ErrorMessage = "Invalid Australian phone number")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shipping address is required")]
        [StringLength(200, ErrorMessage = "Address cannot exceed 200 characters")]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        public string ShippingCity { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postcode is required")]
        [RegularExpression(@"^[0-9]{4}$", ErrorMessage = "Invalid Australian postcode (must be 4 digits)")]
        public string ShippingPostcode { get; set; } = string.Empty;

        public string? ShippingState { get; set; } = "NSW";
        public string? DeliveryInstructions { get; set; }
        public bool AcceptTerms { get; set; }
    }

    /// <summary>
    /// Cart validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}