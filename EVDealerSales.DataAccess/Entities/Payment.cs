using EVDealerSales.BusinessObject.Enums;

namespace EVDealerSales.DataAccess.Entities
{
    public class Payment : BaseEntity
    {
        public Guid InvoiceId { get; set; }
        public Invoice Invoice { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public PaymentStatus Status { get; set; }
    }
}
