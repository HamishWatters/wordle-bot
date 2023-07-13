using Discord;
using Discord.WebSocket;
using WordleBot.Result;

namespace WordleBot;

public class DiscordBot
{
    private const ulong GuildId = 326451862582722561;
    private const ulong WordleChannelId = 945629466800029736;
    private const ulong WinnerChannelId = 992503715820994651;
    
    private readonly DiscordSocketClient _discordClient;
    private readonly BotResults _results = new();
    
    public DiscordBot()
    {
        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        };
        
        _discordClient = new DiscordSocketClient(discordConfig);
        _discordClient.Ready += ReadyHandler;
    }

    public async Task Launch(string token)
    {
        await _discordClient.LoginAsync(TokenType.Bot, token);
        await _discordClient.StartAsync();
    }

    private async Task ReadyHandler()
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
    }
}