using EVDealerSales.Business.Interfaces;
using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.OrderDTOs;
using EVDealerSales.BusinessObject.DTOs.DeliveryDTOs;
using EVDealerSales.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVDealerSales.Presentation.Pages.Order
{
    [Authorize(Policy = "StaffPolicy")]
    public class ManageOrdersModel : PageModel
    {
        private readonly IOrderService _orderService;
        private readonly IDeliveryService _deliveryService;  // ⭐ THÊM DELIVERY SERVICE
        private readonly ILogger<ManageOrdersModel> _logger;

        public ManageOrdersModel(
            IOrderService orderService,
            IDeliveryService deliveryService,  // ⭐ INJECT DELIVERY SERVICE
            ILogger<ManageOrdersModel> logger)
        {
            _orderService = orderService;
            _deliveryService = deliveryService;
            _logger = logger;
        }

        public Pagination<OrderResponseDto> Orders { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        // Search and Filter Properties
        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public OrderStatus? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public PaymentStatus? PaymentStatus { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation("Loading orders management page (Page: {PageNumber}, Size: {PageSize})",
                    PageNumber, PageSize);

                // Build filter DTO
                var filter = new OrderFilterDto
                {
                    SearchTerm = SearchTerm,
                    Status = Status,
                    PaymentStatus = PaymentStatus,
                    FromDate = FromDate,
                    ToDate = ToDate
                };

                Orders = await _orderService.GetAllOrdersAsync(
                    pageNumber: PageNumber,
                    pageSize: PageSize,
                    filter: filter
                );

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading orders list");
                TempData["ErrorMessage"] = "Failed to load orders list. Please try again.";
                return Page();
            }
        }

        // ⭐ CẬP NHẬT METHOD NÀY
        public async Task<IActionResult> OnPostShipAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("Creating delivery for order {OrderId}", id);

                // Kiểm tra xem delivery đã tồn tại chưa
                var existingDelivery = await _deliveryService.GetDeliveryByOrderIdAsync(id);

                if (existingDelivery != null)
                {
                    TempData["ErrorMessage"] = "Delivery already exists for this order!";
                    return RedirectToPage(new { PageNumber, PageSize, SearchTerm, Status, PaymentStatus, FromDate, ToDate });
                }

                // Tạo delivery mới
                var createDeliveryRequest = new CreateDeliveryRequestDto
                {
                    OrderId = id,
                    PlannedDate = DateTime.Now.AddDays(7),  // Dự kiến giao sau 7 ngày
                    Status = DeliveryStatus.Scheduled
                };

                await _deliveryService.CreateDeliveryAsync(createDeliveryRequest);

                TempData["SuccessMessage"] = "Delivery has been created successfully! Order is scheduled for delivery.";

                _logger.LogInformation("Delivery created successfully for order {OrderId}", id);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Cannot create delivery for order {OrderId}", id);
                TempData["ErrorMessage"] = ex.Message;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to create delivery for order {OrderId}", id);
                TempData["ErrorMessage"] = "You don't have permission to create deliveries.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating delivery for order {OrderId}", id);
                TempData["ErrorMessage"] = "Failed to create delivery. Please try again.";
            }

            return RedirectToPage(new { PageNumber, PageSize, SearchTerm, Status, PaymentStatus, FromDate, ToDate });
        }
    }
}