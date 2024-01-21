using Discord;

namespace WordleBot.Bot;

public interface IMessageService
{
    Task<MessageResult> HandleWordleMessageAsync(IMessage message, bool live);
    void HandleWinnerMessage(IMessage message);
}