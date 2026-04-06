namespace ZansiHustle.Infrastructure.Communications.Email.Directories;

/// <summary>
/// Stores configured sender identities for outbound email providers.
/// </summary>
public static class EmailSenderDirectory
{
    public const string HOST_SENDER = "mail.zansihustle.co.za";
    public const int HOST_PORT = 8889;
    public const bool HOST_ENABLE_SSL = false;
    public const string HOST_DOMAIN = "zansihustle.co.za";

    public static class Accounts
    {
        public const string Host = HOST_SENDER;
        public const int Port = HOST_PORT;
        public const string Username = $"accounts@{HOST_DOMAIN}";
        public const string Password = "Accounts1!";
        public const string FromEmail = $"accounts@{HOST_DOMAIN}";
        public const string FromName = "ZansiHustle Accounts";
        public const bool EnableSsl = HOST_ENABLE_SSL;
    }

    public static class Support
    {
        public const string Host = HOST_SENDER;
        public const int Port = HOST_PORT;
        public const string Username = $"support@{HOST_DOMAIN}";
        public const string Password = "$upportZan$1";
        public const string FromEmail = $"support@{HOST_DOMAIN}";
        public const string FromName = "ZansiHustle Support";
        public const bool EnableSsl = HOST_ENABLE_SSL;
    }

    public static class NoReply
    {
        public const string Host = HOST_SENDER;
        public const int Port = HOST_PORT;
        public const string Username = $"noreply@{HOST_DOMAIN}";
        public const string Password = "Zan$1Noreply";
        public const string FromEmail = $"noreply@{HOST_DOMAIN}";
        public const string FromName = "ZansiHustle";
        public const bool EnableSsl = HOST_ENABLE_SSL;
    }

    public static class Security
    {
        public const string Host = HOST_SENDER;
        public const int Port = HOST_PORT;
        public const string Username = $"security@{HOST_DOMAIN}";
        public const string Password = "$ecurityZans1";
        public const string FromEmail = $"security@{HOST_DOMAIN}";
        public const string FromName = "ZansiHustle Security";
        public const bool EnableSsl = HOST_ENABLE_SSL;
    }

    public static class Payments
    {
        public const string Host = HOST_SENDER;
        public const int Port = HOST_PORT;
        public const string Username = $"payments@{HOST_DOMAIN}";
        public const string Password = "Payment$Zan$1#ustle";
        public const string FromEmail = $"payments@{HOST_DOMAIN}";
        public const string FromName = "ZansiHustle Payments";
        public const bool EnableSsl = HOST_ENABLE_SSL;
    }
}