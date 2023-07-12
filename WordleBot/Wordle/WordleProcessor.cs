using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace WordleBot.Wordle;

public class WordleProcessor
{
    // Allow between 5 and 10 characters because the coloured squares all use two UTF-16 characters
    private static readonly Regex WordleRegex = new(
        "^Wordle\\s(\\d+)\\s([1-6X])/6\n{2}(([⬜⬛🟨🟩🟦🟧]){5,10}\n){0,5}[⬜⬛🟨🟩🟦🟧]{5,10}$"
        );
    
    public static WordleValidateResult Validate(string input)
    {
        var match = WordleRegex.Match(input);
        if (!match.Success)
        {
            return WordleValidateResult.Failure(WordleValidateResultType.RegexMismatch);
        }

        var lines = input.Split('\n');
        for (var i = 2; i < lines.Length; i++)
        {
            var utf32 = Encoding.UTF32.GetBytes(lines[i]);
            if (utf32.Length != 20)
            {
                return WordleValidateResult.Failure(WordleValidateResultType.InvalidLineLength);
            }
        }

        var daysString = match.Groups[1].Value;
        if (!int.TryParse(daysString, out var days))
        {
            // regex should have already refused this
            throw new DataException($"Wordle day was not a number: {daysString}");
        }

        var attemptsString = match.Groups[2].Value;
        if (!int.TryParse(attemptsString, out var attempts))
        {
            // regex should have already refused this
            throw new DataException($"Wordle attempts was not a number: {attemptsString}");
        }
        
        return WordleValidateResult.Success(days, attempts);
    }
        
}