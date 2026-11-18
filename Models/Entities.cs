using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace TravelBuddy.Models
{
    public class ApplicationUser : IdentityUser
    {
        [MaxLength(150)]
        public string FullName { get; set; } = string.Empty;

        public UserStatus Status { get; set; } = UserStatus.Active;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAtUtc { get; set; }
    }

    public class Operator
    {
        [Key]
        public long OperatorId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required, MaxLength(50)]
        public string OperatorCode { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }

    public class Route
    {
        [Key]
        public long RouteId { get; set; }

        [Required, MaxLength(50)]
        public string RouteCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(30)]
        public string Status { get; set; } = "Published"; // Published, Inactive

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
        public virtual ICollection<FareProductRoute> FareProductRoutes { get; set; } = new List<FareProductRoute>();
    }

    public class Stop
    {
        [Key]
        public long StopId { get; set; }

        [Required, MaxLength(50)]
        public string StopCode { get; set; } = string.Empty;

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        [MaxLength(30)]
        public string Status { get; set; } = "Active";

        public virtual ICollection<RouteStop> RouteStops { get; set; } = new List<RouteStop>();
    }

    public class RouteStop
    {
        [Key]
        public long RouteStopId { get; set; }

        public long RouteId { get; set; }
        [ForeignKey("RouteId")]
        public virtual Route? Route { get; set; }

        public long StopId { get; set; }
        [ForeignKey("StopId")]
        public virtual Stop? Stop { get; set; }

        public int StopSequence { get; set; }
    }

    public class FareProduct
    {
        [Key]
        public long FareProductId { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        public PassType PassType { get; set; } = PassType.Daily;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        public int ValidityValue { get; set; } = 1; // 1, 7, 30

        [MaxLength(30)]
        public string ValidityUnit { get; set; } = "Days"; // Journeys, Days, Weeks, Months

        [MaxLength(50)]
        public string CoverageType { get; set; } = "AllRoutes"; // AllRoutes, SpecificRoutes

        public FareProductStatus Status { get; set; } = FareProductStatus.Published;

        public DateTime? SalesStartUtc { get; set; }

        public DateTime? SalesEndUtc { get; set; }

        public virtual ICollection<FareProductRoute> FareProductRoutes { get; set; } = new List<FareProductRoute>();
    }

    public class FareProductRoute
    {
        public long FareProductId { get; set; }
        [ForeignKey("FareProductId")]
        public virtual FareProduct? FareProduct { get; set; }

        public long RouteId { get; set; }
        [ForeignKey("RouteId")]
        public virtual Route? Route { get; set; }
    }

    public class Order
    {
        [Key]
        public long OrderId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [Required, MaxLength(50)]
        public string OrderNumber { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        public OrderStatus Status { get; set; } = OrderStatus.PendingPayment;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }

    public class OrderItem
    {
        [Key]
        public long OrderItemId { get; set; }

        public long OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        public long FareProductId { get; set; }
        [ForeignKey("FareProductId")]
        public virtual FareProduct? FareProduct { get; set; }

        [Required, MaxLength(150)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        public int Quantity { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal LineTotal { get; set; }

        public virtual Pass? IssuedPass { get; set; }
    }

    public class Payment
    {
        [Key]
        public long PaymentId { get; set; }

        public long OrderId { get; set; }
        [ForeignKey("OrderId")]
        public virtual Order? Order { get; set; }

        [MaxLength(50)]
        public string Provider { get; set; } = "Razorpay";

        [MaxLength(100)]
        public string? ProviderPaymentRef { get; set; } // razorpay_payment_id

        [MaxLength(100)]
        public string? ProviderOrderRef { get; set; } // razorpay_order_id

        [MaxLength(200)]
        public string? ProviderSignature { get; set; } // razorpay_signature

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;

        public DateTime? PaidAtUtc { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }

    public class Pass
    {
        [Key]
        public long PassId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        public long? OrderItemId { get; set; }
        [ForeignKey("OrderItemId")]
        public virtual OrderItem? OrderItem { get; set; }

        [Required, MaxLength(50)]
        public string PassNumber { get; set; } = string.Empty;

        public PassType PassType { get; set; } = PassType.Daily;

        public DateTime ValidFromUtc { get; set; }

        public DateTime ValidUntilUtc { get; set; }

        public PassStatus Status { get; set; } = PassStatus.Active;

        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedAtUtc { get; set; }

        [MaxLength(500)]
        public string? RevokeReason { get; set; }

        public virtual ICollection<QrCredential> QrCredentials { get; set; } = new List<QrCredential>();
        public virtual ICollection<BoardingValidation> BoardingValidations { get; set; } = new List<BoardingValidation>();
    }

    public class QrCredential
    {
        [Key]
        public long QrCredentialId { get; set; }

        public long PassId { get; set; }
        [ForeignKey("PassId")]
        public virtual Pass? Pass { get; set; }

        [Required, MaxLength(128)]
        public string TokenHash { get; set; } = string.Empty;

        [NotMapped]
        public string? RawToken { get; set; }

        public DateTime IssuedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAtUtc { get; set; }

        public DateTime? RevokedAtUtc { get; set; }

        public bool IsRevoked => RevokedAtUtc.HasValue;
    }

    public class BoardingValidation
    {
        [Key]
        public long ValidationId { get; set; }

        public long? PassId { get; set; }
        [ForeignKey("PassId")]
        public virtual Pass? Pass { get; set; }

        public long? OperatorId { get; set; }
        [ForeignKey("OperatorId")]
        public virtual Operator? Operator { get; set; }

        public long? RouteId { get; set; }
        [ForeignKey("RouteId")]
        public virtual Route? Route { get; set; }

        [Required, MaxLength(50)]
        public string ResultCode { get; set; } = "VALID";

        [MaxLength(250)]
        public string Message { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DeviceReference { get; set; }

        public DateTime ValidatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? RequestId { get; set; }
    }

    public class Refund
    {
        [Key]
        public long RefundId { get; set; }

        public long PaymentId { get; set; }
        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }

        public long? PassId { get; set; }
        [ForeignKey("PassId")]
        public virtual Pass? Pass { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        [ForeignKey("UserId")]
        public virtual ApplicationUser? User { get; set; }

        [MaxLength(100)]
        public string? GatewayRefundId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RefundAmount { get; set; }

        [MaxLength(250)]
        public string Reason { get; set; } = string.Empty;

        public RefundStatus Status { get; set; } = RefundStatus.Requested;

        [MaxLength(500)]
        public string? AdminRemarks { get; set; }

        public DateTime RequestedAtUtc { get; set; } = DateTime.UtcNow;

        public DateTime? ProcessedAtUtc { get; set; }
    }

    public class AuditLog
    {
        [Key]
        public long AuditLogId { get; set; }

        public string? ActorUserId { get; set; }

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        public string? BeforeJson { get; set; }

        public string? AfterJson { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [MaxLength(100)]
        public string? CorrelationId { get; set; }
    }
}
