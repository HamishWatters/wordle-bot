using WordleBot.Commands;

namespace WordleBot.Bot.Commands;

public interface ICommandService
{
    bool TryParseCommand(string content, DateTimeOffset timestamp, out Command command);
    Task<MessageResult> ProcessCommand(ulong userId, Command command);
}