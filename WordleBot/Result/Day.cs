using WordleBot.Wordle;

namespace WordleBot.Result;

public class Day
{
    private readonly int _dayNumber;
    private readonly Dictionary<string, User> _results = new();

    public Day(int dayNumber)
    {
        _dayNumber = dayNumber;
    }

    public DayAddUserResult AddUserResult(string username, DateTimeOffset timestamp, WordleValidateResult result, int score)
    {
        if (_results.ContainsKey(username))
        {
            return DayAddUserResult.Known;
        }

        _results[username] = new User(timestamp, result.Attempts!.Value, score);
        return DayAddUserResult.New;
    }
}

public enum DayAddUserResult
{
    New,
    Known
}