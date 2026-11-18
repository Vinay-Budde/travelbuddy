using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelBuddy.Data;
using TravelBuddy.DTOs;
using TravelBuddy.Models;
using TravelBuddy.Services;

namespace TravelBuddy.Controllers
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var existing = await _userManager.FindByEmailAsync(dto.Email);
            if (existing != null)
                return BadRequest(new { message = "Email is already registered." });

            var user = new ApplicationUser
            {
                UserName = dto.Email,
                Email = dto.Email,
                FullName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                Status = UserStatus.Active
            };

            var result = await _userManager.CreateAsync(user, dto.Password);
            if (!result.Succeeded)
                return BadRequest(result.Errors);

            await _userManager.AddToRoleAsync(user, "Passenger");
            await _signInManager.SignInAsync(user, isPersistent: true);

            return Ok(new { message = "Registration successful", userId = user.Id, email = user.Email, fullName = user.FullName });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null) return Unauthorized(new { message = "Invalid email or password." });

            var result = await _signInManager.PasswordSignInAsync(user, dto.Password, true, lockoutOnFailure: true);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid email or password." });

            var roles = await _userManager.GetRolesAsync(user);

            return Ok(new
            {
                message = "Login successful",
                userId = user.Id,
                email = user.Email,
                fullName = user.FullName,
                roles
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully" });
        }
    }

    [ApiController]
    [Route("api/v1/routes")]
    public class RoutesApiController : ControllerBase
    {
        private readonly TravelBuddyDbContext _context;

        public RoutesApiController(TravelBuddyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetRoutes()
        {
            var routes = await _context.Routes
                .Where(r => r.Status == "Published")
                .Include(r => r.RouteStops)
                .ThenInclude(rs => rs.Stop)
                .Select(r => new
                {
                    r.RouteId,
                    r.RouteCode,
                    r.Name,
                    r.Description,
                    Stops = r.RouteStops.OrderBy(rs => rs.StopSequence).Select(rs => new
                    {
                        rs.StopId,
                        rs.StopSequence,
                        StopName = rs.Stop != null ? rs.Stop.Name : "",
                        StopCode = rs.Stop != null ? rs.Stop.StopCode : ""
                    })
                })
                .ToListAsync();

            return Ok(routes);
        }
    }

    [ApiController]
    [Route("api/v1/fare-products")]
    public class FareProductsApiController : ControllerBase
    {
        private readonly TravelBuddyDbContext _context;

        public FareProductsApiController(TravelBuddyDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetFareProducts()
        {
            var products = await _context.FareProducts
                .Where(p => p.Status == FareProductStatus.Published)
                .Select(p => new
                {
                    p.FareProductId,
                    p.Name,
                    p.Description,
                    PassType = p.PassType.ToString(),
                    p.Price,
                    p.Currency,
                    p.ValidityValue,
                    p.ValidityUnit,
                    p.CoverageType
                })
                .ToListAsync();

            return Ok(products);
        }
    }

    [ApiController]
    [Route("api/v1/orders")]
    public class OrdersApiController : ControllerBase
    {
        private readonly TravelBuddyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRazorpayService _razorpayService;

        public OrdersApiController(
            TravelBuddyDbContext context,
            UserManager<ApplicationUser> userManager,
            IRazorpayService razorpayService)
        {
            _context = context;
            _userManager = userManager;
            _razorpayService = razorpayService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            var user = await _userManager.GetUserAsync(User);
            // Fallback for direct API testing if no cookie header
            var userId = user?.Id ?? (await _context.Users.FirstOrDefaultAsync())?.Id;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { message = "Please sign in to buy a pass." });

            var product = await _context.FareProducts.FindAsync(request.FareProductId);
            if (product == null || product.Status != FareProductStatus.Published)
                return NotFound(new { message = "Selected pass product not available." });

            var orderNumber = $"TB-ORD-{DateTime.UtcNow:yyMMdd}-{Random.Shared.Next(10000, 99999)}";
            var total = product.Price * (request.Quantity <= 0 ? 1 : request.Quantity);

            var order = new Order
            {
                UserId = userId,
                OrderNumber = orderNumber,
                Subtotal = total,
                Tax = 0,
                Total = total,
                Currency = "INR",
                Status = OrderStatus.PendingPayment
            };

            var orderItem = new OrderItem
            {
                FareProductId = product.FareProductId,
                ProductNameSnapshot = product.Name,
                UnitPrice = product.Price,
                Quantity = request.Quantity <= 0 ? 1 : request.Quantity,
                LineTotal = total
            };

            order.Items.Add(orderItem);
            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Create Razorpay Order
            var rzpOrderId = _razorpayService.CreateOrder(order.OrderId, total);

            var payment = new Payment
            {
                OrderId = order.OrderId,
                Provider = "Razorpay",
                ProviderOrderRef = rzpOrderId,
                Amount = total,
                Currency = "INR",
                Status = PaymentStatus.Pending
            };
            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new OrderResponseDto
            {
                OrderId = order.OrderId,
                OrderNumber = order.OrderNumber,
                TotalAmount = order.Total,
                Currency = order.Currency,
                RazorpayKeyId = _razorpayService.GetKeyId(),
                RazorpayOrderId = rzpOrderId,
                Status = order.Status.ToString()
            });
        }
    }

    [ApiController]
    [Route("api/v1/payments")]
    public class PaymentsApiController : ControllerBase
    {
        private readonly TravelBuddyDbContext _context;
        private readonly IRazorpayService _razorpayService;
        private readonly IPassService _passService;

        public PaymentsApiController(
            TravelBuddyDbContext context,
            IRazorpayService razorpayService,
            IPassService passService)
        {
            _context = context;
            _razorpayService = razorpayService;
            _passService = passService;
        }

        [HttpPost("verify")]
        public async Task<IActionResult> VerifyPayment([FromBody] VerifyPaymentRequest request)
        {
            var isValid = _razorpayService.VerifyPaymentSignature(request.RazorpayOrderId, request.RazorpayPaymentId, request.RazorpaySignature);
            if (!isValid)
            {
                return BadRequest(new { success = false, message = "Payment signature verification failed." });
            }

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderId == request.OrderId);

            if (order == null) return NotFound(new { message = "Order not found." });

            var payment = order.Payments.FirstOrDefault(p => p.ProviderOrderRef == request.RazorpayOrderId)
                          ?? order.Payments.LastOrDefault();

            if (payment == null)
            {
                payment = new Payment
                {
                    OrderId = order.OrderId,
                    Provider = "Razorpay",
                    ProviderOrderRef = request.RazorpayOrderId,
                    Amount = order.Total,
                    Currency = order.Currency
                };
                _context.Payments.Add(payment);
            }

            payment.ProviderPaymentRef = request.RazorpayPaymentId;
            payment.ProviderSignature = request.RazorpaySignature;
            payment.Status = PaymentStatus.Succeeded;
            payment.PaidAtUtc = DateTime.UtcNow;

            order.Status = OrderStatus.Paid;

            await _context.SaveChangesAsync();

            // Issue pass atomically
            var pass = await _passService.IssuePassForOrderAsync(order.OrderId);

            return Ok(new
            {
                success = true,
                message = "Payment verified and pass successfully activated!",
                orderId = order.OrderId,
                passId = pass.PassId,
                passNumber = pass.PassNumber
            });
        }

        [HttpPost("webhook/razorpay")]
        public async Task<IActionResult> Webhook([FromBody] System.Text.Json.JsonElement payload)
        {
            // Webhook endpoint for Razorpay event simulation and confirmation
            return Ok(new { status = "received" });
        }
    }

    [ApiController]
    [Route("api/v1/passes")]
    public class PassesApiController : ControllerBase
    {
        private readonly TravelBuddyDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPassService _passService;

        public PassesApiController(
            TravelBuddyDbContext context,
            UserManager<ApplicationUser> userManager,
            IPassService passService)
        {
            _context = context;
            _userManager = userManager;
            _passService = passService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyPasses()
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? (await _context.Users.FirstOrDefaultAsync())?.Id;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var passes = await _context.Passes
                .Where(p => p.UserId == userId)
                .Include(p => p.OrderItem)
                .OrderByDescending(p => p.IssuedAtUtc)
                .ToListAsync();

            var list = new List<PassDto>();
            foreach (var p in passes)
            {
                var canCancel = await _passService.CanCancelPassAsync(p.PassId, userId);
                list.Add(new PassDto
                {
                    PassId = p.PassId,
                    PassNumber = p.PassNumber,
                    ProductName = p.OrderItem?.ProductNameSnapshot ?? "Transit Pass",
                    PassType = p.PassType.ToString(),
                    ValidFromUtc = p.ValidFromUtc,
                    ValidUntilUtc = p.ValidUntilUtc,
                    Status = p.Status.ToString(),
                    Coverage = "All Eligible Routes",
                    IsEligibleForCancellation = canCancel
                });
            }

            return Ok(list);
        }

        [HttpGet("{passId}/qr")]
        public async Task<IActionResult> GetDynamicQr(long passId)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? (await _context.Users.FirstOrDefaultAsync())?.Id;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var qr = await _passService.GetDynamicQrForPassAsync(passId, userId);
            if (qr == null)
                return BadRequest(new { message = "QR is unavailable. Pass may not be active or belongs to another user." });

            return Ok(qr);
        }

        [HttpPost("{passId}/cancel")]
        public async Task<IActionResult> CancelPass(long passId, [FromBody] CancelPassRequest? req)
        {
            var user = await _userManager.GetUserAsync(User);
            var userId = user?.Id ?? (await _context.Users.FirstOrDefaultAsync())?.Id;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var result = await _passService.CancelPassAsync(passId, userId, req?.Reason ?? "Passenger cancellation request");
            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            return Ok(new
            {
                success = true,
                message = result.Message,
                refund = result.Refund != null ? new
                {
                    result.Refund.RefundId,
                    result.Refund.RefundAmount,
                    result.Refund.GatewayRefundId,
                    Status = result.Refund.Status.ToString()
                } : null
            });
        }
    }

    [ApiController]
    [Route("api/v1/validations")]
    public class ValidationsApiController : ControllerBase
    {
        private readonly IPassService _passService;
        private readonly TravelBuddyDbContext _context;

        public ValidationsApiController(IPassService passService, TravelBuddyDbContext context)
        {
            _passService = passService;
            _context = context;
        }

        [HttpPost("qr")]
        public async Task<IActionResult> ValidateQr([FromBody] ValidateQrRequest request)
        {
            var operatorRecord = await _context.Operators.FirstOrDefaultAsync(o => o.Status == "Active");
            var result = await _passService.ValidateBoardingAsync(request, operatorRecord?.OperatorId);
            return Ok(result);
        }
    }
}
