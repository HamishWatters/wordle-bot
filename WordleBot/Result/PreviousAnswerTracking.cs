using WordleBot.Wordle;

namespace WordleBot.Result;

public class PreviousAnswerTracking
{
    public Dictionary<string, DateOnly> PreviousAnswers { get; } = new();
    
    public void Feed(string message)
    {
        var winnerMatch = WordleProcessor.WinnerRegex.Match(message);
        if (!winnerMatch.Success)
        {
            return;
        }

        var dayNumber = int.Parse(winnerMatch.Groups[1].Value);
        var date = WordleUtil.DayOne.AddDays(dayNumber);
        PreviousAnswers[winnerMatch.Groups[5].Value] = date;
    }
}