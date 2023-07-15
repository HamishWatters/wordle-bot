namespace WordleBot.Wordle;

public record WordleAnnouncementResult(WordleAnnouncementResultType Type, int? Day = null, string? username = null)
{
    public static WordleAnnouncementResult Success(int day, string username)
    {
        return new WordleAnnouncementResult(WordleAnnouncementResultType.Success, day, username);
    }

    public static WordleAnnouncementResult Failure(WordleAnnouncementResultType reason)
    {
        return new WordleAnnouncementResult(reason);
    }
}

public enum WordleAnnouncementResultType
{
    Success,
    RegexMismatch
}