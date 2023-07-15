using System.Globalization;
using System.Text.Json;

namespace WordleBot.Answer;

public class AnswerProvider
{
    private static readonly DateOnly DayOne =
        DateOnly.ParseExact("2021-06-19", "yyyy-MM-dd", CultureInfo.InvariantCulture);
    
    private readonly HttpClient _http = new();

    public async Task<string> GetAsync(int day)
    {
        var dayDate = DayOne.AddDays(day);
        var formatted = $"{dayDate.Year}-{dayDate.Month:D2}-{dayDate.Day:D2}";
        var url = $"https://www.nytimes.com/svc/wordle/v2/{formatted}.json";
        var response = await _http.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var answer = JsonSerializer.Deserialize<AnswerResponse>(json);
        if (answer == null)
        {
            throw new Exception($"Failed to parse JSON: {json}");
        }
        return answer.Solution;
    }
}