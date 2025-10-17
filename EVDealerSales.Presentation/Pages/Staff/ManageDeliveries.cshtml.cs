using EVDealerSales.Business.Interfaces;
using EVDealerSales.Business.Utils;
using EVDealerSales.BusinessObject.DTOs.DeliveryDTOs;
using EVDealerSales.BusinessObject.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EVDealerSales.Presentation.Pages.Staff
{
    [Authorize(Roles = "DealerStaff,DealerManager")]
    public class ManageDeliveriesModel : PageModel
    {
        private readonly IDeliveryService _deliveryService;
        private readonly ILogger<ManageDeliveriesModel> _logger;

        public ManageDeliveriesModel(
            IDeliveryService deliveryService,
            ILogger<ManageDeliveriesModel> logger)
        {
            _deliveryService = deliveryService;
            _logger = logger;
        }

        public Pagination<DeliveryResponseDto> Deliveries { get; set; } = null!;

        [BindProperty(SupportsGet = true)]
        public string? SearchTerm { get; set; }

        [BindProperty(SupportsGet = true)]
        public DeliveryStatus? Status { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 10;

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                var filter = new DeliveryFilterDto
                {
                    SearchTerm = SearchTerm,
                    Status = Status,
                    FromDate = FromDate,
                    ToDate = ToDate
                };

                Deliveries = await _deliveryService.GetAllDeliveriesAsync(PageNumber, PageSize, filter);

                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage("/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading deliveries");
                TempData["ErrorMessage"] = "An error occurred while loading deliveries";
                Deliveries = new Pagination<DeliveryResponseDto>(
                    new List<DeliveryResponseDto>(), 0, PageNumber, PageSize);
                return Page();
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(Guid id, DeliveryStatus status, DateTime? plannedDate, DateTime? actualDate)
        {
            try
            {
                var request = new UpdateDeliveryStatusRequestDto
                {
                    Status = status,
                    PlannedDate = plannedDate,
                    ActualDate = actualDate
                };

                await _deliveryService.UpdateDeliveryStatusAsync(id, request);

                TempData["SuccessMessage"] = $"Delivery status updated to {status}";
                return RedirectToPage();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating delivery status");
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToPage();
            }
        }
    }
}