namespace WordleBot.Commands;

public class Command
{
    public CommandType Type { get; }
    public int? Day { get; }

    private Command(CommandType type, int? day)
    {
        Type = type;
        Day = day;
    }

    public static Command List(int day)
    {
        return new Command(CommandType.List, day);
    }

    public static Command End(int day)
    {
        return new Command(CommandType.End, day);
    }

    public static Command RoundUp()
    {
        return new Command(CommandType.RoundUp, null);
    }

    public static Command Unknown()
    {
        return new Command(CommandType.Unknown, null);
    }
}

public enum CommandType
{
    List,
    End,
    RoundUp,
    Unknown
}