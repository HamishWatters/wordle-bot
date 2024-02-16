using System.Text;
using WordleBot.Answer;
using WordleBot.Bot;
using WordleBot.Config;
using WordleBot.Result;
using MessageResult = WordleBot.Bot.MessageResult;
using MessageResultType = WordleBot.Bot.MessageResultType;

namespace WordleBot.Wordle;

public class WordleService(
    MessageConfig messageConfig,
    IEnumerable<ulong> requiredUsers,
    IDisplayNameProvider displayNameProvider,
    IAnswerProvider answerProvider)
    : IWordleService
{
    private readonly Dictionary<int, Day> _results = new();
    private readonly PreviousAnswerTracking _previousAnswerTracking = new();

    public bool TryGetResult(int dayNumber, bool allowAnnounced, out Day result)
    {
        if (_results.TryGetValue(dayNumber, out var res))
        {
            if (!res.Announced || allowAnnounced)
            {
                result = res;
                return true;
            }
        }

        result = new Day();
        return false;
    }

    public async Task<MessageResult> GetAnnouncementAsync(int day)
    {
        var dayResults = _results[day];
        var results = dayResults.GetSortedList();
        var builder = new StringBuilder();
        var winner = results[0];
        var winnerName = await displayNameProvider.GetAsync(winner.Key);
        builder.Append(string.Format(messageConfig.WinnerFormat, day, winnerName, winner.Value.Attempts, winner.Value.Score));
        var answer = await answerProvider.GetAsync(day);
        if (answer != null)
        {
            builder.Append('\n');
            builder.Append(string.Format(messageConfig.TodaysAnswerFormat, answer));
        }

        for (var i = 1; i < results.Count; i++)
        {
            builder.Append('\n');
            var name = await displayNameProvider.GetAsync(results[i].Key);
            builder.Append(string.Format(messageConfig.RunnersUpFormat, i + 1, name, results[i].Value.Score));
        }

        return new MessageResult(MessageResultType.ForWinner, builder.ToString());
    }

    public DateOnly? GetDateForAnswer(string word)
    {
        return _previousAnswerTracking.PreviousAnswers.TryGetValue(word, out var date) ? date : null;
    }

    public async Task<MessageResult> TryProcessWordleAsync(ulong userId, DateTimeOffset timestamp, string messageContent)
    {
        var validateResult = WordleProcessor.Validate(messageContent);

        if (validateResult.Type != WordleValidateResultType.Success)
        {
            return new MessageResult(MessageResultType.NoOp);
        }
        var day = validateResult.Day!.Value;
        
        if (!_results.ContainsKey(day))
        {
            _results[day] = new Day();
        }

        var dayResult = _results[day];
        if (dayResult.Results.ContainsKey(userId))
        {
            return new MessageResult(MessageResultType.NoOp);
        }

        var score = WordleProcessor.Score(validateResult, messageContent);
        dayResult.Results[userId] = new User(timestamp, validateResult.Attempts!.Value, score);

        if (!dayResult.Announced && requiredUsers.All(dayResult.Results.ContainsKey))
        {
            return await GetAnnouncementAsync(day);
        }

        if (messageConfig.ResultResponses.TryGetValue(validateResult.Attempts!.Value, out var responses))
        {
            if (responses.Count == 0)
            {
                return new MessageResult(MessageResultType.NoOp);
            }

            var index = new Random().Next(responses.Count - 1);
            return new MessageResult(MessageResultType.ForWordle, responses[index]);
        }

        return new MessageResult(MessageResultType.NoOp);
    }

    public void ProcessWinnerMessage(string messageContent)
    {
        var announcementResult = WordleProcessor.IsAnnouncement(messageContent);

        if (announcementResult.Type != WordleAnnouncementResultType.Success)
        {
            return;
        }
        
        _previousAnswerTracking.Feed(messageContent);
        
        var day = announcementResult.Day!.Value;
        if (!_results.TryGetValue(day, out var value))
        {
            value = new Day();
            _results[day] = value;
        }

        value.Announced = true;
    }
}