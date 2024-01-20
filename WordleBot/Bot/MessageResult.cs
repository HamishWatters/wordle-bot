namespace WordleBot.Bot;

public struct MessageResult
{
    public MessageResult()
    {
    }

    public MessageResultType Type { get; set; } = MessageResultType.NoOp;
    public string? Content { get; set; } = null;
}