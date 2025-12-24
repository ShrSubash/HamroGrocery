// Services/PaymentFactory.cs
namespace HamroGrocery.Services
{
    public class PaymentFactory
    {
        public static IPaymentService CreatePaymentService(IServiceProvider provider)
        {
            return provider.GetRequiredService<StripePaymentService>();
        }
    }
}