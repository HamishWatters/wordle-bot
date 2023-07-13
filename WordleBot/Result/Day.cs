using WordleBot.Wordle;

namespace WordleBot.Result;

public class Day
{
    private readonly int _dayNumber;
    
    public Dictionary<string, User> Results { get; } = new();
    public bool Announced { get; set; }

    public Day(int dayNumber)
    {
        _dayNumber = dayNumber;
    }

    public DayAddUserResult AddUserResult(string username, DateTimeOffset timestamp, WordleValidateResult result, int score)
    {
        if (Results.ContainsKey(username))
        {
            return DayAddUserResult.Known;
        }

        Results[username] = new User(timestamp, result.Attempts!.Value, score);
        return DayAddUserResult.New;
    }
}

public enum DayAddUserResult
{
    New,
    Known
}