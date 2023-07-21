using System.Text;
using WordleBot.Wordle;

namespace WordleBot.Result;

public class Day
{
    private readonly int _dayNumber;
    
    public Dictionary<ulong, User> Results { get; } = new();
    public bool Announced { get; set; }

    public Day(int dayNumber)
    {
        _dayNumber = dayNumber;
    }

    public DayAddUserResult AddUserResult(ulong userId, DateTimeOffset timestamp, WordleValidateResult result, int score,
        Func<Day, bool> winCondition)
    {
        if (Results.ContainsKey(userId))
        {
            return DayAddUserResult.Known;
        }

        Results[userId] = new User(timestamp, result.Attempts!.Value, score);
        return winCondition.Invoke(this) ? DayAddUserResult.Win : DayAddUserResult.New;
    }

    public string GetWinMessage(string winnerFormat, string answerFormat, string runnersUpFormat, Dictionary<ulong, string> names, string? answer = null)
    {
        var results = GetSortedList();
        var builder = new StringBuilder();
        var winner = results[0];
        var winnerName = names[winner.Key];
        builder.Append(string.Format(winnerFormat, _dayNumber, winnerName, winner.Value.Attempts, winner.Value.Score));
        if (answer != null)
        {
            builder.Append('\n');
            builder.Append(string.Format(answerFormat, answer));
        }

        for (var i = 1; i < results.Count; i++)
        {
            builder.Append('\n');
            var name = names[results[i].Key];
            builder.Append(string.Format(runnersUpFormat, i + 1, name, results[i].Value.Score));
        }

        return builder.ToString();
    }

    public string GetListForMsg(string runnersUpFormat, Dictionary<ulong, string> names)
    {
        var results = GetSortedList();
        var builder = new StringBuilder();
        for (var i = 0; i < results.Count; i++)
        {
            if (i > 0)
            {
                builder.Append('\n');
            }

            var name = names[results[i].Key];
            builder.Append(string.Format(runnersUpFormat, i + 1, name, results[i].Value.Score));
        }

        return builder.ToString();
    }

    private List<KeyValuePair<ulong, User>> GetSortedList()
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

            return left.Timestamp.CompareTo(right.Timestamp);
        });
        return results;
    }
}

public enum DayAddUserResult
{
    New,
    Known,
    Win
}