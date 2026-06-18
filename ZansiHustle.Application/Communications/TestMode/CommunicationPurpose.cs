namespace ZansiHustle.Application.Communications.TestMode;

/// <summary>
/// What a communication is FOR. Drives test-mode recipient overriding: security
/// purposes (<see cref="SecurityOtp"/>/<see cref="LoginOtp"/>/<see cref="PasswordReset"/>)
/// are NEVER overridden unless explicitly allowed, so QA test-mode can never
/// silently capture a real user's auth codes.
/// </summary>
public enum CommunicationPurpose
{
    // Transactional / notification (overridable in test mode).
    ShipmentCustomer = 0,
    ShipmentSeller = 1,
    OrderStatusCustomer = 2,
    OrderStatusSeller = 3,
    DeliveryUpdateCustomer = 4,
    PickupUpdateSeller = 5,
    /// <summary>Seller-application approval welcome/compliance email — overridable in test mode.</summary>
    SellerApplicationApproved = 6,

    // Security (overridden ONLY when OverrideSecurityOtpRecipients=true).
    SecurityOtp = 100,
    LoginOtp = 101,
    PasswordReset = 102,
}
