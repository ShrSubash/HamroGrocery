namespace HamroGrocery.Models.Entities
{
    public class ShippingProvider : BaseEntity
    {
        public string ProviderName { get; set; } = string.Empty;
        public string ProviderCode { get; set; } = string.Empty; // FEDEX, UPS, etc.
        public string ApiEndpoint { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        // Remove this line - IsActive is already inherited from BaseEntity
        // public bool IsActive { get; set; } = true;
        public decimal BaseRate { get; set; }
        public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    }
}