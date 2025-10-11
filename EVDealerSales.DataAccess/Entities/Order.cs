using EVDealerSales.BusinessObject.Enums;

namespace EVDealerSales.DataAccess.Entities
{
    public class Order : BaseEntity
    {
        // Customer who made the order
        public Guid CustomerId { get; set; }
        public User Customer { get; set; }

        // Dealer staff who processes it
        public Guid? StaffId { get; set; }
        public User Staff { get; set; }

        public OrderStatus Status { get; set; }
        public decimal TotalAmount { get; set; }

        // Navigation
        public ICollection<OrderItem> Items { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
        public Delivery Delivery { get; set; }
        public ICollection<Feedback> Feedbacks { get; set; }
    }
}
