using Discord;

namespace WordleBot.Bot;

public interface IMessageService
{
    Task<MessageResult> HandleWordleMessageAsync(IMessage message, bool live);
    Task<MessageResult> HandleWinnerMessageAsync(IMessage message, bool live);
}