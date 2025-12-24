namespace HamroGrocery.Models.Entities
{
    public class TrackingEvent : BaseEntity
    {
        public int ShipmentId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Location { get; set; }
        public DateTime EventDate { get; set; }

        public Shipment Shipment { get; set; } = null!;
    }
}
