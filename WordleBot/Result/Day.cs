using System.Text;
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

    public DayAddUserResult AddUserResult(string username, DateTimeOffset timestamp, WordleValidateResult result, int score,
        Func<Day, bool> winCondition)
    {
        if (Results.ContainsKey(username))
        {
            return DayAddUserResult.Known;
        }

        Results[username] = new User(timestamp, result.Attempts!.Value, score);
        return winCondition.Invoke(this) ? DayAddUserResult.Win : DayAddUserResult.New;
    }

    public string GetWinMessage(string winnerFormat, string answerFormat, string runnersUpFormat, string? answer = null)
    {
        var results = Results.ToList();
        results.Sort((a, b) =>
        {
            var left = a.Value;
            var right = b.Value;
            var comparison = left.Attempts.CompareTo(right.Attempts); // compare left to right here as a smaller attempts is better
            if (comparison != 0) return comparison;

            comparison = right.Score.CompareTo(left.Score);
            if (comparison != 0) return comparison;

            return right.Timestamp.CompareTo(left.Timestamp);
        });
        
        var builder = new StringBuilder();
        var winner = results[0];
        builder.Append(string.Format(winnerFormat, _dayNumber, winner.Key, winner.Value.Attempts, winner.Value.Score));
        if (answer != null)
        {
            builder.Append('\n');
            builder.Append(string.Format(answerFormat, answer));
        }

        for (var i = 1; i < results.Count; i++)
        {
            builder.Append('\n');
            builder.Append(string.Format(runnersUpFormat, i + 1, results[i].Key, results[i].Value.Score));
        }

        return builder.ToString();
    }
}

public enum DayAddUserResult
{
    New,
    Known,
    Win
}