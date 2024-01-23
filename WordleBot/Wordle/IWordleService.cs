using WordleBot.Result;
using MessageResult = WordleBot.Bot.MessageResult;

namespace WordleBot.Wordle;

public interface IWordleService
{
    bool TryGetResult(int dayNumber, bool allowAnnounced, out Day day);
    Task<MessageResult> GetAnnouncementAsync(int commandDay);
    DateOnly? GetDateForAnswer(string word);
    Task<MessageResult> TryProcessWordleAsync(ulong userId, DateTimeOffset timestamp, string messageContent);
    void ProcessWinnerMessage(string messageContent);
}