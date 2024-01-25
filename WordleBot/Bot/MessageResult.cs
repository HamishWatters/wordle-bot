namespace WordleBot.Bot;

public struct MessageResult
{
    public MessageResult(MessageResultType type, string? content = null)
    {
        Type = type;
        Content = content;
    }

    public MessageResultType Type { get; } = MessageResultType.NoOp;
    public string? Content { get; } = null;
}