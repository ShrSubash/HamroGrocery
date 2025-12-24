namespace HamroGrocery.Models.Entities
{
    public class Shipment : BaseEntity
    {
        public int OrderId { get; set; }
        public int ShippingProviderId { get; set; }
        public string TrackingNumber { get; set; } = string.Empty;
        public string? ProviderTrackingId { get; set; }
        public ShipmentStatus Status { get; set; } = ShipmentStatus.Created;
        public DateTime? ShippedDate { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public DateTime? ActualDeliveryDate { get; set; }
        public decimal ShippingCost { get; set; }

        public Order Order { get; set; } = null!;
        public ShippingProvider ShippingProvider { get; set; } = null!;
        public ICollection<TrackingEvent> TrackingEvents { get; set; } = new List<TrackingEvent>();
    }

    public enum ShipmentStatus
    {
        Created = 1,
        LabelCreated = 2,
        Shipped = 3,
        InTransit = 4,
        OutForDelivery = 5,
        Delivered = 6,
        Exception = 7
    }
}