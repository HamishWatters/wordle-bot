using Discord;
using Discord.WebSocket;
using WordleBot.Result;

namespace WordleBot;

public class Program
{
    private const ulong GuildId = 326451862582722561;
    private const ulong WordleChannelId = 945629466800029736;
    private const ulong WinnerChannelId = 992503715820994651;
    
    private readonly BotResults _results = new();
    private DiscordSocketClient? _discordClient;
    
    public static Task Main(string[] args) => new Program().MainAsync(args);

    private async Task MainAsync(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            throw new Exception("Missing API token");
        Console.WriteLine(args[0]);

        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        };
        
        _discordClient = new DiscordSocketClient(discordConfig);
        _discordClient.Ready += async () =>
        {
            try
            {
                var guild = _discordClient.GetGuild(GuildId);
                if (guild == null)
                {
                    throw new Exception("No guild found");
                }

                var channel = guild.GetTextChannel(WordleChannelId);
                if (guild == null)
                {
                    throw new Exception("No channel found");
                }

                await foreach(var page in channel.GetMessagesAsync(100))
                {
                    foreach (var message in page)
                    {
                        _results.ReceiveMessage(message.Author.Username, message.Timestamp, message.Content);
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        };

        await _discordClient.LoginAsync(TokenType.Bot, args[0]);
        await _discordClient.StartAsync();

        await Task.Delay(-1);
    }
}