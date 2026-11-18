using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.DTOs;
using TravelBuddy.Models;

namespace TravelBuddy.Services
{
    public interface IPassService
    {
        Task<Pass> IssuePassForOrderAsync(long orderId);
        Task<QrCodeResponse?> GetDynamicQrForPassAsync(long passId, string userId);
        Task<bool> CanCancelPassAsync(long passId, string userId);
        Task<(bool Success, string Message, Refund? Refund)> CancelPassAsync(long passId, string userId, string reason);
        Task<ValidateQrResponse> ValidateBoardingAsync(ValidateQrRequest request, long? operatorId);
    }

    public class PassService : IPassService
    {
        private readonly TravelBuddyDbContext _context;
        private readonly IQrCredentialService _qrService;
        private readonly IRazorpayService _razorpayService;
        private readonly ILogger<PassService> _logger;

        public PassService(
            TravelBuddyDbContext context,
            IQrCredentialService qrService,
            IRazorpayService razorpayService,
            ILogger<PassService> logger)
        {
            _context = context;
            _qrService = qrService;
            _razorpayService = razorpayService;
            _logger = logger;
        }

        public async Task<Pass> IssuePassForOrderAsync(long orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Items)
                .ThenInclude(i => i.FareProduct)
                .FirstOrDefaultAsync(o => o.OrderId == orderId);

            if (order == null) throw new InvalidOperationException("Order not found");

            var firstItem = order.Items.FirstOrDefault();
            if (firstItem == null) throw new InvalidOperationException("Order contains no items");

            var fareProduct = firstItem.FareProduct;
            var now = DateTime.UtcNow;

            DateTime validFrom = now;
            DateTime validUntil = fareProduct?.PassType switch
            {
                PassType.SingleJourney => now.AddHours(2),
                PassType.Daily => now.Date.AddDays(1).AddSeconds(-1),
                PassType.Weekly => now.AddDays(7),
                PassType.Monthly => now.AddDays(30),
                _ => now.AddDays(1)
            };

            var pass = new Pass
            {
                UserId = order.UserId,
                OrderItemId = firstItem.OrderItemId,
                PassNumber = $"TB-PASS-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(1000, 9999)}",
                PassType = fareProduct?.PassType ?? PassType.Daily,
                ValidFromUtc = validFrom,
                ValidUntilUtc = validUntil,
                Status = PassStatus.Active,
                IssuedAtUtc = now
            };

            _context.Passes.Add(pass);
            await _context.SaveChangesAsync();

