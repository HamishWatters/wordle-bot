namespace WordleBot.Wordle;

public record WordleAnnouncementResult(WordleAnnouncementResultType Type, int? Day = null)
{
    public static WordleAnnouncementResult Success(int day)
    {
        return new WordleAnnouncementResult(WordleAnnouncementResultType.Success, day);
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