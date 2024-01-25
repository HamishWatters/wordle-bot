namespace WordleBot;

public interface IMessageProvider
{
    IAsyncEnumerable<string> GetWinnerMessageEnumerator(int limit, int? year = null);
}