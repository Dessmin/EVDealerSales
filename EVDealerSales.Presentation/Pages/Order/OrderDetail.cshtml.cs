using EVDealerSales.Business.Interfaces;
using EVDealerSales.BusinessObject.DTOs.OrderDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace EVDealerSales.Presentation.Pages.Order
{
    [Authorize(Roles = "Customer,DealerStaff")]
    public class OrderDetailModel : PageModel
    {
        private readonly IOrderService _orderService;

        public OrderDetailModel(IOrderService orderService)
        {
            _orderService = orderService;
        }

        public OrderResponseDto Order { get; set; } = null!;
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            try
            {
                Order = await _orderService.GetOrderByIdAsync(id);
                
                if (Order == null)
                {
                    ErrorMessage = "Order not found";
                    return Page();
                }

                return Page();
            }
            catch (UnauthorizedAccessException ex)
            {
                ErrorMessage = ex.Message;
                return RedirectToPage("/Order/MyOrders");
            }
            catch (Exception ex)
            {
                ErrorMessage = "An error occurred while loading order details";
                return RedirectToPage("/Order/MyOrders");
            }
        }

        public async Task<IActionResult> OnPostCancelAsync(Guid id, [FromBody] CancelOrderRequest request)
        {
            try
            {
                await _orderService.CancelOrderAsync(id, request?.Reason);
                return new JsonResult(new { success = true, message = "Order cancelled successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 403 };
            }
            catch (Exception ex)
            {
                return new JsonResult(new { success = false, message = ex.Message }) { StatusCode = 400 };
            }
        }
    }

    public class CancelOrderRequest
    {
        public string? Reason { get; set; }
    }
}
