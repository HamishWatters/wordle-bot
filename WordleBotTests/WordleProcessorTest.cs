using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using WordleBot.Wordle;
using Xunit.Abstractions;

namespace WordleBotTests;

public class WordleProcessorTest
{
    private readonly ITestOutputHelper _testOutputHelper;

    public WordleProcessorTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

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
}