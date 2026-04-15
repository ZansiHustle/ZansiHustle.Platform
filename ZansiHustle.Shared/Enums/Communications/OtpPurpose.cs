namespace ZansiHustle.Shared.Enums.Communications;

/// <summary>
/// Business purpose a one-time password was issued for.
/// </summary>
public enum OtpPurpose
{
    PhoneVerification = 1,
    EmailVerification = 2,
    PasswordReset = 3,
    LoginChallenge = 4,
    TransactionApproval = 5
}
