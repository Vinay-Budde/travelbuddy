namespace TravelBuddy.Models
{
    public enum UserStatus
    {
        PendingVerification,
        Active,
        Suspended,
        Deactivated
    }

    public enum FareProductStatus
    {
        Draft,
        Published,
        Paused,
        Archived
    }

    public enum OrderStatus
    {
        PendingPayment,
        Paid,
        PaymentFailed,
        Cancelled,
        PartiallyRefunded,
        Refunded
    }

    public enum PaymentStatus
    {
        Initiated,
        Pending,
        Succeeded,
        Failed,
        Cancelled,
        RefundPending,
        Refunded
    }

    public enum PassStatus
    {
        Upcoming,
        Active,
        Expired,
        Suspended,
        Revoked,
        Cancelled
    }

    public enum PassType
    {
        SingleJourney,
        Daily,
        Weekly,
        Monthly
    }

    public enum RefundStatus
    {
        Requested,
        Approved,
        Processing,
        Completed,
        Failed,
        Rejected
    }

    public enum ValidationResultCode
    {
        VALID,
        INVALID_TOKEN,
        NOT_YET_VALID,
        EXPIRED,
        REVOKED,
        SUSPENDED,
        OUT_OF_COVERAGE,
        ALREADY_USED,
        OPERATOR_UNAUTHORIZED,
        SYSTEM_UNAVAILABLE
    }
}
