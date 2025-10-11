namespace EVDealerSales.DataAccess.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; } // "Customer", "Staff", "Manager"

        // Navigation for relations
        public ICollection<TestDrive> TestDrivesAsCustomer { get; set; }
        public ICollection<TestDrive> TestDrivesAsStaff { get; set; }
        public ICollection<Order> OrdersAsCustomer { get; set; }
        public ICollection<Order> OrdersAsStaff { get; set; }
        public ICollection<Invoice> Invoices { get; set; }
        public ICollection<Feedback> FeedbacksGiven { get; set; }
        public ICollection<Feedback> CreatedFeedbacks { get; set; }
        public ICollection<Feedback> ResolvedFeedbacks { get; set; }
    }
}
