using System.Text.Json;
using Serilog;
using WordleBot.Wordle;

namespace WordleBot.Answer;

public class AnswerProvider(ILogger log): IAnswerProvider
{
    private readonly HttpClient _http = new();

    public async Task<string?> GetAsync(int day)
    {
        try
        {
            var dayDate = WordleUtil.DayOne.AddDays(day);
            var formatted = $"{dayDate.Year}-{dayDate.Month:D2}-{dayDate.Day:D2}";
            var url = $"https://www.nytimes.com/svc/wordle/v2/{formatted}.json";
            var response = await _http.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var answer = JsonSerializer.Deserialize<AnswerResponse>(json, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase});
            if (answer != null) return answer.Solution.ToUpper();
            
            log.Error($"Failed to parse JSON: {json}");
            return null;
        }
        catch (Exception e)
        {
            log.Error(e, $"Failed to get answer for day {day}");
            return null;
        }
    }
}