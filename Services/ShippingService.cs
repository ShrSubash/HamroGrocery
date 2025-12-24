// Services/ShippingService.cs (complete)
using HamroGrocery.Services.Interfaces;
using HamroGrocery.Data;
using HamroGrocery.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace HamroGrocery.Services
{
    public class ShippingService : IShippingService
    {
        private readonly ApplicationDbContext _context;
        private readonly Dictionary<string, IShippingProvider> _providers;

        public ShippingService(ApplicationDbContext context)
        {
            _context = context;
            _providers = new Dictionary<string, IShippingProvider>();
        }

        public async Task<string> CreateShipmentAsync(int orderId, string providerCode)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null) throw new ArgumentException("Order not found");

            var provider = _providers.GetValueOrDefault(providerCode);
            if (provider == null) throw new ArgumentException("Provider not supported");

            var trackingNumber = await provider.CreateShipmentAsync(order);

            // Save shipment record
            var shipment = new Shipment
            {
                OrderId = orderId,
                TrackingNumber = trackingNumber,
                Status = ShipmentStatus.Created
            };

            _context.Shipments.Add(shipment);
            await _context.SaveChangesAsync();

            return trackingNumber;
        }

        public async Task<bool> UpdateTrackingAsync(string trackingNumber)
        {
            var shipment = await _context.Shipments
                .Include(s => s.ShippingProvider)
                .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

            if (shipment == null) return false;

            var provider = _providers.GetValueOrDefault(shipment.ShippingProvider.ProviderCode);
            if (provider == null) return false;

            return await provider.UpdateTrackingAsync(trackingNumber);
        }

        public async Task<List<TrackingEvent>> GetTrackingEventsAsync(string trackingNumber)
        {
            var shipment = await _context.Shipments
                .Include(s => s.ShippingProvider)
                .FirstOrDefaultAsync(s => s.TrackingNumber == trackingNumber);

            if (shipment == null) return new List<TrackingEvent>();

            var provider = _providers.GetValueOrDefault(shipment.ShippingProvider.ProviderCode);
            if (provider == null)
            {
                // Return events from database if provider unavailable
                return await _context.TrackingEvents
                    .Where(te => te.ShipmentId == shipment.Id)
                    .OrderByDescending(te => te.EventDate)
                    .ToListAsync();
            }

            return await provider.GetTrackingEventsAsync(trackingNumber);
        }

        public async Task RegisterProviderAsync(IShippingProvider provider)
        {
            _providers[provider.ProviderCode] = provider;
            await Task.CompletedTask;
        }
    }
}