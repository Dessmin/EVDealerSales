using EVDealerSales.BusinessObject.DTOs.OrderDTOs;

namespace EVDealerSales.Business.Interfaces
{
    public interface IPaymentService
    {
        // Stripe Payment Integration
        Task<PaymentIntentResponseDto> CreatePaymentIntentAsync(Guid orderId);
        Task<string> CreateCheckoutSessionAsync(Guid orderId);
        Task<OrderResponseDto> ConfirmPaymentAsync(string paymentIntentId);
        
        // Payment information
        Task<PaymentIntentResponseDto?> GetPaymentIntentAsync(Guid orderId);
    }
}
