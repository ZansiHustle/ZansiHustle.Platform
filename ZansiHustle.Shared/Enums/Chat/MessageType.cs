namespace ZansiHustle.Shared.Enums.Chat
{
    /// <summary>
    /// Kind of message stored on the Messages row. Text is the only
    /// thing senders can produce in v1. System is reserved for
    /// auto-generated context lines (e.g. "Order #ZH-001 created on
    /// 2026-05-29") that the server may insert at conversation start.
    /// Image / File reserved — not implemented in v1.
    /// </summary>
    public enum MessageType
    {
        Text   = 1,
        System = 2,
        Image  = 3,
        File   = 4,
    }
}
