namespace ZansiHustle.Shared.Enums.ZansiPulse;

/// <summary>
/// Where a <c>ZansiPulseUserInterestScore</c> value originated. Stored as
/// <c>int</c>. A score seeded by onboarding starts as
/// <see cref="Onboarding"/>; once behavioural events adjust it, it becomes
/// <see cref="Mixed"/>. Scores created purely from behaviour are
/// <see cref="Behaviour"/>.
/// </summary>
public enum ZansiPulseInterestSource
{
    Onboarding = 1,
    Behaviour = 2,
    Mixed = 3,
}
