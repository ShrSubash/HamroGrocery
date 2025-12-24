// Models/Entities/UserAddress.cs
namespace HamroGrocery.Models.Entities
{
    public class UserAddress
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        public string Label { get; set; } = "Home"; // Home, Work, Parents, etc.
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string AddressLineOne { get; set; } = string.Empty;
        public string? AddressLineTwo { get; set; }
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "Australia";
        public bool IsDefault { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User User { get; set; } = null!;
    }
}