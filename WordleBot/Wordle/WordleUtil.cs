using System.Globalization;

namespace WordleBot.Wordle;

public static class WordleUtil
{
    public static readonly DateOnly DayOne =
        DateOnly.ParseExact("2021-06-19", "yyyy-MM-dd", CultureInfo.InvariantCulture);
}