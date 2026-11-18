using System.ComponentModel.DataAnnotations;

namespace TravelBuddy.DTOs
{
    public class RegisterDto
    {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? PhoneNumber { get; set; }

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginDto
    {
        [Required, EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class CreateOrderRequest
    {
        [Required]
        public long FareProductId { get; set; }

        public int Quantity { get; set; } = 1;
    }

    public class OrderResponseDto
    {
        public long OrderId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Currency { get; set; } = "INR";
        public string RazorpayKeyId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class VerifyPaymentRequest
    {
        [Required]
        public long OrderId { get; set; }

        [Required]
        public string RazorpayOrderId { get; set; } = string.Empty;

        [Required]
        public string RazorpayPaymentId { get; set; } = string.Empty;

        [Required]
        public string RazorpaySignature { get; set; } = string.Empty;
    }

    public class PassDto
    {
        public long PassId { get; set; }
        public string PassNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string PassType { get; set; } = string.Empty;
        public DateTime ValidFromUtc { get; set; }
        public DateTime ValidUntilUtc { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Coverage { get; set; } = string.Empty;
        public bool IsEligibleForCancellation { get; set; }
    }

    public class QrCodeResponse
    {
        public string Token { get; set; } = string.Empty;
        public string QrBase64Image { get; set; } = string.Empty;
        public DateTime ExpiresAtUtc { get; set; }
        public int RefreshIntervalSeconds { get; set; } = 30;
    }

    public class ValidateQrRequest
    {
        [Required]
        public string QrToken { get; set; } = string.Empty;

        public long? RouteId { get; set; }

        public string? DeviceReference { get; set; }

        public string? RequestId { get; set; }
    }

    public class ValidateQrResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } = string.Empty; // VALID, EXPIRED, etc.
        public string Message { get; set; } = string.Empty;
        public string? PassReference { get; set; }
        public string? PassType { get; set; }
        public string? RouteName { get; set; }
        public DateTime? ValidUntilUtc { get; set; }
        public long? ValidationId { get; set; }
    }

    public class CancelPassRequest
    {
        public string Reason { get; set; } = "Passenger requested cancellation";
    }

    public class RefundProcessDto
    {
        public bool Approved { get; set; }
        public string? AdminRemarks { get; set; }
    }
}
