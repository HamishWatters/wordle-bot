using WordleBot.Config;
using WordleBot.Wordle;

namespace WordleBot.Commands;

public class CommandParser
{
    private readonly string _commandPrefix;
    private readonly string _listCommand;
    private readonly string _endCommand;
    private readonly string _roundupCommand;
    private readonly string _seenCommand;
    private readonly string _helpCommand;
    
    public CommandParser(CommandConfig config)
    {
        _commandPrefix = config.Prefix;
        _listCommand = config.List;
        _endCommand = config.End;
        _roundupCommand = config.RoundUp;
        _seenCommand = config.Seen;
        _helpCommand = config.Help;
    }

    public Command? Parse(string input, DateTimeOffset timestamp)
    {
        var loweredInput = input.ToLowerInvariant();
        if (!loweredInput.StartsWith(_commandPrefix)) return null;

        var remaining = loweredInput[_commandPrefix.Length..].TrimStart();
        
        if (remaining.StartsWith(_listCommand))
        {
            return ParseList(remaining[_listCommand.Length..].TrimStart(), timestamp);
        }

        if (remaining.StartsWith(_endCommand))
        {
            return ParseEnd(remaining[_endCommand.Length..].TrimStart());
        }

        if (remaining.StartsWith(_roundupCommand))
        {
            return Command.RoundUp();
        }

        if (remaining.StartsWith(_seenCommand))
        {
            return ParseSeen(remaining[_seenCommand.Length..].TrimStart());
        }

        if (remaining.StartsWith(_helpCommand))
        {
            return Command.Help();
        }

        return Command.Unknown();
    }

    private static Command ParseList(string input, DateTimeOffset timestamp)
    {
        var dayString = input.Split(' ')[0];
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

    private static Command ParseSeen(string input)
    {
        return input.Length != 5 ? Command.Unknown() : Command.Seen(input);
    }
}