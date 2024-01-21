namespace WordleBot.Bot;

public interface IDisplayNameProvider
{
    Task<string> GetAsync(ulong userId);
}