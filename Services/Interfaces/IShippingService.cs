using HamroGrocery.Models.Entities;

namespace HamroGrocery.Services.Interfaces
{
    public interface IShippingService
    {
        Task<string> CreateShipmentAsync(int orderId, string providerCode);
        Task<bool> UpdateTrackingAsync(string trackingNumber);
        Task<List<TrackingEvent>> GetTrackingEventsAsync(string trackingNumber);
        Task RegisterProviderAsync(IShippingProvider provider);
    }
}