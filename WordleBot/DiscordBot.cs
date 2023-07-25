using Discord;
using Discord.WebSocket;
using Serilog;
using Serilog.Core;
using WordleBot.Answer;
using WordleBot.Commands;
using WordleBot.Result;
using WordleBot.Wordle;

namespace WordleBot;

public class DiscordBot
{
    private readonly ILogger _log;
    private readonly ulong _guildId;
    private readonly ulong _wordleChannelId;
    private readonly ulong _winnerChannelId;
    private readonly ulong _botId;
    
    private readonly bool _testMode;
    private readonly IList<ulong> _adminIds;
    private readonly MessageConfig _messageConfig;
    
    private readonly DiscordSocketClient _discordClient;
    private readonly BotResults _results;
    private readonly CommandParser _commandParser;

    private readonly AnswerProvider _answerProvider;
    
    public DiscordBot(Config config, ILogger log)
    {
        _log = log;
        _answerProvider = new AnswerProvider(log);
        
        _guildId = config.GuildChannel;
        _wordleChannelId = config.WordleChannel;
        _winnerChannelId = config.WinnerChannel;
        _botId = config.Bot;
        _testMode = config.TestMode;
        _adminIds = config.Admins;
        _messageConfig = config.Message;
        
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

        var requiredNames = config.RequiredUsers;
        _results = new BotResults(day => requiredNames.All(id => day.Results.ContainsKey(id)));

        _commandParser = new CommandParser(config.Command);
        
        ScheduleDailyPollBackground(config.ScheduledCheckTime);
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

            await ReadyHandlerChannel(guild, _winnerChannelId, "winner", 100, HandleWinnerChannelMessageAsync);
            await ReadyHandlerChannel(guild, _wordleChannelId, "wordle", 250, HandleWordleChannelMessageAsync);

            _log.Information("Startup finished");
        }
        catch (Exception e)
        {
            _log.Error(e, "Error during startup");
            throw;
        }
    }

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
                await ProcessCommand(message.Author.Id, maybeCommand);
            }
        }
        else
        {
            var response = _results.ReceiveWordleMessage(message.Author.Id, message.Timestamp, message.Content);
            await HandleMessageResultAsync(response, message.Author.Id, live);
        }
    }

    private async Task ProcessCommand(ulong id, Command command)
    {
        switch (command.Type)
        {
            case CommandType.List:
                await ProcessList(command.Day!.Value);
                break;
            
            case CommandType.End:
                await ProcessEnd(id, command.Day!.Value);
                break;
            
            case CommandType.Unknown:
            default:
                _log.Warning("Unknown command");
                await SendMessageAsync(_wordleChannelId, _messageConfig.CommandUnknown);
                break;
        }
    }

    private async Task ProcessList(int day)
    {
        if (_results.Results.TryGetValue(day, out var dayResult))
        {
            var names = await GetDisplayNameMap(dayResult.Results.Keys);
            await SendMessageAsync(_wordleChannelId, dayResult.GetListForMsg(_messageConfig.RunnersUpFormat, names));
        }
        else
        {
            await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandUnknownDay, day));
        }
    }

    private async Task ProcessEnd(ulong id, int day)
    {
        if (_adminIds.Contains(id))
        {
            if (_results.Results.TryGetValue(day, out var dayResult))
            {
                var answer = await _answerProvider.GetAsync(day);
                var names = await GetDisplayNameMap(dayResult.Results.Keys);
                await SendMessageAsync(_winnerChannelId,
                    dayResult.GetWinMessage(_messageConfig.WinnerFormat, _messageConfig.TodaysAnswerFormat,
                        _messageConfig.RunnersUpFormat, names, answer));
            }
            else
            {
                await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandUnknownDay, day));
            }
        }
        else
        {
            var name = await ResolveName(id);
            await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.CommandNotAdmin, name));
        }
    }

    private async Task<Dictionary<ulong, string>> GetDisplayNameMap(IEnumerable<ulong> ids)
    {
        var ret = new Dictionary<ulong, string>();
        var tasks = ids.Select(async id => ret[id] = await ResolveName(id));
        await Task.WhenAll(tasks);
        return ret;
    }

    private async Task HandleWinnerChannelMessageAsync(IMessage message, bool live)
    {
        if (message.Author.Id != _botId)
        {
            // Only process announcements that the bot sent
            return;
        }

        var response = _results.ReceiveWinnerMessage(message.Content);
        await HandleMessageResultAsync(response, message.Author.Id, live);
    }

    private async Task HandleMessageResultAsync(MessageResult response, ulong id, bool live)
    {
        switch (response.Type)
        {
            case MessageResultType.Continue:
                // nothing happened
                break;
            
            case MessageResultType.Winner:
                // message had a new result, check for winner
                await TryAnnounceDayAsync(response.Day!.Value);
                break;

            case MessageResultType.AlreadySubmitted:
                if (live)
                {
                    var name = await ResolveName(id);
                    await SendMessageAsync(_wordleChannelId, string.Format(_messageConfig.AlreadySubmittedFormat, name, response.Day));
                }

                break;
            
        }
    }

    private async Task TryAnnounceDayAsync(int dayNumber)
    {
        var day = _results.Results[dayNumber];
        if (!day.Announced)
        {
            day.Announced = true;
            var answer = await _answerProvider.GetAsync(dayNumber);
            var names = await GetDisplayNameMap(day.Results.Keys);
            await SendMessageAsync(_winnerChannelId,
                day.GetWinMessage(_messageConfig.WinnerFormat, _messageConfig.TodaysAnswerFormat,
                    _messageConfig.RunnersUpFormat, names, answer));
        }
        else
        {
            _log.Information("Not sending because it's announced");
        }
    }  

    private async Task<string> ResolveName(ulong userId, string fallbackName = "?????")
    {
        try
        {
            var user = await _discordClient.GetUserAsync(userId);
            if (user == null)
            {
                return fallbackName;
            }

            if (user.GlobalName != null)
            {
                return user.GlobalName;
            }

            return user.Username ?? fallbackName;
        }
        catch (Exception e)
        {
            _log.Warning(e, $"Error resolving name '{userId}'");
            return fallbackName;
        }
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

            if (!_results.Results.ContainsKey(dayNumber))
            {
                _log.Information($"Nobody has done day {dayNumber}, skipping daily poll");
            }
            else
            {
                await TryAnnounceDayAsync(dayNumber);
            }

            nextPollDay = nextPollDay.AddDays(1);
        }
    }
}