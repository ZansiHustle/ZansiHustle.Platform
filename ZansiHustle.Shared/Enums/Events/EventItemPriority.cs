namespace ZansiHustle.Shared.Enums.Events;

/// <summary>
/// How critical a category is to the success of the event. Used to group
/// the checklist in the mobile Plan screen.
/// </summary>
public enum EventItemPriority
{
    Essential = 1,
    Recommended = 2,
    Optional = 3
}
