namespace WordleBot.Result;

public class Day
{
    public Dictionary<ulong, User> Results { get; } = new();
    public bool Announced { get; set; }

    public List<KeyValuePair<ulong, User>> GetSortedList()
    {
        var results = Results.ToList();
        results.Sort((a, b) =>
        {
            var left = a.Value;
            var right = b.Value;
            var comparison = left.Attempts.CompareTo(right.Attempts); // compare left to right here as a smaller attempts is better
            if (comparison != 0) return comparison;

            comparison = right.Score.CompareTo(left.Score);
            if (comparison != 0) return comparison;

            return left.Timestamp.CompareTo(right.Timestamp);
        });
        return results;
    }
}