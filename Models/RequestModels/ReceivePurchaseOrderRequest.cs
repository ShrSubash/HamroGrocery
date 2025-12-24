namespace HamroGrocery.Models.RequestModels
{
    public class ReceivePurchaseOrderRequest
    {
        public List<ReceivedItemModel> ReceivedItems { get; set; } = new();
    }
}
