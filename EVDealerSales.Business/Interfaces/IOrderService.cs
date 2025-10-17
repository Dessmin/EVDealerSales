using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.OrderDTOs;

namespace EVDealerSales.Business.Interfaces
{
    public interface IOrderService
    {
        // Customer operations
        Task<Guid> CreateOrderAsync(CreateOrderRequestDto request);
        Task<Pagination<OrderResponseDto>> GetMyOrdersAsync(int pageNumber = 1, int pageSize = 10);
        Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId);
        Task<bool> CancelOrderAsync(Guid orderId, string? reason = null);

        // Staff operations
        Task<Pagination<OrderResponseDto>> GetAllOrdersAsync(int pageNumber = 1, int pageSize = 10, OrderFilterDto? filter = null);
        Task<OrderResponseDto?> AssignStaffToOrderAsync(Guid orderId, Guid staffId);
        Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequestDto request);

        // Statistics
        Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null);
        Task<int> GetTotalOrdersCountAsync(DateTime? fromDate = null, DateTime? toDate = null);
    }
}
