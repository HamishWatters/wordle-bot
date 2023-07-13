using WordleBot.Wordle;

namespace WordleBot.Result;

public class BotResults
{
    private readonly Dictionary<int, Day> _results = new();
    
    public void ReceiveMessage(string authorUsername, DateTimeOffset timestamp, string messageContent)
    {
        var result = WordleProcessor.Validate(messageContent);
        if (result.Type != WordleValidateResultType.Success) return;

        var day = result.Day!.Value;
        if (!_results.ContainsKey(day))
        {
            _results[day] = new Day(day);
        }
        var score = WordleProcessor.Score(result, messageContent);
        _results[day].AddUserResult(authorUsername, timestamp, result, score);

        Console.WriteLine($"{authorUsername} scored {score} on day {result.Day}");
    }
}