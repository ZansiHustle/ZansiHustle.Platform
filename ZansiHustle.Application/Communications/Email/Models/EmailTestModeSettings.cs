namespace ZansiHustle.Application.Communications.Email.Models;

/// <summary>
/// DEPRECATED — superseded by <c>CommunicationTestMode</c> +
/// <c>ICommunicationRecipientResolver</c>. Retained ONLY so a previously-set
/// <c>EmailTestMode__OverrideShipmentEmailsTo</c> still works as a FALLBACK when
/// <c>CommunicationTestMode__OverrideEmailTo</c> is empty (the resolver logs a
/// deprecation warning when it falls back). No call site reads this directly any
/// more. Migrate UAT to <c>CommunicationTestMode__OverrideEmailTo</c> and remove
/// this section. Default empty = no override.
/// </summary>
public sealed class EmailTestModeSettings
{
    public const string SectionName = "EmailTestMode";

    /// <summary>DEPRECATED fallback only — prefer <c>CommunicationTestMode:OverrideEmailTo</c>.</summary>
    public string? OverrideShipmentEmailsTo { get; set; }
}
