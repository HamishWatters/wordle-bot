namespace WordleBot.Answer;

public interface IAnswerProvider
{
    public Task<string?> GetAsync(int day);
}