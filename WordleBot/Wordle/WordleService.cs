using System.Text;
using WordleBot.Answer;
using WordleBot.Bot;
using WordleBot.Config;
using WordleBot.Result;
using MessageResult = WordleBot.Bot.MessageResult;
using MessageResultType = WordleBot.Bot.MessageResultType;

namespace WordleBot.Wordle;

public class WordleService: IWordleService
{
    private readonly MessageConfig _messageConfig;
    private readonly IDisplayNameProvider _displayNameProvider;
    private readonly IAnswerProvider _answerProvider;
    private readonly IMessageProvider _messageProvider;
    
    private readonly Dictionary<int, Day> _results = new();
    private readonly PreviousAnswerTracking _previousAnswerTracking = new();

    public WordleService(MessageConfig messageConfig, IDisplayNameProvider displayNameProvider, IAnswerProvider answerProvider, IMessageProvider messageProvider)
    {
        _messageConfig = messageConfig;
        _displayNameProvider = displayNameProvider;
        _answerProvider = answerProvider;
        _messageProvider = messageProvider;

        BuildWords();
    }

    private async void BuildWords()
    {
        var messages = _messageProvider.GetWinnerMessageEnumerator(2000);
        await foreach (var message in messages)
        {
            _previousAnswerTracking.Feed(message);
        }
    }
    
    public bool TryGetResult(int dayNumber, out Day result)
    {
        return _results.TryGetValue(dayNumber, out result!);
    }

    public async Task<MessageResult> GetAnnouncementAsync(int day)
    {
        var dayResults = _results[day];
        var results = dayResults.GetSortedList();
        var builder = new StringBuilder();
        var winner = results[0];
        var winnerName = await _displayNameProvider.GetAsync(winner.Key);
        builder.Append(string.Format(_messageConfig.WinnerFormat, day, winnerName, winner.Value.Attempts, winner.Value.Score));
        var answer = await _answerProvider.GetAsync(day);
        if (answer != null)
        {
            builder.Append('\n');
            builder.Append(string.Format(_messageConfig.TodaysAnswerFormat, answer));
        }

        for (var i = 1; i < results.Count; i++)
        {
            builder.Append('\n');
            var name = _displayNameProvider.GetAsync(results[i].Key);
            builder.Append(string.Format(_messageConfig.RunnersUpFormat, i + 1, name, results[i].Value.Score));
        }

        return new MessageResult(MessageResultType.ForWinner, builder.ToString());
    }

    public DateOnly? GetDateForAnswer(string word)
    {
        return _previousAnswerTracking.PreviousAnswers.TryGetValue(word, out var date) ? date : null;
    }
}