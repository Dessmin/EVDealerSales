using EVDealerSales.BusinessObject.Enums;

namespace EVDealerSales.DataAccess.Entities
{
    public class Delivery : BaseEntity
    {
        public Guid OrderId { get; set; }
        public Order Order { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DateTime? ActualDate { get; set; }
        public DeliveryStatus Status { get; set; }
    }
}
