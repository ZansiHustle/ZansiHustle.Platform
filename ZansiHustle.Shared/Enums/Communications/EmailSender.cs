namespace ZansiHustle.Shared.Enums.Communications;

/// <summary>
/// Represents the configured sender identity to use for outbound emails.
/// </summary>
public enum EmailSender
{
    Default = 0,
    Accounts = 1,
    Support = 2,
    NoReply = 3,
    Security = 4,
    Payments = 5
}