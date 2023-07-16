using FluentAssertions;
using WordleBot.Wordle;

namespace WordleBotTests.Wordle;

public class WordleProcessorTest
{
    #region Validation
    [Fact]
    public void Validate_Optimistic()
    {
        var input =
            "Wordle 734 4/6\n\n" +

            "⬜🟨⬜⬜⬜\n" +
            "⬜🟩🟨🟩⬜\n" +
            "⬜🟨⬜⬜⬜\n" +
            "🟩🟩🟩🟩🟩";


        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.Success, 734, 4));
    }

    [Fact]
    public void Validate_Loss()
    {
        var input =
            "Wordle 745 X/6\n\n" +

            "⬛🟩🟩⬛🟩\n" +
            "⬛🟩🟩⬛🟩\n" +
            "⬛🟩🟩⬛🟩\n" +
            "⬛🟩🟩⬛🟩\n" +
            "🟨🟩🟩⬛🟩\n" +
            "⬛🟩🟩🟩🟩";

        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.Success, 745, 10));
        
    }

    [Fact]
    public void Validate_BadRegex()
    {
        var input =
            "Wordle 734 4/6\n\n" +

            "NotAGoodResult\n";


        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.RegexMismatch));
    }

    [Fact]
    public void Validate_BadLineLength()
    {
        var input =
            "Wordle 734 4/6\n\n" +

            "⬜🟨⬜⬜⬜⬜\n" +
            "⬜🟨⬜⬜⬜\n" +
            "⬜🟩🟨🟩⬜\n" +
            "🟩🟩🟩🟩🟩";


        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidLineLength));
    }

    [Fact]
    public void Validate_BadAttemptsTooFew()
    {
        var input =
"Wordle 734 4/6\n\n" +

        "⬜🟨⬜⬜⬜\n" + 
        "⬜🟨⬜⬜⬜\n" +
        "🟩🟩🟩🟩🟩";


        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidAttempts));
    }
    
    [Fact]
    public void Validate_BadLineLengthTooMany()
    {
        var input =
            "Wordle 734 4/6\n\n" +

            "⬜🟨⬜⬜⬜\n" +
            "⬜🟨⬜⬜⬜\n" +
            "⬜🟩🟨⬜⬜\n" +
            "⬜🟩🟨🟩⬜\n" +
            "🟩🟩🟩🟩🟩";


        var result = WordleProcessor.Validate(input);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidAttempts));
    }

    [Fact]
    public void IsAnnouncement_Optimistic()
    {
        var input = "Wordle 681 winner is Jonathan! Who scored 2/6 (97).\n" +
                    "Today's answer was RANGE\n" +
                    "2 - Zefiren: 84 points\n" +
                    "3 - Hamish: 73 points\n" +
                    "4 - Valiant: 45 points";
        var result = WordleProcessor.IsAnnouncement(input);
        result.Should().BeEquivalentTo(WordleAnnouncementResult.Success(681));
    }
    #endregion
    
    #region Scoring

    [Fact]
    public void Score_AttemptsLightMode()
    {
        var input =
            "Wordle 753 4/6\n\n" +

            "⬜⬜🟩⬜⬜\n" +
            "⬜🟨🟩⬜⬜\n" +
            "⬜🟩🟩🟨⬜\n" +
            "🟩🟩🟩🟩🟩";

        WordleProcessor.Score(WordleValidateResult.Success(753, 4), input)
            .Should().Be(55);
    }
    
    [Fact]
    public void Score_AttemptsDarkMode()
    {
        var input =
            "Wordle 753 5/6\n\n" +

            "🟨⬛⬛⬛⬛\n" +
            "⬛🟨⬛🟨⬛\n" +
            "⬛🟨🟨⬛🟨\n" +
            "🟩🟩🟩🟩⬛\n" +
            "🟩🟩🟩🟩🟩";

        WordleProcessor.Score(WordleValidateResult.Success(753, 5), input)
            .Should().Be(47);

        input =
            "Wordle 753 5/6\n\n" +

            "⬛🟨⬛⬛⬛\n" +
            "🟨⬛⬛⬛🟨\n" +
            "⬛🟨⬛⬛🟨\n" +
            "⬛🟩🟩🟩⬛\n" +
            "🟩🟩🟩🟩🟩";

        WordleProcessor.Score(WordleValidateResult.Success(753, 5), input)
            .Should().Be(39);

        input =
            "Wordle 753 3/6\n\n" +

            "⬛🟨⬛⬛⬛\n" +
            "⬛🟩⬛🟩⬛\n" +
            "🟩🟩🟩🟩🟩";

        WordleProcessor.Score(WordleValidateResult.Success(753, 3), input)
            .Should().Be(69);
    }
    
    
    [Fact]
    public void Score_AttemptsHighContrastMode()
    {
        var input =
            "Wordle 684 5/6\n\n" +

            "⬛⬛⬛⬛⬛\n" +
            "⬛🟦⬛🟦⬛\n" +
            "⬛🟧🟧⬛🟧\n" +
            "⬛🟧🟧🟧🟧\n" +
            "🟧🟧🟧🟧🟧";

        WordleProcessor.Score(WordleValidateResult.Success(684, 5), input)
            .Should().Be(42);
        input =
            "Wordle 684 5/6\n\n" +

            "⬛⬛⬛⬛⬛\n" +
            "⬛🟨⬛🟨⬛\n" +
            "⬛🟩🟩⬛🟩\n" +
            "⬛🟩🟩🟩🟩\n" +
            "🟩🟩🟩🟩🟩";

        WordleProcessor.Score(WordleValidateResult.Success(684, 5), input)
            .Should().Be(42);
    }

    [Fact]
    public void Score_UnexpectedUnicode_ThrowsException()
    {
        // real workflow should filter out this input because it doesn't match the regex
        var input =
            "Wordle 684 5/6\n\n" +

            "⬛⬛⬛⬛⬛\n" +
            "⬛🟨⬛🟨⬛\n" +
            "⬛🟥🟥⬛🟥\n" +
            "⬛🟥🟥🟥🟥\n" +
            "🟥🟥🟥🟥🟥";

        Action getScore = () => WordleProcessor.Score(WordleValidateResult.Success(684, 5), input);
        getScore.Should().Throw<Exception>()
            .WithMessage("Don't understand UTF32 char: 229 247 1 0");
    }

    #endregion
}