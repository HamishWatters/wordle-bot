namespace WordleBot.Commands;

public class CommandParser
{
    private readonly string _commandPrefix;
    private readonly string _listCommand;
    private readonly string _endCommand;
    public CommandParser(CommandConfig config)
    {
        _commandPrefix = config.Prefix;
        _listCommand = config.List;
        _endCommand = config.End;
    }

    public Command? Parse(string input)
    {
        var loweredInput = input.ToLowerInvariant();
        if (!loweredInput.StartsWith(_commandPrefix)) return null;

        var remaining = loweredInput[_commandPrefix.Length..].TrimStart();
        
        if (remaining.StartsWith(_listCommand))
        {
            return ParseList(remaining[_listCommand.Length..].TrimStart());
        }

        if (remaining.StartsWith(_endCommand))
        {
            return ParseEnd(remaining[_endCommand.Length..].TrimStart());
        }

        return Command.Unknown();
    }

    private static Command ParseList(string input)
    {
        var dayString = input.Split(' ')[0];
        return int.TryParse(dayString, out var days) ? Command.List(days) : Command.Unknown();
    }

    private static Command ParseEnd(string input)
    {
        var dayString = input.Split(' ')[0];
        return int.TryParse(dayString, out var days) ? Command.End(days) : Command.Unknown();
    }
}