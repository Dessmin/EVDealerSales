using EVDealerSales.Business.Interfaces;
using EVDealerSales.BusinessObject.DTOs.AuthDTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace EVDealerSales.Presentation.Pages.Auth
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly IAuthService _authService;

        public RegisterModel(IAuthService authService)
        {
            _authService = authService;
        }

        [BindProperty]
        [Required(ErrorMessage = "Full name is required")]
        public string FullName { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Phone number is required")]
        [Phone(ErrorMessage = "Invalid phone number format")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }

        public void OnGet()
        {
            // Nếu user đã login, redirect về trang chủ
            if (User.Identity?.IsAuthenticated == true)
            {
                Response.Redirect("/Home/LandingPage");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            try
            {
                var registrationRequest = new UserRegistrationDto
                {
                    FullName = FullName,
                    Email = Email,
                    Password = Password,
                    PhoneNumber = PhoneNumber
                };

                var result = await _authService.RegisterUserAsync(registrationRequest);

                if (result == null)
                {
                    ErrorMessage = "Đăng ký thất bại. Vui lòng thử lại.";
                    return Page();
                }

                SuccessMessage = "Đăng ký thành công! Đang chuyển hướng đến trang đăng nhập...";

                // Redirect đến trang login sau khi đăng ký thành công
                return RedirectToPage("/Auth/Login");
            }
            catch (Exception ex)
            {
                // Xử lý các exception từ ErrorHelper
                if (ex.Data.Contains("StatusCode"))
                {
                    var statusCode = (int)ex.Data["StatusCode"]!;
                    if (statusCode == 409) // Conflict - Email đã tồn tại
                    {
                        ErrorMessage = "Email này đã được đăng ký. Vui lòng sử dụng email khác.";
                    }
                    else
                    {
                        ErrorMessage = ex.Message;
                    }
                }
                else
                {
                    ErrorMessage = "Có lỗi xảy ra. Vui lòng thử lại sau.";
                }
                return Page();
            }
        }
    }
}