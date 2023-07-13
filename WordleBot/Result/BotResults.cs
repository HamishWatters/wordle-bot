using WordleBot.Wordle;

namespace WordleBot.Result;

public class BotResults
{
    public Dictionary<int, Day> Results { get; } = new();
    
    public MessageResult ReceiveMessage(string authorUsername, DateTimeOffset timestamp, string messageContent)
    {
        var validateResult = WordleProcessor.Validate(messageContent);
        if (validateResult.Type != WordleValidateResultType.Success) return new MessageResult(MessageResultType.Continue);

        var day = validateResult.Day!.Value;
        if (!Results.ContainsKey(day))
        {
            Results[day] = new Day(day);
        }

        var dayResult = Results[day];
        if (dayResult.Announced) return new MessageResult(MessageResultType.AlreadyAnnounced);
        
        var score = WordleProcessor.Score(validateResult, messageContent);
        var addResult = dayResult.AddUserResult(authorUsername, timestamp, validateResult, score);

        return addResult switch
        {
            DayAddUserResult.New => new MessageResult(MessageResultType.NewSubmission, validateResult.Day),
            DayAddUserResult.Known => new MessageResult(MessageResultType.AlreadySubmitted, validateResult.Day),
            _ => throw new ArgumentOutOfRangeException($"Unknown addResult {addResult}")
        };
    }
}