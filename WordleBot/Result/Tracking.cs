using System.Text;
using System.Text.RegularExpressions;
using WordleBot.Wordle;

namespace WordleBot.Result;

public class Tracking(IDictionary<string, IList<string>> userNames)
{
    private static readonly Regex RunnerUpRegex = new(
        @"^(\\d+) - (.+): (\\d+) points$"
    );
    
    private readonly IDictionary<string, string> _displayNameToUserName = FlattenUserNames(userNames);

    private readonly Dictionary<string, TrackingUser> _users = new();
    private readonly HashSet<int> _daysPassed = [];

    private static Dictionary<string, string> FlattenUserNames(IDictionary<string, IList<string>>? input)
    {
        var ret = new Dictionary<string, string>();
        if (input == null)
        {
            return ret;
        }
        
        foreach (var keyValue in input)
        {
            foreach (var name in keyValue.Value)
            {
                if (ret.ContainsKey(name))
                {
                    throw new Exception("Duplicate names in user name configuration");
                }

                ret[name] = keyValue.Key;
            }
        }

        return ret;
    }

    public void Feed(string message)
    {
        var winnerMatch = WordleProcessor.WinnerRegex.Match(message);
        if (!winnerMatch.Success)
        {
            return;
        }
        
        if (!_daysPassed.Add(int.Parse(winnerMatch.Groups[1].Value)))
        {
            return;
        }

        var winnerName = GetTrackingName(winnerMatch.Groups[2].Value);
        var winnerTracking = GetTrackingUser(winnerName);
        winnerTracking.ProcessDay(1, long.Parse(winnerMatch.Groups[4].Value));

        var lines = message.Split("\n");
        var place = 2;
        for (var i = 1; i < lines.Length; i++)
        {
            var match = RunnerUpRegex.Match(lines[i]);
            if (!match.Success)
            {
                continue;
            }

            var name = GetTrackingName(match.Groups[2].Value);
            var userTracking = GetTrackingUser(name);
            userTracking.ProcessDay(place++, long.Parse(match.Groups[3].Value));
        }
    }

    private TrackingUser GetTrackingUser(string name)
    {
        if (_users.TryGetValue(name, out var value)) return value;
        
        value = new TrackingUser(name);
        _users[name] = value;

        return value;
    }

    private string GetTrackingName(string name)
    {
        return _displayNameToUserName.TryGetValue(name, out var display) ? display : name;
    }

    public string GetOutput()
    {
        var b = new StringBuilder();
        b.Append($"Roundup for {DateTime.Now.Year} so far\n");
        b.Append($"Completed the Wordle on {_daysPassed.Count} days\n");
        Console.WriteLine($"Processed {_daysPassed.Count}");
        var items = _users.Values.ToList();
        items.Sort((p, q) => q.Wins.CompareTo(p.Wins));
        foreach (var tu in items)
        {
            var winPercentage = tu.Wins * 100 / tu.Attempts;
            b.Append($"{tu.Name} won {tu.Wins} out of {tu.Attempts}, which is {winPercentage}% of their attempts. Their average score was {tu.AverageScore}\n");
        }

        return b.ToString();
    }
}