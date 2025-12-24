// Models/ViewModels/CheckoutViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace HamroGrocery.Models.ViewModels
{
    /// <summary>
    /// ViewModel for checkout process with delivery options
    /// </summary>
    public class CheckoutViewModel
    {
        public Cart Cart { get; set; } = new Cart();

        [Required(ErrorMessage = "Please select a delivery method")]
        public DeliveryMethod DeliveryMethod { get; set; }

        // Delivery Address (only required if DeliveryMethod = Delivery)
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20)]
        public string Phone { get; set; } = string.Empty;

        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Street address is required")]
        [StringLength(200)]
        public string StreetAddress { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Suburb { get; set; }

        [Required(ErrorMessage = "City is required")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(50)]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postcode is required")]
        [StringLength(10)]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Invalid Australian postcode")]
        public string Postcode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? DeliveryInstructions { get; set; }

        // Pickup Details (only if DeliveryMethod = Pickup)
        public string? PickupStoreName { get; set; }
        public string? PickupStoreAddress { get; set; }
        public DateTime? EstimatedPickupTime { get; set; }

        // Payment Method
        [Required(ErrorMessage = "Please select a payment method")]
        public PaymentMethod PaymentMethod { get; set; }

        // Calculated Costs
        public decimal ShippingCost { get; set; }
        public decimal TotalWithShipping { get; set; }

        // Validation
        public bool IsDeliveryAddressRequired => DeliveryMethod == DeliveryMethod.Delivery;
    }

    public enum DeliveryMethod
    {
        Delivery = 1,
        Pickup = 2
    }

    public enum PaymentMethod
    {
        CreditCard = 1,
        PayPal = 2,
        Afterpay = 3,
        CashOnDelivery = 4  // Only available for Delivery method
    }

    /// <summary>
    /// DTO for AJAX shipping calculation
    /// </summary>
    public class ShippingCalculationRequest
    {
        [Required]
        public string Postcode { get; set; } = string.Empty;

        [Required]
        public string Suburb { get; set; } = string.Empty;

        public decimal CartTotal { get; set; }
    }

    /// <summary>
    /// Response for shipping calculation
    /// </summary>
    public class ShippingCalculationResponse
    {
        public bool Success { get; set; }
        public decimal StandardShipping { get; set; }
        public decimal ExpressShipping { get; set; }
        public int EstimatedDays { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsFreeShippingEligible { get; set; }
    }
}