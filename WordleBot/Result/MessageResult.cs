namespace WordleBot.Result;

public struct MessageResult
{
    public MessageResultType Type { get; }
    public int? Day { get; }

    public MessageResult(MessageResultType type, int? day = null)
    {
        Type = type;
        Day = day;
    }
}

public enum MessageResultType
{
    Continue,
    Winner,
    AlreadySubmitted,
    AlreadyAnnounced,
    Announcement
}