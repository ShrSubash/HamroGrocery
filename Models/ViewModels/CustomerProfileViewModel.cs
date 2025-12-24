using HamroGrocery.Models.Entities;

namespace HamroGrocery.Models.ViewModels
{
    public class CustomerProfileViewModel
    {
        public User User { get; set; }
        public int TotalOrders { get; set; }
        public List<Order> RecentOrders { get; set; } = new();
        public List<UserAddress> Addresses { get; set; } = new();
        public List<PaymentMethod> PaymentMethods { get; set; } = new();
    }
}
