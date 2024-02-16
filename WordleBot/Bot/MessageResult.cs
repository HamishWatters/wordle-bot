namespace WordleBot.Bot;

public struct MessageResult
{
    public MessageResult(MessageResultType type, string? content = null, int? wait = null)
    {
        Type = type;
        Content = content;
        Wait = wait;
    }

    public MessageResultType Type { get; } = MessageResultType.NoOp;
    public string? Content { get; } = null;
    public int? Wait { get; }
}