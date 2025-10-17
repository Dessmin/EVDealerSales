using EVDealerSales.Business.Interfaces;
using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.OrderDTOs;
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
        private readonly ILogger<ManageOrdersModel> _logger;

        public ManageOrdersModel(IOrderService orderService, ILogger<ManageOrdersModel> logger)
        {
            _orderService = orderService;
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

        public async Task<IActionResult> OnPostShipAsync(Guid id)
        {
            try
            {
                // TODO: Implement shipping logic later
                _logger.LogInformation("Shipping order {OrderId}", id);
                TempData["SuccessMessage"] = "Order marked for shipping! (Feature coming soon)";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error shipping order {OrderId}", id);
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToPage(new { PageNumber, PageSize, SearchTerm, Status, PaymentStatus, FromDate, ToDate });
        }
    }
}