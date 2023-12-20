using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace WordleBot.Wordle;

public static class WordleProcessor
{
    // Allow between 5 and 10 characters because the coloured squares all use two UTF-16 characters
    private static readonly Regex WordleRegex = new(
        @"^Wordle (\d+) ([1-6X])/6\n{2}(([⬜⬛🟨🟩🟦🟧]){5,10}\n){0,5}[⬜⬛🟨🟩🟦🟧]{5,10}$"
        );

    public static readonly Regex WinnerRegex = new(
        @"^Wordle (\d+) winner is (.+)! Who scored ([1-6X])/6 \((\d{1,2}|100)%?\)\.\nToday's answer was (\w{5})"
        );
    
    #region Validation
    public static WordleValidateResult Validate(string input)
    {
        var match = WordleRegex.Match(input);
        if (!match.Success)
        {
            return WordleValidateResult.Failure(WordleValidateResultType.RegexMismatch);
        }

        var daysString = match.Groups[1].Value;
        if (!int.TryParse(daysString, out var days))
        {
            // regex should have already refused this
            throw new DataException($"Wordle day was not a number: {daysString}");
        }

        var attemptsString = match.Groups[2].Value;
        int attemptScore;
        int attempts;
        if (attemptsString == "X")
        {
            attemptScore = 10;
            attempts = 6;
        }
        else if (int.TryParse(attemptsString, out attemptScore))
        {
            attempts = attemptScore;
        }
        else
        {
            // regex should have already refused this
            throw new DataException($"Wordle attempts was not a number: {attemptsString}");
        }

        var lines = input.Split('\n');
        if (lines.Length - 2 != attempts)
        {
            return WordleValidateResult.Failure(WordleValidateResultType.InvalidAttempts);
        }
        
        for (var i = 2; i < lines.Length; i++)
        {
            var utf32 = Encoding.UTF32.GetBytes(lines[i]);
            if (utf32.Length != 20)
            {
                return WordleValidateResult.Failure(WordleValidateResultType.InvalidLineLength);
            }
        }
        
        return WordleValidateResult.Success(days, attemptScore);
    }

    public static WordleAnnouncementResult IsAnnouncement(string input)
    {
        var match = WinnerRegex.Match(input);
        if (!match.Success)
        {
            return WordleAnnouncementResult.Failure(WordleAnnouncementResultType.RegexMismatch);
        }

        var daysString = match.Groups[1].Value;
        if (!int.TryParse(daysString, out var days))
        {
            // regex should have already refused this
            throw new DataException($"Wordle day was not a number: {daysString}");
        }

        return WordleAnnouncementResult.Success(days);
    }

    #endregion
    
    #region Scoring
    public static int Score(WordleValidateResult result, string input)
    {
        if (result.Type != WordleValidateResultType.Success)
        {
            throw new Exception("Cannot get score for an invalid input");
        }

        var lines = input.Split('\n');
        var attempts = Math.Min(result.Attempts!.Value, 6);
        var solution = new string[attempts];
        Array.Copy(lines, 2, solution, 0, attempts);
        return OriginalScoring(attempts, solution);
    }

    private static int OriginalScoring(int attempts, IReadOnlyList<string> solution)
    {
        var score = attempts switch
        {
            1 => 50,
            2 => 40,
            3 => 30,
            4 => 20,
            5 => 10,
            _ => 0
        };

        var knownGreens = new bool[5];
        var knownYellows = new bool[5];

        for (var i = 0; i < solution.Count; i++)
        {
            var line = solution[i];
            var utf32 = Encoding.UTF32.GetBytes(line);
            var newGreens = 0;
            var newYellows = 0;
            for (var j = 0; j < 5; j++)
            {
                var square = ParseSquare(utf32, j * 4);
                if (square == Square.Green && !knownGreens[j])
                {
                    newGreens++;
                    knownGreens[j] = true;
                }
                else if (square == Square.Yellow && !knownYellows[j])
                {
                    newYellows++;
                    knownYellows[j] = true;
                }
            }

            score += newGreens * Math.Max(1, 10 - i * 2);
            score += newYellows * (5 - i);
        }

        return score;
    }

    private enum Square
    {
        Grey,
        Yellow,
        Green
    }

    private static Square ParseSquare(IReadOnlyList<byte> utf32Bytes, int index)
    {
        if (utf32Bytes[index + 3] != 0x00)
        {
            // Regex should filter out unexpected characters
            throw new DataException("All expected unicode characters have 4th byte 0");
        }
        if ((utf32Bytes[index] == 0x1c || utf32Bytes[index] == 0x1b) && utf32Bytes[index + 1] == 0x2b && utf32Bytes[index + 2] == 0x00)
        {
            // Black or white squares
            return Square.Grey;
        }

        if (utf32Bytes[index + 1] == 0xf7 && utf32Bytes[index + 2] == 0x01)
        {
            // Green, yellow, orange and blue all have this 2nd and 3rd byte
            switch (utf32Bytes[index])
            {
                case 0xe9: return Square.Green;
                case 0xe7: return Square.Green;
                case 0xe8: return Square.Yellow;
                case 0xe6: return Square.Yellow;
            }
        }
        
        throw new Exception($"Don't understand UTF32 char: {utf32Bytes[index]} {utf32Bytes[index + 1]} {utf32Bytes[index + 2]} {utf32Bytes[index + 3]}");
    }
    #endregion
}