using WordleBot.Wordle;

namespace WordleBot.Result;

public class BotResults
{
    public Dictionary<int, Day> Results { get; } = new();

    private readonly Func<Day, bool> _winCondition;

    public BotResults(Func<Day, bool> winCondition)
    {
        _winCondition = winCondition;
    }

    public MessageResult ReceiveWordleMessage(string authorUsername, DateTimeOffset timestamp, string messageContent)
    {
        var validateResult = WordleProcessor.Validate(messageContent);

        if (validateResult.Type == WordleValidateResultType.Success)
        {
            var day = validateResult.Day!.Value;
            if (!Results.ContainsKey(day))
            {
                Results[day] = new Day(day);
            }

            var dayResult = Results[day];
            if (dayResult.Announced) return new MessageResult(MessageResultType.AlreadyAnnounced);

            var score = WordleProcessor.Score(validateResult, messageContent);
            var addResult = dayResult.AddUserResult(authorUsername, timestamp, validateResult, score, _winCondition);

            return addResult switch
            {
                DayAddUserResult.New => new MessageResult(MessageResultType.Continue, validateResult.Day),
                DayAddUserResult.Win => new MessageResult(MessageResultType.Winner, validateResult.Day),
                DayAddUserResult.Known => new MessageResult(MessageResultType.AlreadySubmitted, validateResult.Day),
                _ => throw new ArgumentOutOfRangeException($"Unknown addResult {addResult}")
            };
        }
        else
        {
            return new MessageResult(MessageResultType.Continue);
        }


    }

    public MessageResult ReceiveWinnerMessage(string messageContent)
    {
        var announcementResult = WordleProcessor.IsAnnouncement(messageContent);

        if (announcementResult.Type == WordleAnnouncementResultType.Success)
        {
            var day = announcementResult.Day!.Value;
            if (Results.TryGetValue(day, out var result))
            {
                result.Announced = true;
            }
        }
        return new MessageResult(MessageResultType.Continue);

    }
}