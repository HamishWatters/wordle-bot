using FluentAssertions;
using WordleBot.Result;

namespace WordleBotTests.Result;

public class BotResultsTest
{
    private readonly BotResults _botResults = new();
    private readonly DateTimeOffset _baseDate = DateTimeOffset.Now;

    [Fact]
    public void ReceiveMessage_ValidFirst_Optimistic()
    {
        _botResults.ReceiveWordleMessage("hamish", _baseDate,
            "Wordle 755 4/6\n\n" +

            "⬛⬛⬛⬛🟨\n" +
            "⬛🟨⬛⬛⬛\n" +
            "⬛🟩🟨🟨🟩\n" +
            "🟩🟩🟩🟩🟩"
        )
            .Should()
            .BeEquivalentTo(new MessageResult(MessageResultType.NewSubmission, 755));

        _botResults.Results.Count.Should().Be(1);
        _botResults.Results.ContainsKey(755).Should().BeTrue();
        _botResults.Results[755].Results.Count.Should().Be(1);
        _botResults.Results[755].Results.ContainsKey("hamish").Should().BeTrue();
        var userResult = _botResults.Results[755].Results["hamish"];
        userResult.Attempts.Should().Be(4);
        userResult.Timestamp.Should().Be(_baseDate);
        userResult.Score.Should().Be(59);
    }

    [Fact]
    public void ReceiveMessage_RandomText_Optimistic()
    {
        _botResults.ReceiveWordleMessage("hamish", _baseDate, "Hello World")
            .Should()
            .BeEquivalentTo(new MessageResult(MessageResultType.Continue));
    }
}