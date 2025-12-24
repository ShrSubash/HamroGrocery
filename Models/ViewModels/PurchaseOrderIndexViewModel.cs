using HamroGrocery.Models.Entities;
namespace HamroGrocery.Models.ViewModels
{
    public class PurchaseOrderIndexViewModel
    {
        public List<PurchaseOrder> PurchaseOrders { get; set; } = new();
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public decimal TotalValue { get; set; }
    }
}
