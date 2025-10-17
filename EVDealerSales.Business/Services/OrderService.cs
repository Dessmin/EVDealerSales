using EVDealerSales.Business.Interfaces;
using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.DeliveryDTOs;
using EVDealerSales.BusinessObject.DTOs.OrderDTOs;
using EVDealerSales.BusinessObject.Enums;
using EVDealerSales.DataAccess.Entities;
using EVDealerSales.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EVDealerSales.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderService> _logger;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;

        public OrderService(
            IUnitOfWork unitOfWork,
            ILogger<OrderService> logger,
            IClaimsService claimsService,
            ICurrentTime currentTime)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _claimsService = claimsService;
            _currentTime = currentTime;
        }

        public async Task<Guid> CreateOrderAsync(CreateOrderRequestDto request)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                _logger.LogInformation("User {UserId} creating order for vehicle {VehicleId}",
                    currentUserId, request.VehicleId);

                // Get customer
                var customer = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                if (customer == null || customer.IsDeleted)
                {
                    throw new KeyNotFoundException("Customer not found");
                }

                // Get vehicle
                var vehicle = await _unitOfWork.Vehicles.GetByIdAsync(request.VehicleId);
                if (vehicle == null || vehicle.IsDeleted)
                {
                    throw new KeyNotFoundException($"Vehicle with ID {request.VehicleId} not found");
                }

                if (!vehicle.IsActive)
                {
                    throw new InvalidOperationException("This vehicle is not available for purchase");
                }

                // Check stock availability
                if (vehicle.Stock <= 0)
                {
                    throw new InvalidOperationException("This vehicle is out of stock");
                }

                // Generate order number
                var orderNumber = await GenerateOrderNumberAsync();

                // Create order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = currentUserId,
                    OrderNumber = orderNumber,
                    Status = OrderStatus.Pending,
                    TotalAmount = vehicle.BasePrice,
                    Notes = request.Notes,
                    CreatedAt = _currentTime.GetCurrentTime(),
                    IsDeleted = false
                };

                await _unitOfWork.Orders.AddAsync(order);

                // Create order item
                var orderItem = new OrderItem
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    VehicleId = vehicle.Id,
                    UnitPrice = vehicle.BasePrice,
                    CreatedAt = _currentTime.GetCurrentTime(),
                    IsDeleted = false
                };

                await _unitOfWork.OrderItems.AddAsync(orderItem);

                // Create invoice
                var invoiceNumber = await GenerateInvoiceNumberAsync();
                var invoice = new Invoice
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    CustomerId = currentUserId,
                    InvoiceNumber = invoiceNumber,
                    TotalAmount = vehicle.BasePrice,
                    Status = InvoiceStatus.Pending,
                    Notes = "Awaiting payment",
                    CreatedAt = _currentTime.GetCurrentTime(),
                    IsDeleted = false
                };

                // Decrement stock for each vehicle in the order
                if (vehicle != null && !vehicle.IsDeleted)
                {
                    if (vehicle.Stock > 0)
                    {
                        vehicle.Stock -= 1;
                        vehicle.UpdatedAt = _currentTime.GetCurrentTime();
                        await _unitOfWork.Vehicles.Update(vehicle);
                        _logger.LogInformation("Decremented stock for vehicle {VehicleId} from {OldStock} to {NewStock}",
                            vehicle.Id, vehicle.Stock + 1, vehicle.Stock);
                    }
                    else
                    {
                        _logger.LogWarning("Vehicle {VehicleId} has no stock left but payment was already confirmed", vehicle.Id);
                    }
                }

                await _unitOfWork.Invoices.AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} created successfully with invoice {InvoiceId}",
                    order.Id, invoice.Id);

                return order.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order for vehicle {VehicleId}", request.VehicleId);
                throw;
            }
        }

        public async Task<Pagination<OrderResponseDto>> GetMyOrdersAsync(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                _logger.LogInformation("User {UserId} fetching their orders", currentUserId);

                // Validate pagination
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var query = _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Customer)
                    .Include(o => o.Staff)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .Include(o => o.Delivery)
                    .Where(o => o.CustomerId == currentUserId && !o.IsDeleted)
                    .OrderByDescending(o => o.CreatedAt);

                var totalCount = await query.CountAsync();

                var orders = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responseDtos = new List<OrderResponseDto>();
                foreach (var order in orders)
                {
                    responseDtos.Add(await MapToResponseDto(order));
                }

                return new Pagination<OrderResponseDto>(responseDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user's orders");
                throw;
            }
        }

        public async Task<OrderResponseDto?> GetOrderByIdAsync(Guid orderId)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Customer)
                    .Include(o => o.Staff)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .Include(o => o.Delivery)
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                {
                    return null;
                }

                // Check permission: customer can only view their own orders
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                bool isOwner = order.CustomerId == currentUserId;
                bool isStaff = currentUser?.Role == RoleType.DealerStaff || currentUser?.Role == RoleType.DealerManager;

                if (!isOwner && !isStaff)
                {
                    throw new UnauthorizedAccessException("You don't have permission to view this order");
                }

                return await MapToResponseDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<bool> CancelOrderAsync(Guid orderId, string? reason = null)
        {
            try
            {
                _logger.LogInformation("Cancelling order {OrderId}", orderId);

                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }

                // Only customer who ordered or staff can cancel
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                bool isOwner = order.CustomerId == currentUserId;
                bool isStaff = currentUser?.Role == RoleType.DealerStaff || currentUser?.Role == RoleType.DealerManager;

                if (!isOwner && !isStaff)
                {
                    throw new UnauthorizedAccessException("You don't have permission to cancel this order");
                }

                if (order.Status == OrderStatus.Cancelled)
                {
                    throw new InvalidOperationException("Order is already cancelled");
                }

                if (order.Status == OrderStatus.Delivered)
                {
                    throw new InvalidOperationException("Cannot cancel a delivered order");
                }

                // Check if order has been paid
                var hasPaidPayment = order.Invoices
                    .SelectMany(i => i.Payments)
                    .Any(p => p.Status == PaymentStatus.Paid);

                if (hasPaidPayment)
                {
                    throw new InvalidOperationException("Cannot cancel a paid order. Please contact staff for assistance.");
                }

                order.Status = OrderStatus.Cancelled;
                order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                    ? $"Cancelled: {reason}"
                    : $"{order.Notes}\nCancelled: {reason}";
                order.UpdatedAt = _currentTime.GetCurrentTime();
                order.UpdatedBy = currentUserId;

                _logger.LogInformation("OrderItemCount", order.Items.Count);


                // Restore stock for each vehicle in the order
                foreach (var orderItem in order.Items)
                {
                    var vehicle = orderItem.Vehicle ?? await _unitOfWork.Vehicles.GetByIdAsync(orderItem.VehicleId);
                    if (vehicle != null && !vehicle.IsDeleted)
                    {
                        vehicle.Stock += 1;
                        vehicle.UpdatedAt = _currentTime.GetCurrentTime();
                        await _unitOfWork.Vehicles.Update(vehicle);
                        _logger.LogInformation("Restored stock for vehicle {VehicleId} from {OldStock} to {NewStock}",
                            vehicle.Id, vehicle.Stock - 1, vehicle.Stock);
                    }
                    else
                    {
                        throw new InvalidOperationException($"Vehicle with ID {orderItem.VehicleId} not found for stock restoration");
                    }
                }

                await _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} cancelled by user {UserId}", orderId, currentUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Pagination<OrderResponseDto>> GetAllOrdersAsync(
            int pageNumber = 1,
            int pageSize = 10,
            OrderFilterDto? filter = null)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                // Check if user is staff
                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                if (currentUser == null || (currentUser.Role != RoleType.DealerStaff && currentUser.Role != RoleType.DealerManager))
                {
                    throw new UnauthorizedAccessException("Only staff can view all orders");
                }

                _logger.LogInformation("Staff {UserId} fetching all orders", currentUserId);

                // Validate pagination
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var query = _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Customer)
                    .Include(o => o.Staff)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .Include(o => o.Delivery)
                    .Where(o => !o.IsDeleted);

                // Apply filters
                if (filter != null)
                {
                    if (filter.CustomerId.HasValue)
                    {
                        query = query.Where(o => o.CustomerId == filter.CustomerId);
                    }

                    if (filter.StaffId.HasValue)
                    {
                        query = query.Where(o => o.StaffId == filter.StaffId);
                    }

                    if (filter.Status.HasValue)
                    {
                        query = query.Where(o => o.Status == filter.Status);
                    }

                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(o => o.CreatedAt >= filter.FromDate);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        query = query.Where(o => o.CreatedAt <= filter.ToDate);
                    }

                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    {
                        var searchTerm = filter.SearchTerm.ToLower();
                        query = query.Where(o =>
                            o.OrderNumber.ToLower().Contains(searchTerm) ||
                            (o.Customer != null && o.Customer.FullName.ToLower().Contains(searchTerm)) ||
                            (o.Customer != null && o.Customer.Email.ToLower().Contains(searchTerm)));
                    }
                }

                query = query.OrderByDescending(o => o.CreatedAt);

                var totalCount = await query.CountAsync();

                var orders = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responseDtos = new List<OrderResponseDto>();
                foreach (var order in orders)
                {
                    responseDtos.Add(await MapToResponseDto(order));
                }

                return new Pagination<OrderResponseDto>(responseDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all orders");
                throw;
            }
        }

        public async Task<OrderResponseDto?> AssignStaffToOrderAsync(Guid orderId, Guid staffId)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                if (currentUser == null || (currentUser.Role != RoleType.DealerStaff && currentUser.Role != RoleType.DealerManager))
                {
                    throw new UnauthorizedAccessException("Only staff can assign orders");
                }

                var order = await _unitOfWork.Orders.GetByIdAsync(orderId);
                if (order == null || order.IsDeleted)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }

                var staff = await _unitOfWork.Users.GetByIdAsync(staffId);
                if (staff == null || staff.IsDeleted || (staff.Role != RoleType.DealerStaff && staff.Role != RoleType.DealerManager))
                {
                    throw new KeyNotFoundException("Staff not found or invalid role");
                }

                order.StaffId = staffId;
                order.UpdatedAt = _currentTime.GetCurrentTime();
                order.UpdatedBy = currentUserId;

                await _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} assigned to staff {StaffId}", orderId, staffId);

                return await MapToResponseDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning staff to order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<OrderResponseDto?> UpdateOrderStatusAsync(Guid orderId, UpdateOrderStatusRequestDto request)
        {
            try
            {
                var currentUserId = _claimsService.GetCurrentUserId;
                if (currentUserId == Guid.Empty)
                {
                    throw new UnauthorizedAccessException("User not authenticated");
                }

                var currentUser = await _unitOfWork.Users.GetByIdAsync(currentUserId);
                if (currentUser == null || (currentUser.Role != RoleType.DealerStaff && currentUser.Role != RoleType.DealerManager))
                {
                    throw new UnauthorizedAccessException("Only staff can update order status");
                }

                var order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .FirstOrDefaultAsync(o => o.Id == orderId && !o.IsDeleted);

                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {orderId} not found");
                }

                // Validate status transition
                if (request.Status == OrderStatus.Confirmed)
                {
                    var hasPaidPayment = order.Invoices
                        .SelectMany(i => i.Payments)
                        .Any(p => p.Status == PaymentStatus.Paid);

                    if (!hasPaidPayment)
                    {
                        throw new InvalidOperationException("Cannot confirm order without payment");
                    }
                }

                order.Status = request.Status;
                if (!string.IsNullOrWhiteSpace(request.Notes))
                {
                    order.Notes = string.IsNullOrWhiteSpace(order.Notes)
                        ? request.Notes
                        : $"{order.Notes}\n{request.Notes}";
                }
                order.UpdatedAt = _currentTime.GetCurrentTime();
                order.UpdatedBy = currentUserId;

                await _unitOfWork.Orders.Update(order);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, request.Status);

                return await MapToResponseDto(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId}", orderId);
                throw;
            }
        }

        public async Task<decimal> GetTotalRevenueAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _unitOfWork.Orders.GetQueryable()
                    .Where(o => !o.IsDeleted && o.Status == OrderStatus.Confirmed || o.Status == OrderStatus.Delivered);

                if (fromDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= fromDate);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= toDate);
                }

                return await query.SumAsync(o => o.TotalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating total revenue");
                throw;
            }
        }

        public async Task<int> GetTotalOrdersCountAsync(DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _unitOfWork.Orders.GetQueryable()
                    .Where(o => !o.IsDeleted);

                if (fromDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt >= fromDate);
                }

                if (toDate.HasValue)
                {
                    query = query.Where(o => o.CreatedAt <= toDate);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting total orders");
                throw;
            }
        }

        private async Task<string> GenerateOrderNumberAsync()
        {
            var date = _currentTime.GetCurrentTime();
            var dateStr = date.ToString("yyyyMMdd");

            var todayOrderCount = await _unitOfWork.Orders.GetQueryable()
                .Where(o => o.OrderNumber.StartsWith($"ORD-{dateStr}"))
                .CountAsync();

            return $"ORD-{dateStr}-{(todayOrderCount + 1):D4}";
        }

        private async Task<string> GenerateInvoiceNumberAsync()
        {
            var date = _currentTime.GetCurrentTime();
            var dateStr = date.ToString("yyyyMMdd");

            var todayInvoiceCount = await _unitOfWork.Invoices.GetQueryable()
                .Where(i => i.InvoiceNumber.StartsWith($"INV-{dateStr}"))
                .CountAsync();

            return $"INV-{dateStr}-{(todayInvoiceCount + 1):D4}";
        }

        private async Task<OrderResponseDto> MapToResponseDto(Order order)
        {
            // Ensure navigation properties are loaded
            if (order.Customer == null)
            {
                order.Customer = await _unitOfWork.Users.GetByIdAsync(order.CustomerId)
                    ?? throw new InvalidOperationException("Customer not found");
            }

            User? staff = null;
            if (order.StaffId.HasValue)
            {
                staff = order.Staff ?? await _unitOfWork.Users.GetByIdAsync(order.StaffId.Value);
            }

            // Load items if not loaded
            if (order.Items == null || !order.Items.Any())
            {
                var items = await _unitOfWork.OrderItems.GetQueryable()
                    .Include(oi => oi.Vehicle)
                    .Where(oi => oi.OrderId == order.Id && !oi.IsDeleted)
                    .ToListAsync();
                order.Items = items;
            }

            // Load invoices if not loaded
            if (order.Invoices == null || !order.Invoices.Any())
            {
                var invoices = await _unitOfWork.Invoices.GetQueryable()
                    .Include(i => i.Payments)
                    .Where(i => i.OrderId == order.Id && !i.IsDeleted)
                    .ToListAsync();
                order.Invoices = invoices;
            }

            // Load delivery if exists
            if (order.Delivery == null)
            {
                var deliveryEntity = await _unitOfWork.Deliveries.GetQueryable()
                    .FirstOrDefaultAsync(d => d.OrderId == order.Id && !d.IsDeleted);
                order.Delivery = deliveryEntity;
            }

            var invoice = order.Invoices.FirstOrDefault();
            var payment = invoice?.Payments.FirstOrDefault();
            var delivery = order.Delivery;

            // Map invoice to DTO
            InvoiceResponseDto? invoiceDto = null;
            if (invoice != null)
            {
                invoiceDto = new InvoiceResponseDto
                {
                    Id = invoice.Id,
                    OrderId = invoice.OrderId,
                    InvoiceNumber = invoice.InvoiceNumber,
                    IssueDate = invoice.CreatedAt,
                    DueDate = null, // Set if you have a due date field
                    TotalAmount = invoice.TotalAmount,
                    Status = invoice.Status,
                    Notes = invoice.Notes,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt
                };
            }

            // Map payment to DTO
            PaymentResponseDto? paymentDto = null;
            if (payment != null)
            {
                paymentDto = new PaymentResponseDto
                {
                    Id = payment.Id,
                    InvoiceId = payment.InvoiceId,
                    Amount = payment.Amount,
                    Status = payment.Status,
                    PaymentDate = payment.PaymentDate,
                    PaymentMethod = payment.PaymentMethod,
                    PaymentIntentId = payment.PaymentIntentId,
                    TransactionId = payment.PaymentIntentId, // Using PaymentIntentId as TransactionId
                    CreatedAt = payment.CreatedAt
                };
            }

            // Map delivery to DTO
            DeliveryResponseDto? deliveryDto = null;
            if (delivery != null)
            {
                // Get vehicle info
                var vehicleInfo = order.Items != null && order.Items.Any()
                    ? string.Join(", ", order.Items.Select(oi => $"{oi.Vehicle?.ModelName} {oi.Vehicle?.TrimName}"))
                    : "N/A";

                deliveryDto = new DeliveryResponseDto
                {
                    Id = delivery.Id,
                    OrderId = delivery.OrderId,
                    OrderNumber = order.OrderNumber,
                    CustomerId = order.CustomerId,
                    CustomerName = order.Customer.FullName,
                    CustomerEmail = order.Customer.Email,
                    CustomerPhone = order.Customer.PhoneNumber,
                    PlannedDate = delivery.PlannedDate,
                    ActualDate = delivery.ActualDate,
                    Status = delivery.Status,
                    VehicleInfo = vehicleInfo,
                    ShippingAddress = delivery.ShippingAddress,
                    Notes = delivery.Notes,
                    StaffNotes = delivery.StaffNotes,
                    CreatedAt = delivery.CreatedAt,
                    UpdatedAt = delivery.UpdatedAt
                };
            }

            return new OrderResponseDto
            {
                Id = order.Id,
                OrderNumber = order.OrderNumber,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                CustomerPhone = order.Customer.PhoneNumber,
                StaffId = order.StaffId,
                StaffName = staff?.FullName,
                StaffEmail = staff?.Email,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Items = order.Items?.Select(oi => new OrderItemDto
                {
                    Id = oi.Id,
                    VehicleId = oi.VehicleId,
                    VehicleModelName = oi.Vehicle?.ModelName ?? "Unknown",
                    VehicleTrimName = oi.Vehicle?.TrimName ?? "Unknown",
                    VehicleImageUrl = oi.Vehicle?.ImageUrl,
                    UnitPrice = oi.UnitPrice,
                    Year = oi.Vehicle?.ModelYear ?? 0,
                }).ToList() ?? new List<OrderItemDto>(),
                // Flattened properties for backward compatibility
                PaymentStatus = payment?.Status,
                PaymentDate = payment?.PaymentDate,
                PaymentIntentId = payment?.PaymentIntentId,
                DeliveryId = delivery?.Id,
                DeliveryStatus = delivery?.Status,
                DeliveryDate = delivery?.ActualDate,
                // Nested objects
                Invoice = invoiceDto,
                Payment = paymentDto,
                Delivery = deliveryDto,
                Notes = order.Notes,
                CreatedAt = order.CreatedAt,
                UpdatedAt = order.UpdatedAt
            };
        }
    }
}
