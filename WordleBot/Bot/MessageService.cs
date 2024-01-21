using Discord;
using WordleBot.Bot.Commands;

namespace WordleBot.Bot;

public class MessageService(Config.Config config, ICommandService commandService) : IMessageService
{
    private readonly ulong _botId = config.Bot;

    public Task<MessageResult> HandleWordleMessageAsync(IMessage message, bool live)
    {
        var result = new MessageResult();
        if (message.Author.Id == _botId)
        {
            return Task.FromResult(result);
        }

        if (commandService.TryParseCommand(message.Content, message.Timestamp, out var messageResult))
        {
            return commandService.ProcessCommand(message.Author.Id, messageResult);
        }

        throw new NotImplementedException();
    }

    public Task<MessageResult> HandleWinnerMessageAsync(IMessage message, bool live)
    {
        throw new NotImplementedException();
    }
}