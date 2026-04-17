namespace ZansiHustle.Shared.Enums.Events;

/// <summary>
/// Type of occasion the user is planning. Drives the template of recommended
/// service categories in the Event Builder flow.
/// </summary>
public enum EventType
{
    Wedding = 1,
    Birthday = 2,
    BabyShower = 3,
    Graduation = 4,
    Funeral = 5,
    Corporate = 6,
    Other = 7
}
