﻿namespace EVDealerSales.BusinessObject.DTOs.AuthDTOs
{
    public class UserRegistrationDto
    {
        public required string FullName { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
    }
}
