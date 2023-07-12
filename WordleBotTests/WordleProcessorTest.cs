using FluentAssertions;
using WordleBot.Wordle;

namespace WordleBotTests;

public class WordleProcessorTest
{
    [Fact]
    public void Validate_Optimistic()
    {
        var sample = """
Wordle 734 4/6

⬜🟨⬜⬜⬜
⬜🟨⬜⬜⬜
⬜🟩🟨🟩⬜
🟩🟩🟩🟩🟩
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.Success, 734, 4));
    }

    [Fact]
    public void Validate_BadRegex()
    {
        var sample = """
Wordle 734 4/6

NotAGoodResult
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.RegexMismatch));
    }

    [Fact]
    public void Validate_BadLineLength()
    {
        var sample = """
Wordle 734 4/6

⬜🟨⬜⬜⬜⬜
⬜🟨⬜⬜⬜
⬜🟩🟨🟩⬜
🟩🟩🟩🟩🟩
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidLineLength));
    }

    [Fact]
    public void Validate_BadAttemptsTooFew()
    {
        var sample = """
Wordle 734 4/6

⬜🟨⬜⬜⬜
⬜🟨⬜⬜⬜
🟩🟩🟩🟩🟩
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidAttempts));
    }
    
    [Fact]
    public void Validate_BadLineLengthTooMany()
    {
        var sample = """
Wordle 734 4/6

⬜🟨⬜⬜⬜
⬜🟨⬜⬜⬜
⬜🟩🟨⬜⬜
⬜🟩🟨🟩⬜
🟩🟩🟩🟩🟩
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().BeEquivalentTo(new WordleValidateResult(WordleValidateResultType.InvalidAttempts));
    }
}