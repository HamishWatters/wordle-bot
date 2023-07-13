using Discord;
using Discord.WebSocket;
using WordleBot.Result;

namespace WordleBot;

public class Program
{
    public static Task Main(string[] args) => new Program().MainAsync(args);

    private async Task MainAsync(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new Exception("Missing API token");

        var bot = new DiscordBot();
        await bot.Launch(args[0]);
        await Task.Delay(-1);
    }
}