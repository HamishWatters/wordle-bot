using WordleBot.Commands;
using WordleBot.Config;
using WordleBot.Wordle;

namespace WordleBot.Bot.Commands;

public class CommandService(CommandConfig config)
    : ICommandService
{
    private readonly string _commandPrefix = config.Prefix;
    private readonly string _listCommand = config.List;
    private readonly string _endCommand = config.End;
    private readonly string _roundupCommand = config.RoundUp;
    private readonly string _findCommand = config.Find;
    private readonly string _helpCommand = config.Help;

    public bool TryParseCommand(string content, DateTimeOffset timestamp, out Command command)
    {
        var loweredContent = content.ToLowerInvariant();
        if (!loweredContent.StartsWith(_commandPrefix))
        {
            command = Command.Unknown();
            return false;
        }

        var commandContent = loweredContent[_commandPrefix.Length..].TrimStart();
        if (commandContent.StartsWith(_listCommand))
        {
            command = ParseList(commandContent[_listCommand.Length..].TrimStart(), timestamp);
            return true;
        }

        if (commandContent.StartsWith(_endCommand))
        {
            command = ParseEnd(commandContent[_endCommand.Length..].TrimStart());
            return true;
        }

        if (commandContent.StartsWith(_roundupCommand))
        {
            command = Command.RoundUp();
            return true;
        }

        if (commandContent.StartsWith(_findCommand))
        {
            command = ParseFind(commandContent[_findCommand.Length..].TrimStart());
            return true;
        }

        if (commandContent.StartsWith(_helpCommand))
        {
            command = Command.Help();
            return true;
        }

        command = Command.Unknown();
        return false;
    }

    private static Command ParseList(string input, DateTimeOffset timestamp)
    {
        var dayString = input.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)[0];
        if (!string.IsNullOrWhiteSpace(dayString))
        {
            return int.TryParse(dayString, out var days) ? Command.List(days) : Command.Unknown();
        }

        var date = DateOnly.FromDateTime(timestamp.Date);
        var dayNumber = date.DayNumber - WordleUtil.DayOne.DayNumber;
        return Command.List(dayNumber);
    }

    private static Command ParseEnd(string input)
    {
        var dayString = input.Split(' ')[0];
        return int.TryParse(dayString, out var days) ? Command.End(days) : Command.Unknown();
    }

    private static Command ParseFind(string input)
    {
        return input.Length switch
        {
            5 => Command.Find(input, false),
            9 when input.StartsWith("||") && input.EndsWith("||") => Command.Find(input.Substring(2, 5), true),
            _ => Command.Unknown()
        };
    }

    public MessageResult ProcessCommand(ulong userId, Command command)
    {
        throw new NotImplementedException();
    }
}