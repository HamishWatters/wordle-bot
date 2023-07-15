using Discord;
using Discord.WebSocket;
using WordleBot.Result;

namespace WordleBot;

public class DiscordBot
{
    private const ulong GuildId = 326451862582722561;
    private const ulong WordleChannelId = 945629466800029736;
    private const ulong WinnerChannelId = 992503715820994651;
    private const bool TestMode = true;

    private readonly string[] _requiredNames = { "hamish.w", "honeystain", "zefiren", "valiantstar" };

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
        _discordClient.MessageReceived += MessageReceivedHandler;
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

            SocketTextChannel wordleChannel = guild.GetTextChannel(WordleChannelId);
            SocketTextChannel winnerChannel = guild.GetTextChannel(WinnerChannelId);

            await ReadyHandlerChannel(wordleChannel);
            await ReadyHandlerChannel(winnerChannel);

        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task ReadyHandlerChannel(SocketTextChannel channel)
    {
        if (channel == null)
        {
            throw new Exception("No channel found");
        }

        await foreach (var page in channel.GetMessagesAsync(100))
        {
            foreach (var message in page)
            {
                await HandleChannelAsync(message, false);
            }
        }
    }

    private async Task MessageReceivedHandler(SocketMessage message)
    {
        await HandleChannelAsync(message, true);
    }

    private async Task HandleChannelAsync(IMessage message, bool live)
    {
        var guild = _discordClient.GetGuild(GuildId);

        if (guild == null)
        {
            throw new Exception("No guild found");
        }

        var channel = message.Channel;

        if (channel == null)
        {
            throw new Exception("No channel found");
        }

        if (channel == guild.GetTextChannel(WordleChannelId))
        {
            await HandleMessageAsync(message, live, WordleChannelId);
        }
        else if (channel == guild.GetTextChannel(WinnerChannelId))
        {
            await HandleMessageAsync(message, live, WinnerChannelId);
        }
    }

    private async Task HandleMessageAsync(IMessage message, bool live, ulong channelId)
    {

        MessageResult response = new MessageResult(MessageResultType.Continue);

        switch (channelId)
        {
            case WordleChannelId:
                response = _results.ReceiveWordleMessage(message.Author.Username, message.Timestamp, message.Content);
                break;
            case WinnerChannelId:
                response = _results.ReceiveWinnerMessage(message.Author.Username, message.Timestamp, message.Content);
                break;
        }

        switch (response.Type)
        {
            case MessageResultType.Continue:
                // nothing happened
                break;

            case MessageResultType.NewSubmission:
                // message had a new result, check for winner
                var day = _results.Results[response.Day!.Value];
                if (_requiredNames.All(name => day.Results.ContainsKey(name)))
                {
                    await SendMessageAsync(WinnerChannelId, $"Winner");
                }

                break;

            case MessageResultType.AlreadySubmitted:
                if (live)
                {
                    await SendMessageAsync(WordleChannelId, $"{message.Author} has already submitted an answer for Wordle {response.Day}");
                }

                break;

            case MessageResultType.AlreadyAnnounced:
                if (live)
                {
                    await SendMessageAsync(WordleChannelId, $"{message.Author} is too late for Wordle {response.Day}");
                }

                break;

            case MessageResultType.Announcement:
                if (live)
                {
                    //Do shit
                }

                break;

        }

    }

    private async Task SendMessageAsync(ulong channelId, string message)
    {
        if (TestMode)
        {
            Console.WriteLine("Not sending message properly as we are in test mode");
            Console.WriteLine($"Sending to {channelId}: \"{message}\"");
        }
        else
        {
            var guild = _discordClient.GetGuild(GuildId);
            if (guild == null)
            {
                throw new Exception("No guild found");
            }

            var channel = guild.GetTextChannel(channelId);
            if (guild == null)
            {
                throw new Exception("No channel found");
            }

            await channel.SendMessageAsync(message);
        }
    }
}