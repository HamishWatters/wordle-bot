namespace WordleBot.Config;

public class CommandConfig
{
    public string Prefix { get; set; } = "wordle-bot";
    public string List { get; set; } = "ls";
    public string End { get; set; } = "end";
    public string RoundUp { get; set; } = "roundup";
    public string Find { get; set; } = "find";
    public string Help { get; set; } = "help";
}