            // Generate initial credential
            await GenerateNewCredentialForPass(pass.PassId);

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = order.UserId,
                Action = "PASS_ISSUED",
                EntityType = "Pass",
                EntityId = pass.PassId.ToString(),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new { pass.PassNumber, pass.PassType, pass.ValidFromUtc, pass.ValidUntilUtc })
            });
            await _context.SaveChangesAsync();

            return pass;
        }

        private async Task<QrCredential> GenerateNewCredentialForPass(long passId)
        {
            var rawToken = _qrService.GenerateRawToken();
            var tokenHash = _qrService.HashToken(rawToken);

            var credential = new QrCredential
            {
                PassId = passId,
                TokenHash = tokenHash,
                RawToken = rawToken,
                IssuedAtUtc = DateTime.UtcNow,
                ExpiresAtUtc = DateTime.UtcNow.AddSeconds(45) // Short-lived 45s window
            };

            _context.QrCredentials.Add(credential);
            await _context.SaveChangesAsync();

            return credential;
        }

        public async Task<QrCodeResponse?> GetDynamicQrForPassAsync(long passId, string userId)
        {
            var pass = await _context.Passes.FirstOrDefaultAsync(p => p.PassId == passId && p.UserId == userId);
            if (pass == null || pass.Status != PassStatus.Active) return null;

            // Generate a fresh dynamic rotating credential
            var cred = await GenerateNewCredentialForPass(passId);
            return _qrService.CreateDynamicQr(cred.RawToken!, cred.ExpiresAtUtc);
        }

        public async Task<bool> CanCancelPassAsync(long passId, string userId)
        {
            var pass = await _context.Passes
                .Include(p => p.BoardingValidations)
                .Include(p => p.OrderItem)
                .ThenInclude(i => i!.Order)
                .FirstOrDefaultAsync(p => p.PassId == passId && p.UserId == userId);

            if (pass == null) return false;
            if (pass.Status != PassStatus.Active && pass.Status != PassStatus.Upcoming) return false;

            // Product rules: Single journey is refundable before any QR boarding.
            // Passes can be cancelled if no successful boardings were ever recorded.
            var hasSuccessfulBoarding = pass.BoardingValidations.Any(v => v.ResultCode == "VALID");
            if (hasSuccessfulBoarding) return false;

            // If time-based pass started and had boardings or exceeded start
            if (pass.PassType != PassType.SingleJourney && DateTime.UtcNow > pass.ValidFromUtc.AddHours(2) && hasSuccessfulBoarding)
                return false;

            return true;
        }

        public async Task<(bool Success, string Message, Refund? Refund)> CancelPassAsync(long passId, string userId, string reason)
        {
            var canCancel = await CanCancelPassAsync(passId, userId);
            if (!canCancel)
            {
                return (false, "This pass is not eligible for cancellation or has already been used.", null);
            }

            var pass = await _context.Passes
                .Include(p => p.OrderItem)
                .ThenInclude(i => i!.Order)
                .ThenInclude(o => o!.Payments)
                .FirstOrDefaultAsync(p => p.PassId == passId && p.UserId == userId);

            if (pass == null) return (false, "Pass not found", null);

            var payment = pass.OrderItem?.Order?.Payments.FirstOrDefault(p => p.Status == PaymentStatus.Succeeded);
            decimal refundAmount = pass.OrderItem?.LineTotal ?? 0;

            pass.Status = PassStatus.Cancelled;
            pass.RevokedAtUtc = DateTime.UtcNow;
            pass.RevokeReason = reason;

            string gatewayRefundId = _razorpayService.RefundPayment(payment?.ProviderPaymentRef ?? "sim_pay", refundAmount);

            var refund = new Refund
            {
                PaymentId = payment?.PaymentId ?? 0,
                PassId = pass.PassId,
                UserId = userId,
                RefundAmount = refundAmount,
                Reason = reason,
                Status = RefundStatus.Completed,
                GatewayRefundId = gatewayRefundId,
                RequestedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                AdminRemarks = "Auto-refunded in accordance with MVP Cancellation Policy"
            };

            _context.Refunds.Add(refund);

            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = userId,
                Action = "PASS_CANCELLED_REFUND",
                EntityType = "Pass",
                EntityId = pass.PassId.ToString(),
                AfterJson = System.Text.Json.JsonSerializer.Serialize(new { refund.RefundId, refund.RefundAmount, refund.Status })
            });

            await _context.SaveChangesAsync();

            return (true, "Pass has been cancelled and full refund of ₹" + refundAmount + " has been initiated to your original payment method.", refund);
        }

        public async Task<ValidateQrResponse> ValidateBoardingAsync(ValidateQrRequest request, long? operatorId)
        {
            var now = DateTime.UtcNow;
            var hashed = _qrService.HashToken(request.QrToken);

            var credential = await _context.QrCredentials
                .Include(q => q.Pass)
                .ThenInclude(p => p!.OrderItem)
                .ThenInclude(i => i!.FareProduct)
                .ThenInclude(fp => fp!.FareProductRoutes)
                .FirstOrDefaultAsync(q => q.TokenHash == hashed);

            var validation = new BoardingValidation
            {
                OperatorId = operatorId,
                RouteId = request.RouteId,
                DeviceReference = request.DeviceReference ?? "Validator-Web-01",
                ValidatedAtUtc = now,
                RequestId = request.RequestId ?? Guid.NewGuid().ToString()
            };

            if (credential == null)
            {
                validation.ResultCode = ValidationResultCode.INVALID_TOKEN.ToString();
                validation.Message = "Token does not exist or is malformed.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message };
            }

            validation.PassId = credential.PassId;
            var pass = credential.Pass;

            if (credential.IsRevoked)
            {
                validation.ResultCode = ValidationResultCode.REVOKED.ToString();
                validation.Message = "The scanned credential has been revoked.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass?.PassNumber };
            }

            if (now > credential.ExpiresAtUtc)
            {
                validation.ResultCode = ValidationResultCode.EXPIRED.ToString();
                validation.Message = "Dynamic QR code has expired. Please refresh the QR screen.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass?.PassNumber };
            }

            if (pass == null || pass.Status != PassStatus.Active)
            {
                validation.ResultCode = pass?.Status == PassStatus.Revoked || pass?.Status == PassStatus.Cancelled ?
                    ValidationResultCode.REVOKED.ToString() :
                    ValidationResultCode.SUSPENDED.ToString();
                validation.Message = $"Pass is not active. Status: {pass?.Status}";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass?.PassNumber };
            }

            if (now < pass.ValidFromUtc)
            {
                validation.ResultCode = ValidationResultCode.NOT_YET_VALID.ToString();
                validation.Message = $"Pass validity starts at {pass.ValidFromUtc:yyyy-MM-dd HH:mm} UTC.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass.PassNumber };
            }

            if (now > pass.ValidUntilUtc)
            {
                validation.ResultCode = ValidationResultCode.EXPIRED.ToString();
                validation.Message = $"Pass expired on {pass.ValidUntilUtc:yyyy-MM-dd HH:mm} UTC.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass.PassNumber };
            }

            // Route eligibility check
            var fareProduct = pass.OrderItem?.FareProduct;
            if (request.RouteId.HasValue && fareProduct != null && fareProduct.CoverageType == "SpecificRoutes")
            {
                var isRouteAllowed = fareProduct.FareProductRoutes.Any(r => r.RouteId == request.RouteId.Value);
                if (!isRouteAllowed)
                {
                    validation.ResultCode = ValidationResultCode.OUT_OF_COVERAGE.ToString();
                    validation.Message = "This pass is not valid for the selected route.";
                    _context.BoardingValidations.Add(validation);
                    await _context.SaveChangesAsync();
                    return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass.PassNumber };
                }
            }

            // Boarding Policy check:
            // Single Journey: strictly ONE boarding. After 1 valid scan, mark as USED/EXPIRED.
            if (pass.PassType == PassType.SingleJourney)
            {
                var pastValidScan = await _context.BoardingValidations
                    .AnyAsync(v => v.PassId == pass.PassId && v.ResultCode == "VALID");

                if (pastValidScan)
                {
                    validation.ResultCode = ValidationResultCode.ALREADY_USED.ToString();
                    validation.Message = "Single Journey ticket has already been used.";
                    _context.BoardingValidations.Add(validation);
                    await _context.SaveChangesAsync();
                    return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass.PassNumber };
                }
            }

            // Unlimited passes (Daily, Weekly, Monthly):
            // Replay/Duplicate check: protect from double-scanning within 60 seconds on the same device
            var recentValidation = await _context.BoardingValidations
                .Where(v => v.PassId == pass.PassId && v.ResultCode == "VALID")
                .OrderByDescending(v => v.ValidatedAtUtc)
                .FirstOrDefaultAsync();

            if (recentValidation != null && (now - recentValidation.ValidatedAtUtc).TotalSeconds < 30)
            {
                validation.ResultCode = "DUPLICATE_SCAN";
                validation.Message = "Scan detected within 30 seconds of previous validation. Anti-passback alert.";
                _context.BoardingValidations.Add(validation);
                await _context.SaveChangesAsync();
                return new ValidateQrResponse { Success = false, Status = validation.ResultCode, Message = validation.Message, PassReference = pass.PassNumber };
            }

            // If single journey, mark pass as Expired/Used now
            if (pass.PassType == PassType.SingleJourney)
            {
                pass.Status = PassStatus.Expired;
            }

            // SUCCESS!
            validation.ResultCode = ValidationResultCode.VALID.ToString();
            validation.Message = "Pass verified successfully. Boarding Accepted.";
            _context.BoardingValidations.Add(validation);
            await _context.SaveChangesAsync();

            var maskedPass = pass.PassNumber.Length > 8 
                ? "•••• " + pass.PassNumber[^4..]
                : pass.PassNumber;

            return new ValidateQrResponse
            {
                Success = true,
                Status = "VALID",
                Message = validation.Message,
                PassReference = maskedPass,
                PassType = pass.PassType.ToString(),
                RouteName = request.RouteId.HasValue ? $"Route #{request.RouteId}" : "All Eligible Routes",
                ValidUntilUtc = pass.ValidUntilUtc,
                ValidationId = validation.ValidationId
            };
        }
    }
}
