namespace WordleBot.Commands;

public class Command
{
    public CommandType Type { get; }
    public int? Day { get; }
    public string? Word { get; }

    private Command(CommandType type, int? day = null, string? word = null)
    {
        Type = type;
        Day = day;
        Word = word;
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

    public static Command Seen(string word)
    {
        return new Command(CommandType.Find, null, word);
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
    Find,
    Unknown
}