using EVDealerSales.BusinessObject.Enums;

namespace EVDealerSales.BusinessObject.DTOs.DeliveryDTOs
{
    public class CreateDeliveryRequestDto
    {
        public Guid OrderId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DeliveryStatus Status { get; set; } = DeliveryStatus.Scheduled;
    }
}