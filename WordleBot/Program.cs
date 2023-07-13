namespace WordleBot;

public class Program
{
    public static Task Main(string[] args) => MainAsync(args);

    private static async Task MainAsync(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new Exception("Missing API token");

        var bot = new DiscordBot();
        await bot.Launch(args[0]);
        await Task.Delay(-1);
    }
}