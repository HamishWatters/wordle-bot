namespace WordleBot.Result;

public class TrackingUser(string name)
{
    public string Name { get; } = name;
    public int Attempts { get; private set; }
    public int Wins => _results.GetValueOrDefault(1, 0);
    public long AverageScore => _totalScore / Attempts;

    private readonly Dictionary<int, int> _results = new();
    private long _totalScore;

    public void ProcessDay(int place, long score)
    {
        Attempts++;
        _results[place] = _results.GetValueOrDefault(place, 0) + 1;
        _totalScore += score;
    }
}