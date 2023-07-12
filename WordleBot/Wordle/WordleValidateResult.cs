namespace WordleBot.Wordle;

public record WordleValidateResult(WordleValidateResultType Type, int? Day = null, int? Attempts = null)
{
    public static WordleValidateResult Success(int day, int attempts)
    {
        return new WordleValidateResult(WordleValidateResultType.Success, day, attempts);
    }

    public static WordleValidateResult Failure(WordleValidateResultType reason)
    {
        return new WordleValidateResult(reason);
    }
}

public enum WordleValidateResultType
{
    Success,
    RegexMismatch,
    InvalidLineLength,
    InvalidAttempts
}