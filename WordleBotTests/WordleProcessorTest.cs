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
        result.Should().Be(WordleValidateResult.Success);
    }

    [Fact]
    public void Validate_BadRegex()
    {
        var sample = """
Wordle 734 4/6

NotAGoodResult
""";


        var result = WordleProcessor.Validate(sample);
        result.Should().Be(WordleValidateResult.RegexMismatch);
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
        result.Should().Be(WordleValidateResult.InvalidLineLength);
    }
}