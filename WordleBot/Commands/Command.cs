namespace WordleBot.Commands;

public class Command
{
    public CommandType Type { get; }
    public int? Day { get; }
    public string? Word { get; }
    public bool? Spoiler { get; }

    private Command(CommandType type, int? day = null, string? word = null, bool? spoiler = null)
    {
        Type = type;
        Day = day;
        Word = word;
        Spoiler = spoiler;
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
        return new Command(CommandType.RoundUp);
    }

    public static Command Seen(string word, bool spoiler)
    {
        return new Command(CommandType.Find, null, word, spoiler);
    }

    public static Command Help()
    {
        return new Command(CommandType.Help);
    }

    public static Command Unknown()
    {
        return new Command(CommandType.Unknown);
    }
}

public enum CommandType
{
    List,
    End,
    RoundUp,
    Find,
    Unknown,
    Help
}