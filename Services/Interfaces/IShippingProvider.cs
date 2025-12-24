using HamroGrocery.Models.Entities;

namespace HamroGrocery.Services.Interfaces
{
    public interface IShippingProvider
    {
        Task<string> CreateShipmentAsync(Order order);
        Task<bool> UpdateTrackingAsync(string trackingNumber);
        Task<List<TrackingEvent>> GetTrackingEventsAsync(string trackingNumber);
        string ProviderCode { get; }
    }
}