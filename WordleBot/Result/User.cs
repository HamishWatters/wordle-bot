namespace WordleBot.Result;

public record User(DateTimeOffset Timestamp, int Attempts, int Score);