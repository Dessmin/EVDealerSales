using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.DeliveryDTOs;

namespace EVDealerSales.Business.Interfaces
{
    public interface IDeliveryService
    {
        Task<DeliveryResponseDto> CreateDeliveryAsync(CreateDeliveryRequestDto request);
        Task<DeliveryResponseDto?> GetDeliveryByIdAsync(Guid id);
        Task<DeliveryResponseDto?> GetDeliveryByOrderIdAsync(Guid orderId);
        Task<Pagination<DeliveryResponseDto>> GetAllDeliveriesAsync(
            int pageNumber = 1,
            int pageSize = 10,
            DeliveryFilterDto? filter = null);
        Task<DeliveryResponseDto?> UpdateDeliveryStatusAsync(Guid id, UpdateDeliveryStatusRequestDto request);
    }
}
