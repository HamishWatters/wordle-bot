using System.Text;
using Serilog;
using WordleBot.Commands;
using WordleBot.Config;
using WordleBot.Result;
using WordleBot.Wordle;

namespace WordleBot.Bot.Commands;

public class CommandService(ILogger log, CommandConfig config, MessageConfig messageConfig, 
    ICollection<ulong> adminIds, IDictionary<string, IList<string>> userNames,
    IWordleService wordleService, IDisplayNameProvider displayNameProvider, IMessageProvider messageProvider)
    : ICommandService
{
    private DateTime _nextAllowedRoundup = DateTime.Now;
    
    public bool TryParseCommand(string content, DateTimeOffset timestamp, out Command command)
    {
        var loweredContent = content.ToLowerInvariant();
        if (!loweredContent.StartsWith(config.Prefix))
        {
            command = Command.Unknown();
            return false;
        }

        var commandContent = loweredContent[config.Prefix.Length..].TrimStart();
        if (commandContent.StartsWith(config.List))
        {
            command = ParseList(commandContent[config.List.Length..].TrimStart(), timestamp);
            return true;
        }

        if (commandContent.StartsWith(config.End))
        {
            command = ParseEnd(commandContent[config.End.Length..].TrimStart());
            return true;
        }

        if (commandContent.StartsWith(config.RoundUp))
        {
            command = Command.RoundUp();
            return true;
        }

        if (commandContent.StartsWith(config.Find))
        {
            command = ParseFind(commandContent[config.Find.Length..].TrimStart());
            return true;
        }

        if (commandContent.StartsWith(config.Help))
        {
            command = Command.Help();
            return true;
        }

        command = Command.Unknown();
        return false;
    }

    private static Command ParseList(string input, DateTimeOffset timestamp)
    {
        var split = input.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (split.Length > 0)
        {
            var dayString = split[0];
            if (!string.IsNullOrWhiteSpace(dayString))
            {
                return int.TryParse(dayString, out var days) ? Command.List(days) : Command.Unknown();
            }
        }

        var date = DateOnly.FromDateTime(timestamp.LocalDateTime);
        var dayNumber = date.DayNumber - WordleUtil.DayOne.DayNumber;
        return Command.List(dayNumber);
    }

    private static Command ParseEnd(string input)
    {
        var dayString = input.Split(' ')[0];
        return int.TryParse(dayString, out var days) ? Command.End(days) : Command.Unknown();
    }

    private static Command ParseFind(string input)
    {
        return input.Length switch
        {
            5 => Command.Find(input, false),
            9 when input.StartsWith("||") && input.EndsWith("||") => Command.Find(input.Substring(2, 5), true),
            _ => Command.Unknown()
        };
    }

    public Task<MessageResult> ProcessCommand(ulong userId, Command command)
    {
        switch (command.Type)
        {
            case CommandType.List:
                return ProcessListAsync(command.Day!.Value);
            
            case CommandType.End:
                return ProcessEnd(userId, command.Day!.Value);
            
            case CommandType.RoundUp:
                return ProcessRoundupAsync();
            
            case CommandType.Find:
                return ProcessFind(command);
            
            case CommandType.Help:
                return ProcessHelp();
            
            case CommandType.Unknown:
            default:
                log.Warning("Unknown command");
                return Task.FromResult(new MessageResult(MessageResultType.ForWordle, messageConfig.CommandUnknown));
        }
    }

    #region List
    private Task<MessageResult> ProcessListAsync(int day)
    {
        return wordleService.TryGetResult(day, true, out var dayResult) 
            ? BuildDayMessageAsync(day, dayResult) 
            : Task.FromResult(new MessageResult(MessageResultType.ForWordle, string.Format(messageConfig.CommandUnknownDay, day)));
    }

    private async Task<MessageResult> BuildDayMessageAsync(int day, Day result)
    {
        var resultList = result.GetSortedList();
        var builder = new StringBuilder($"Wordle {day}");
        for (var i = 0; i < resultList.Count; i++)
        {
            builder.Append('\n');
            var userResult = resultList[i];
            var name = await displayNameProvider.GetAsync(userResult.Key);
            builder.Append(string.Format(messageConfig.RunnersUpFormat, i + 1, name, userResult.Value.Score));
        }

        return new MessageResult(MessageResultType.ForWordle, builder.ToString());
    }
    #endregion

    private Task<MessageResult> ProcessEnd(ulong id, int commandDay)
    {
        if (!adminIds.Contains(id))
        {
            var displayName = displayNameProvider.GetAsync(id);
            return Task.FromResult(new MessageResult(MessageResultType.ForWordle,
                string.Format(messageConfig.CommandNotAdmin, displayName)));
        }

        if (!wordleService.TryGetResult(commandDay, true, out _))
        {
            return Task.FromResult(new MessageResult(MessageResultType.ForWordle,
                string.Format(messageConfig.CommandUnknownDay, commandDay)));
        }

        return wordleService.GetAnnouncementAsync(commandDay);
    }

    #region Roundup
    private async Task<MessageResult> ProcessRoundupAsync()
    {
        var now = DateTime.Now;
        if (now.CompareTo(_nextAllowedRoundup) < 0)
        {
            return new MessageResult(MessageResultType.ForWordle, messageConfig.RoundupEarly);
        }

        _nextAllowedRoundup = DateTime.Now.AddMinutes(5);

        var messages = messageProvider.GetWinnerMessageEnumerator(1000, DateTime.Now.Year);
        var tracking = new Tracking(userNames);
        
        await foreach (var message in messages)
        {
            tracking.Feed(message);
        }
        
        return new MessageResult(MessageResultType.ForWordle, tracking.GetOutput());
    }
    #endregion

    #region Find
    private Task<MessageResult> ProcessFind(Command command)
    {
        var upper = command.Word!.ToUpper();
        string sendValue;
        if (command.Spoiler != null && command.Spoiler!.Value)
        {
            sendValue = $"||{upper}||";
        }
        else
        {
            sendValue = upper;
        }

        var date = wordleService.GetDateForAnswer(upper);
        return Task.FromResult(new MessageResult(MessageResultType.ForWordle,
            date != null ? $"{sendValue} was the answer on {date}" : $"{sendValue} has not been the answer before"));
    }
    #endregion
    
    #region Help
    private Task<MessageResult> ProcessHelp()
    {
        var response = string.Format(messageConfig.Help, config.List, config.End,
            config.RoundUp, config.Find);

        return Task.FromResult(new MessageResult(MessageResultType.ForWordle, response));
    }
    #endregion
}