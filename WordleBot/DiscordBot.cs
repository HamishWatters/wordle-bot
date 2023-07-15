using Discord;
using Discord.WebSocket;
using WordleBot.Answer;
using WordleBot.Commands;
using WordleBot.Result;

namespace WordleBot;

public class DiscordBot
{
    private readonly ulong _guildId;
    private readonly ulong _wordleChannelId;
    private readonly ulong _winnerChannelId;
    private readonly ulong _botId;
    
    private readonly bool _testMode;
    private readonly IList<string> _requiredNames;
    private readonly IList<string> _adminNames;
    private readonly MessageConfig _messageConfig;
    
    private readonly DiscordSocketClient _discordClient;
    private readonly BotResults _results;
    private readonly CommandParser _commandParser;

    private readonly AnswerProvider _answerProvider = new();
    
    public DiscordBot(Config config)
    {
        _guildId = config.GuildChannel;
        _wordleChannelId = config.WordleChannel;
        _winnerChannelId = config.WinnerChannel;
        _botId = config.Bot;
        _testMode = config.TestMode;
        _requiredNames = config.RequiredUsers;
        _adminNames = config.Admins;
        _messageConfig = config.Message;
        
        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        };
        
        _discordClient = new DiscordSocketClient(discordConfig);
        _discordClient.Ready += ReadyHandler;
        _discordClient.MessageReceived += MessageReceivedHandler;

        _results = new BotResults(day => _requiredNames.All(name => day.Results.ContainsKey(name)));

        _commandParser = new CommandParser(config.Command);
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

            await ReadyHandlerChannel(guild, _winnerChannelId, "winner", HandleWinnerChannelMessageAsync);
            await ReadyHandlerChannel(guild, _wordleChannelId, "wordle", HandleWordleChannelMessageAsync);

            Console.WriteLine("Startup finished");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static async Task ReadyHandlerChannel(SocketGuild guild, ulong channelId, string channelDescription, Func<IMessage, bool, Task> messageAction)
    {
        var channel = guild.GetTextChannel(channelId);
        if (channel == null)
        {
            throw new Exception($"No {channelDescription} channel found");
        }

        await foreach (var page in channel.GetMessagesAsync(100))
        {
            foreach (var message in page)
            {
                await messageAction.Invoke(message, false);
            }
        }
    }

    private async Task MessageReceivedHandler(SocketMessage message)
    {
        var channelId = message.Channel.Id;

        if (channelId == _wordleChannelId)
        {
            await HandleWordleChannelMessageAsync(message, true);
        }
        else if (channelId == _winnerChannelId)
        {
            await HandleWinnerChannelMessageAsync(message, true);
        }
    }

    private async Task HandleWordleChannelMessageAsync(IMessage message, bool live)
    {
        
        var maybeCommand = _commandParser.Parse(message.Content);
        if (maybeCommand != null)
        {
            if (live)
            {
                await ProcessCommand(message.Author.Username, maybeCommand);
            }
        }
        else
        {
            var response = _results.ReceiveWordleMessage(message.Author.Username, message.Timestamp, message.Content);
            await HandleMessageResultAsync(response, message.Author.Username, live);
        }
    }

    private async Task ProcessCommand(string username, Command command)
    {
        switch (command.Type)
        {
            case CommandType.List:
                await ProcessList(command.Day!.Value);
                break;
            
            case CommandType.End:
                await ProcessEnd(username, command.Day!.Value);
                break;
            
            case CommandType.Unknown:
            default:
                Console.WriteLine("Unknown command");
                await SendMessageAsync(_wordleChannelId, _messageConfig.CommandUnknown);
                break;
        }
    }

    private async Task ProcessList(int day)
    {
        if (_results.Results.TryGetValue(day, out var dayResult))
        {
            await SendMessageAsync(_wordleChannelId, dayResult.GetListForMsg(_messageConfig.RunnersUpFormat));
        }
        else
        {
            await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandUnknownDay, day));
        }
    }

    private async Task ProcessEnd(string username, int day)
    {
        if (_adminNames.Contains(username))
        {
            if (_results.Results.TryGetValue(day, out var dayResult))
            {
                var answer = await _answerProvider.GetAsync(day);
                await SendMessageAsync(_winnerChannelId,
                    dayResult.GetWinMessage(_messageConfig.WinnerFormat, _messageConfig.TodaysAnswerFormat, _messageConfig.RunnersUpFormat, answer));
            }
            else
            {
                await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandUnknownDay, day));
            }
        }
        else
        {
            await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandNotAdmin));
        }
    }
    
    private async Task HandleWinnerChannelMessageAsync(IMessage message, bool live)
    {
        if (message.Author.Id != _botId)
        {
            // Only process announcements that the bot sent
            return;
        }

        var response = _results.ReceiveWinnerMessage(message.Content);
        await HandleMessageResultAsync(response, message.Author.Username, live);
    }

    private async Task HandleMessageResultAsync(MessageResult response, string author, bool live)
    {
        switch (response.Type)
        {
            case MessageResultType.Continue:
                // nothing happened
                break;
            
            case MessageResultType.Winner:
                // message had a new result, check for winner
                var day = _results.Results[response.Day!.Value];
                if (!day.Announced)
                {
                    day.Announced = true;
                    var answer = await _answerProvider.GetAsync(response.Day!.Value);
                    await SendMessageAsync(_winnerChannelId,
                        day.GetWinMessage(_messageConfig.WinnerFormat, _messageConfig.TodaysAnswerFormat, _messageConfig.RunnersUpFormat, answer));
                }
                else
                {
                    Console.WriteLine("Not sending because it's announced");
                }
                break;

            case MessageResultType.AlreadySubmitted:
                if (live)
                {
                    await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.AlreadySubmittedFormat, author, response.Day));
                }

                break;
            
            case MessageResultType.AlreadyAnnounced:
                if (live)
                {
                    await SendMessageAsync(_wordleChannelId,
                        string.Format(_messageConfig.SubmittedTooLateFormat, author, response.Day));
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