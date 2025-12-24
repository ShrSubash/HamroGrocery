namespace HamroGrocery.Models.Entities
{
    public class StockMovement : BaseEntity
    {

        public int ProductVariationId { get; set; }
        public int Quantity { get; set; }
        public StockMovementType MovementType { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public decimal UnitCost { get; set; }
        public int PreviousQuantity { get; set; } // Add this
        public int NewQuantity { get; set; } // Add this
        public string Reason { get; set; } = string.Empty; // Add this
        public ProductVariation ProductVariation { get; set; } = null!;
    }

    public enum StockMovementType
    {
        Sale = 1,
        Purchase = 2,
        Adjustment = 3,
        Ullage = 4,
        Return = 5,
        StockIn = 6,    
        StockOut = 7    
    }
}