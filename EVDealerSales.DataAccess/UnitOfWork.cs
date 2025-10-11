using EVDealerSales.DataAccess.Entities;
using EVDealerSales.DataAccess.Interfaces;

namespace EVDealerSales.DataAccess
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EVDealerSalesDbContext _dbContext;

        public UnitOfWork(EVDealerSalesDbContext dbContext,
            IGenericRepository<User> userRepository,
            IGenericRepository<Vehicle> vehicleRepository,
            IGenericRepository<Order> orderRepository,
            IGenericRepository<OrderItem> orderItemRepository,
            IGenericRepository<Invoice> invoiceRepository,
            IGenericRepository<Payment> paymentRepository,
            IGenericRepository<Delivery> deliveryRepository,
            IGenericRepository<TestDrive> testDriveRepository
            )
        {
            _dbContext = dbContext;
            Users = userRepository;
            Vehicles = vehicleRepository;
            Orders = orderRepository;
            OrderItems = orderItemRepository;
            Invoices = invoiceRepository;
            Payments = paymentRepository;
            Deliveries = deliveryRepository;
            TestDrives = testDriveRepository;
        }

        public IGenericRepository<User> Users { get; }
        public IGenericRepository<Vehicle> Vehicles { get; }
        public IGenericRepository<Order> Orders { get; }
        public IGenericRepository<OrderItem> OrderItems { get; }
        public IGenericRepository<Invoice> Invoices { get; }
        public IGenericRepository<Payment> Payments { get; }
        public IGenericRepository<Delivery> Deliveries { get; }
        public IGenericRepository<TestDrive> TestDrives { get; }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _dbContext.SaveChangesAsync();
        }
    }
}
