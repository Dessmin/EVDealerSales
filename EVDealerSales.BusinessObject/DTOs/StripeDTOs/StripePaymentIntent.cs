﻿namespace EVDealerSales.BusinessObject.DTOs.StripeDTOs
{
    public class StripePaymentIntent
    {
        public string Id { get; set; } = string.Empty;
        public string? ClientSecret { get; set; }
        public string Status { get; set; } = string.Empty;
        public long Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
