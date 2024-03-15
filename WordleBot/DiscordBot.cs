using Discord;
using Discord.WebSocket;
using Serilog;
using WordleBot.Answer;
using WordleBot.Bot;
using WordleBot.Bot.Commands;
using WordleBot.Wordle;

namespace WordleBot;

public class DiscordBot: IMessageProvider
{
    private readonly ILogger _log;
    private readonly MessageService _messageService;
    private readonly WordleService _wordleService;
    private readonly DisplayNameProvider _displayNameProvider;

    private readonly ulong _guildId;
    private readonly ulong _wordleChannelId;
    private readonly ulong _winnerChannelId;
    private readonly ulong _botId;
    
    private readonly bool _testMode;
    
    private readonly DiscordSocketClient _discordClient;

    public DiscordBot(Config.Config config, ILogger log)
    {
        _log = log;
        
        _guildId = config.GuildChannel;
        _wordleChannelId = config.WordleChannel;
        _winnerChannelId = config.WinnerChannel;
        _botId = config.Bot;
        _testMode = config.TestMode;
        
        var discordConfig = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildMembers
        };
        
        _discordClient = new DiscordSocketClient(discordConfig);
        _discordClient.Ready += ReadyHandler;
        _discordClient.MessageReceived += MessageReceivedHandler;
        _discordClient.Connected += () =>
        {
            _log.Debug("Bot is connected");
            return Task.CompletedTask;
        };
        _discordClient.Disconnected += exception =>
        {
            _log.Debug(exception, "Bot is disconnected");
            return Task.CompletedTask;
        };

        ScheduleDailyPollBackground(config.ScheduledCheckTime);
        
        var answerProvider = new AnswerProvider(log);
        _displayNameProvider = new DisplayNameProvider(_log, _discordClient);
        _wordleService = new WordleService(config.Message, config.RequiredUsers, _displayNameProvider, answerProvider);
        var commandService = new CommandService(
            log, config.Command, config.Message, config.Admins, config.UserNames,
            _wordleService, _displayNameProvider, this
        );
        _messageService = new MessageService(config, _wordleService, commandService);
    }

    public async Task Launch(string token)
    {
        await _discordClient.LoginAsync(TokenType.Bot, token);
        await _discordClient.StartAsync();
    }
    
    #region DiscordHandlers
    private async Task ReadyHandler()
    {
        try
        {
            var guild = _discordClient.GetGuild(_guildId);
            if (guild == null)
            {
                throw new Exception("No guild found");
            }

            var date = DateOnly.FromDateTime(DateTime.Now);
            var n = (int)(date.ToDateTime(TimeOnly.MinValue) - WordleUtil.DayOne.ToDateTime(TimeOnly.MinValue)).TotalDays;

            _log.Information("Bot is ready");
            await ReadyHandlerChannel(guild, _winnerChannelId, "winner", n * 3,
                (message, _) =>
                {
                    _messageService.HandleWinnerMessage(message);
                    return Task.CompletedTask;
                });

            _log.Information($"Processed {n * 3} messages from the winner channel");
            await ReadyHandlerChannel(guild, _wordleChannelId, "wordle", 1000, HandleWordleChannelMessageAsync);
            _log.Information("Processed 1000 messages from the wordle channel");
            _log.Information("Startup finished");
        }
        catch (Exception e)
        {
            _log.Error(e, "Error during startup");
            throw;
        }
    }

    private Task MessageReceivedHandler(SocketMessage message)
    {
        var channelId = message.Channel.Id;

        if (channelId == _wordleChannelId)
        {
            return HandleWordleChannelMessageAsync(message, true);
        }

        if (channelId == _winnerChannelId)
        {
            return HandleWinnerChannelMessageAsync(message);
        }

        return Task.CompletedTask;
    }
    #endregion

    private static async Task ReadyHandlerChannel(SocketGuild guild, ulong channelId, string channelDescription, int messages, Func<IMessage, bool, Task> messageAction)
    {
        var channel = guild.GetTextChannel(channelId);
        if (channel == null)
        {
            throw new Exception($"No {channelDescription} channel found");
        }

        await foreach (var page in channel.GetMessagesAsync(messages))
        {
            foreach (var message in page)
            {
                await messageAction.Invoke(message, false);
            }
        }
    }

    private async Task HandleWordleChannelMessageAsync(IMessage message, bool live)
    {
        if (message.Author.Id == _botId)
        {
            return;
        }

        var result = await _messageService.HandleWordleMessageAsync(message, live);
        if (result.Type == MessageResultType.NoOp)
        {
            return;
        }

        var targetChannelId = result.Type switch
        {
            MessageResultType.ForWordle => _wordleChannelId,
            MessageResultType.ForWinner => _winnerChannelId,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        if (result.Wait != null)
        {
            await Task.Delay(result.Wait.Value);
        }

        await SendMessageAsync(targetChannelId, result.Content!);
    }
    
    private Task HandleWinnerChannelMessageAsync(IMessage message)
    {
        if (message.Author.Id != _botId)
        {
            // Only process announcements that the bot sent
            return Task.CompletedTask;
        }
        
        _messageService.HandleWinnerMessage(message);
        return Task.CompletedTask;
    }

    private async Task SendMessageAsync(ulong channelId, string message)
    {
        if (_testMode)
        {
            _log.Information("Not sending message properly as we are in test mode");
            _log.Information($"Sending to {channelId}: \"{message}\"");
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

    private void ScheduleDailyPollBackground(string pollTime)
    {
        ScheduleDailyPoll(pollTime);
    }

    private async void ScheduleDailyPoll(string pollTime)
    {
        var now = DateTime.Now;
        var firstPoll = DateOnly.FromDateTime(now);
        if (TimeOnly.FromDateTime(now) > TimeOnly.Parse(pollTime))
        {
            // Too late, wait for tomorrow
            firstPoll = firstPoll.AddDays(1);
        }

        var nextPollDay = firstPoll;
        while (true)
        {
            var nextPollTime = nextPollDay.ToDateTime(TimeOnly.Parse(pollTime));
            now = DateTime.Now;
            var idleTime = nextPollTime - now;
            _log.Information($"Waiting {idleTime} for daily poll");
            await Task.Delay(idleTime);
            _log.Information("Executing daily poll...");
            var dayNumber = nextPollDay.DayNumber - WordleUtil.DayOne.DayNumber;

            if (!_wordleService.TryGetResult(dayNumber, false, out _))
            {
                continue;
            }

            var announcementResult = await _wordleService.GetAnnouncementAsync(dayNumber);
            await SendMessageAsync(_winnerChannelId, announcementResult.Content!);
            _displayNameProvider.ClearCache(); // let names refresh once per day 

            nextPollDay = nextPollDay.AddDays(1);
        }
    }

    public async IAsyncEnumerable<string> GetWinnerMessageEnumerator(int limit, int? year = null)
    {
        var guild = _discordClient.GetGuild(_guildId);
        if (guild == null)
        {
            throw new Exception($"No guild found for {_guildId}");
        }
        
        var channel = guild.GetTextChannel(_winnerChannelId);
        if (channel == null)
        {
            throw new Exception("No channel found");
        }

        await foreach (var page in channel.GetMessagesAsync(limit))
        {
            foreach (var message in page)
            {
                if (year != null && message.Timestamp.Year != year)
                {
                    yield break;
                }

                if (message.Author.Id == _botId)
                {
                    yield return message.Content;
                }
            }
        }
    }
}