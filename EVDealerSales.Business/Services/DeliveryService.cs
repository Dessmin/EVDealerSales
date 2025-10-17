using EVDealerSales.Business.Interfaces;
using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.DeliveryDTOs;
using EVDealerSales.BusinessObject.Enums;
using EVDealerSales.DataAccess.Entities;
using EVDealerSales.DataAccess.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EVDealerSales.Business.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeliveryService> _logger;
        private readonly IClaimsService _claimsService;
        private readonly ICurrentTime _currentTime;

        public DeliveryService(
            IUnitOfWork unitOfWork,
            ILogger<DeliveryService> logger,
            IClaimsService claimsService,
            ICurrentTime currentTime)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _claimsService = claimsService;
            _currentTime = currentTime;
        }

        public async Task<DeliveryResponseDto> CreateDeliveryAsync(CreateDeliveryRequestDto request)
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
                    throw new UnauthorizedAccessException("Only staff can create deliveries");
                }

                _logger.LogInformation("Staff {StaffId} creating delivery for order {OrderId}",
                    currentUserId, request.OrderId);

                // Validate order exists
                var order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Customer)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .Include(o => o.Invoices).ThenInclude(i => i.Payments)
                    .FirstOrDefaultAsync(o => o.Id == request.OrderId && !o.IsDeleted);

                if (order == null)
                {
                    throw new KeyNotFoundException($"Order with ID {request.OrderId} not found");
                }

                // Check if order is paid
                var hasPaidPayment = order.Invoices
                    .SelectMany(i => i.Payments)
                    .Any(p => p.Status == PaymentStatus.Paid);

                if (!hasPaidPayment)
                {
                    throw new InvalidOperationException("Cannot create delivery for unpaid order");
                }

                // Check if delivery already exists
                var existingDelivery = await _unitOfWork.Deliveries.GetQueryable()
                    .FirstOrDefaultAsync(d => d.OrderId == request.OrderId && !d.IsDeleted);

                if (existingDelivery != null)
                {
                    throw new InvalidOperationException("Delivery already exists for this order");
                }

                // Create delivery
                var delivery = new Delivery
                {
                    Id = Guid.NewGuid(),
                    OrderId = request.OrderId,
                    PlannedDate = request.PlannedDate,
                    Status = request.Status,
                    CreatedAt = _currentTime.GetCurrentTime(),
                    IsDeleted = false
                };

                await _unitOfWork.Deliveries.AddAsync(delivery);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Delivery {DeliveryId} created for order {OrderId}",
                    delivery.Id, request.OrderId);

                return await MapToResponseDto(delivery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delivery for order {OrderId}", request.OrderId);
                throw;
            }
        }

        public async Task<DeliveryResponseDto?> GetDeliveryByIdAsync(Guid id)
        {
            try
            {
                var delivery = await _unitOfWork.Deliveries.GetQueryable()
                    .Include(d => d.Order).ThenInclude(o => o.Customer)
                    .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (delivery == null)
                {
                    return null;
                }

                return await MapToResponseDto(delivery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery {DeliveryId}", id);
                throw;
            }
        }

        public async Task<DeliveryResponseDto?> GetDeliveryByOrderIdAsync(Guid orderId)
        {
            try
            {
                var delivery = await _unitOfWork.Deliveries.GetQueryable()
                    .Include(d => d.Order).ThenInclude(o => o.Customer)
                    .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .FirstOrDefaultAsync(d => d.OrderId == orderId && !d.IsDeleted);

                if (delivery == null)
                {
                    return null;
                }

                return await MapToResponseDto(delivery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching delivery for order {OrderId}", orderId);
                throw;
            }
        }

        public async Task<Pagination<DeliveryResponseDto>> GetAllDeliveriesAsync(
            int pageNumber = 1,
            int pageSize = 10,
            DeliveryFilterDto? filter = null)
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
                    throw new UnauthorizedAccessException("Only staff can view all deliveries");
                }

                _logger.LogInformation("Staff {StaffId} fetching deliveries (Page: {PageNumber}, PageSize: {PageSize})",
                    currentUserId, pageNumber, pageSize);

                // Validate pagination
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 10;

                var query = _unitOfWork.Deliveries.GetQueryable()
                    .Include(d => d.Order).ThenInclude(o => o.Customer)
                    .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .Where(d => !d.IsDeleted);

                // Apply filters
                if (filter != null)
                {
                    if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                    {
                        var searchTerm = filter.SearchTerm.ToLower();
                        query = query.Where(d =>
                            d.Order.OrderNumber.ToLower().Contains(searchTerm) ||
                            d.Order.Customer.FullName.ToLower().Contains(searchTerm) ||
                            d.Order.Customer.Email.ToLower().Contains(searchTerm));
                    }

                    if (filter.Status.HasValue)
                    {
                        query = query.Where(d => d.Status == filter.Status.Value);
                    }

                    if (filter.FromDate.HasValue)
                    {
                        query = query.Where(d => d.PlannedDate >= filter.FromDate.Value || d.ActualDate >= filter.FromDate.Value);
                    }

                    if (filter.ToDate.HasValue)
                    {
                        query = query.Where(d => d.PlannedDate <= filter.ToDate.Value || d.ActualDate <= filter.ToDate.Value);
                    }
                }

                // Order by creation date descending
                query = query.OrderByDescending(d => d.CreatedAt);

                var totalCount = await query.CountAsync();

                var deliveries = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var responseDtos = new List<DeliveryResponseDto>();
                foreach (var delivery in deliveries)
                {
                    responseDtos.Add(await MapToResponseDto(delivery));
                }

                return new Pagination<DeliveryResponseDto>(responseDtos, totalCount, pageNumber, pageSize);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching deliveries");
                throw;
            }
        }

        public async Task<DeliveryResponseDto?> UpdateDeliveryStatusAsync(Guid id, UpdateDeliveryStatusRequestDto request)
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
                    throw new UnauthorizedAccessException("Only staff can update delivery status");
                }

                var delivery = await _unitOfWork.Deliveries.GetQueryable()
                    .Include(d => d.Order).ThenInclude(o => o.Customer)
                    .Include(d => d.Order).ThenInclude(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted);

                if (delivery == null)
                {
                    throw new KeyNotFoundException($"Delivery with ID {id} not found");
                }

                _logger.LogInformation("Staff {StaffId} updating delivery {DeliveryId} status to {Status}",
                    currentUserId, id, request.Status);

                // Update status
                delivery.Status = request.Status;

                // Update planned date if provided
                if (request.PlannedDate.HasValue)
                {
                    delivery.PlannedDate = request.PlannedDate.Value;
                }

                // If status is Delivered, set actual date
                if (request.Status == DeliveryStatus.Delivered)
                {
                    delivery.ActualDate = request.ActualDate ?? _currentTime.GetCurrentTime();

                    // Update order status to Delivered
                    var order = delivery.Order;
                    if (order.Status != OrderStatus.Delivered)
                    {
                        order.Status = OrderStatus.Delivered;
                        order.UpdatedAt = _currentTime.GetCurrentTime();
                        order.UpdatedBy = currentUserId;
                        await _unitOfWork.Orders.Update(order);
                    }
                }

                delivery.UpdatedAt = _currentTime.GetCurrentTime();
                delivery.UpdatedBy = currentUserId;

                await _unitOfWork.Deliveries.Update(delivery);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("Delivery {DeliveryId} status updated to {Status}",
                    id, request.Status);

                return await MapToResponseDto(delivery);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating delivery {DeliveryId}", id);
                throw;
            }
        }

        private async Task<DeliveryResponseDto> MapToResponseDto(Delivery delivery)
        {
            // Ensure navigation properties are loaded
            if (delivery.Order == null)
            {
                delivery.Order = await _unitOfWork.Orders.GetQueryable()
                    .Include(o => o.Customer)
                    .Include(o => o.Items).ThenInclude(oi => oi.Vehicle)
                    .FirstOrDefaultAsync(o => o.Id == delivery.OrderId)
                    ?? throw new InvalidOperationException("Order not found");
            }

            var vehicleInfo = delivery.Order.Items != null && delivery.Order.Items.Any()
                ? string.Join(", ", delivery.Order.Items.Select(oi => $"{oi.Vehicle?.ModelName} {oi.Vehicle?.TrimName}"))
                : "N/A";

            return new DeliveryResponseDto
            {
                Id = delivery.Id,
                OrderId = delivery.OrderId,
                OrderNumber = delivery.Order.OrderNumber,
                CustomerId = delivery.Order.CustomerId,
                CustomerName = delivery.Order.Customer?.FullName ?? "Unknown",
                CustomerEmail = delivery.Order.Customer?.Email ?? "Unknown",
                CustomerPhone = delivery.Order.Customer?.PhoneNumber,
                PlannedDate = delivery.PlannedDate,
                ActualDate = delivery.ActualDate,
                Status = delivery.Status,
                VehicleInfo = vehicleInfo,
                ShippingAddress = delivery.Order.ShippingAddress,
                CreatedAt = delivery.CreatedAt,
                UpdatedAt = delivery.UpdatedAt
            };
        }
    }
}