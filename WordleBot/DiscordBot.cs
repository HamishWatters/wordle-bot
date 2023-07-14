using Discord;
using Discord.WebSocket;
using WordleBot.Result;

namespace WordleBot;

public class DiscordBot
{
    private readonly ulong _guildId;
    private readonly ulong _wordleChannelId;
    private readonly ulong _winnerChannelId;
    
    private readonly bool _testMode;
    private readonly IList<string> _requiredNames;
    private readonly IList<string> _adminNames;
    private readonly MessageConfig _messageConfig;
    
    private readonly DiscordSocketClient _discordClient;
    private readonly BotResults _results = new();
    
    public DiscordBot(Config config)
    {
        _guildId = config.GuildChannel;
        _wordleChannelId = config.WordleChannel;
        _winnerChannelId = config.WinnerChannel;
        _testMode = config.TestMode;
        _requiredNames = config.RequiredUsers;
        _adminNames = config.Admins;
        _messageConfig = config.MessageConfig;
        
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
            var guild = _discordClient.GetGuild(_guildId);
            if (guild == null)
            {
                throw new Exception("No guild found");
            }

            SocketTextChannel wordleChannel = guild.GetTextChannel(_wordleChannelId);
            SocketTextChannel winnerChannel = guild.GetTextChannel(_winnerChannelId);

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
        var guild = _discordClient.GetGuild(_guildId);

        if (guild == null)
        {
            throw new Exception("No guild found");
        }

        var channel = message.Channel;

        if (channel == null)
        {
            throw new Exception("No channel found");
        }

        if (channel == guild.GetTextChannel(_wordleChannelId))
        {
            await HandleMessageAsync(message, live, _wordleChannelId);
        }
        else if (channel == guild.GetTextChannel(_winnerChannelId))
        {
            await HandleMessageAsync(message, live, _winnerChannelId);
        }
    }

    private async Task HandleMessageAsync(IMessage message, bool live, ulong channelId)
    {

        MessageResult response = new MessageResult(MessageResultType.Continue);

        if (channelId == _wordleChannelId)
        {
            response = _results.ReceiveWordleMessage(message.Author.Username, message.Timestamp, message.Content);
        }
        else if (channelId == _winnerChannelId)
        {
            response = _results.ReceiveWinnerMessage(message.Author.Username, message.Timestamp, message.Content);
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
                    await SendMessageAsync(_winnerChannelId, $"Winner");
                }

                break;

            case MessageResultType.AlreadySubmitted:
                if (live)
                {
                    await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.AlreadySubmitted, message.Author, response.Day));
                }

                break;
            
            case MessageResultType.AlreadyAnnounced:
                if (live)
                {
                    await SendMessageAsync(_wordleChannelId,
                        string.Format(_messageConfig.SubmittedTooLate, message.Author, response.Day));
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
        if (_testMode)
        {
            Console.WriteLine("Not sending message properly as we are in test mode");
            Console.WriteLine($"Sending to {channelId}: \"{message}\"");
        }
        else
        {
            var guild = _discordClient.GetGuild(_guildId);
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