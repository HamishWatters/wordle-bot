using System.Text.Json;

namespace WordleBot;

public static class Program
{
    public static Task Main(string[] args) => MainAsync(args);

    private static async Task MainAsync(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new Exception("Missing path to config file");

        Config config;
        await using (var file = new FileStream(args[0], FileMode.Open))
        {
            config = JsonSerializer.Deserialize<Config>(file, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase}) ??
                     throw new Exception("Failed to parse input config file");
        }

        var bot = new DiscordBot(config);
        await bot.Launch(config.ApiToken);
        await Task.Delay(-1);
    }
}