using WordleBot.Result;
using MessageResult = WordleBot.Bot.MessageResult;

namespace WordleBot.Wordle;

public interface IWordleService
{
    bool TryGetResult(int dayNumber, out Day day);
    Task<MessageResult> GetAnnouncementAsync(int commandDay);
    DateOnly? GetDateForAnswer(string word);
}