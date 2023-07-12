using FluentAssertions;
using WordleBot.Wordle;

namespace WordleBotTests;

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

    #endregion
}