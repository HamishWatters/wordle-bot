using Discord;
using Discord.WebSocket;
using Serilog;
using WordleBot.Answer;
using WordleBot.Commands;
using WordleBot.Config;
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
    private readonly CommandConfig _commandConfig;
    private readonly IDictionary<string, IList<string>> _userNames;
    
    private readonly DiscordSocketClient _discordClient;
    private readonly BotResults _results;
    private readonly CommandParser _commandParser;

    private readonly AnswerProvider _answerProvider;
    private readonly PreviousAnswerTracking _previousAnswerTracking = new();

    private DateTime _nextAllowedRoundup = DateTime.Now;

    public DiscordBot(Config.Config config, ILogger log)
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
        _commandConfig = config.Command;
        _userNames = config.UserNames;
        
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

            await ReadyHandlerChannel(guild, _winnerChannelId, "winner", n * 3, HandleWinnerChannelMessageAsync);
            await ReadyHandlerChannel(guild, _wordleChannelId, "wordle", 1000, HandleWordleChannelMessageAsync);
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
            return HandleWinnerChannelMessageAsync(message, true);
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

    private Task HandleWordleChannelMessageAsync(IMessage message, bool live)
    {
        if (message.Author.Id == _botId)
        {
            return Task.CompletedTask;
        }
        
        var maybeCommand = _commandParser.Parse(message.Content, message.Timestamp);
        if (maybeCommand != null)
        {
            if (live)
            {
                return ProcessCommand(message.Author.Id, maybeCommand);
            }
        }
        else
        {
            var response = _results.ReceiveWordleMessage(message.Author.Id, message.Timestamp, message.Content);
            return HandleMessageResultAsync(response, message.Author.Id, live);
        }

        return Task.CompletedTask;
    }
    
    #region CommandHandlers
    private Task ProcessCommand(ulong id, Command command)
    {
        switch (command.Type)
        {
            case CommandType.List:
                return ProcessList(command.Day!.Value);
            
            case CommandType.End:
                return ProcessEnd(id, command.Day!.Value);
            
            case CommandType.RoundUp:
                return ProcessRoundup();
            
            case CommandType.Find:
                return ProcessFind(command.Word!);
            
            case CommandType.Help:
                return ProcessHelp();
            
            case CommandType.Unknown:
            default:
                _log.Warning("Unknown command");
                return SendMessageAsync(_wordleChannelId, _messageConfig.CommandUnknown);
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

    private async Task ProcessRoundup()
    {
        var now = DateTime.Now;
        if (now.CompareTo(_nextAllowedRoundup) < 0)
        {
            await SendMessageAsync(_wordleChannelId, _messageConfig.RoundupEarly);
            return;
        }

        await CheckYearAsync();
    }
    
    private async Task CheckYearAsync()
    {
        _nextAllowedRoundup = DateTime.Now.AddMinutes(5);
        
        var guild = _discordClient.GetGuild(_guildId);
        if (guild == null)
        {
            throw new Exception("No guild found");
        }
        
        var channel = guild.GetTextChannel(_winnerChannelId);
        if (channel == null)
        {
            throw new Exception("No channel found");
        }

        var tracking = new Tracking(_userNames);
        
        var currentYear = DateTime.Now.Year;
        await foreach (var page in channel.GetMessagesAsync(1000))
        {
            foreach (var message in page)
            {
                if (message.Timestamp.Year != currentYear)
                {
                    goto LoopEnd;
                }

                if (message.Author.Id == _botId)
                {
                    tracking.Feed(message.Content);
                }
            }
        }
        LoopEnd:
        var reply = tracking.GetOutput();
        await SendMessageAsync(_wordleChannelId, reply);
    }

    private Task ProcessFind(string word)
    {
        var upper = word.ToUpper();
        return SendMessageAsync(_wordleChannelId, _previousAnswerTracking.PreviousAnswers.TryGetValue(upper, out var date) ? $"{upper} was the answer on {date}" : $"{upper} has not been the answer before");
    }

    private Task ProcessHelp()
    {
        var response = string.Format(_messageConfig.Help, _commandConfig.List, _commandConfig.End,
            _commandConfig.RoundUp, _commandConfig.Seen);
        return SendMessageAsync(_wordleChannelId, response);
    }
    #endregion

    

    private async Task<Dictionary<ulong, string>> GetDisplayNameMap(IEnumerable<ulong> ids)
    {
        var ret = new Dictionary<ulong, string>();
        var tasks = ids.Select(async id => ret[id] = await ResolveName(id));
        await Task.WhenAll(tasks);
        return ret;
    }

    private Task HandleWinnerChannelMessageAsync(IMessage message, bool live)
    {
        if (message.Author.Id != _botId)
        {
            // Only process announcements that the bot sent
            return Task.CompletedTask;
        }

        var response = _results.ReceiveWinnerMessage(message.Content);
        _previousAnswerTracking.Feed(message.Content);
        return HandleMessageResultAsync(response, message.Author.Id, live);
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
            _log.Information($"Announcing result for {dayNumber}");
            await SendMessageAsync(_winnerChannelId,
                day.GetWinMessage(_messageConfig.WinnerFormat, _messageConfig.TodaysAnswerFormat,
                    _messageConfig.RunnersUpFormat, names, answer));
        }
        else
        {
            _log.Debug($"Not sending result for {dayNumber} because it's announced");
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