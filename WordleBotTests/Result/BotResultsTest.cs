using System.Globalization;
using FluentAssertions;
using WordleBot;
using WordleBot.Result;

namespace WordleBotTests.Result;

public class BotResultsTest
{
    private const string IsoFormat = "yyyy-MM-ddThh:mm:ssZ";
    
    private readonly BotResults _botResults = new(_ => false);
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
            .BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

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

    [Fact]
    public void Integration_FullDay755_Optimistic()
    {
        var botResults = new BotResults(day => day.Results.ContainsKey("honeystain") &&
                                               day.Results.ContainsKey("zefiren") &&
                                               day.Results.ContainsKey("hamish.w") &&
                                               day.Results.ContainsKey("valiantstar"));

        botResults.ReceiveWordleMessage("honeystain",
                DateTimeOffset.ParseExact("2023-07-14T00:03:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 3/6\n\n" +

                "⬛⬛⬛🟩🟨\n" +
                "⬛⬛🟩🟩⬛\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage("hamish.w",
                DateTimeOffset.ParseExact("2023-07-14T00:39:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 4/6\n\n" +

                "⬜🟨⬜🟨⬜\n" +
                "⬜🟩🟨🟨⬜\n" +
                "🟩🟩🟩⬜🟩\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage("zefiren",
                DateTimeOffset.ParseExact("2023-07-14T01:22:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 3/6\n\n" +

                "⬛⬛⬛⬛🟨\n" +
                "⬛⬛🟩⬛⬛\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage("valiantstar",
                DateTimeOffset.ParseExact("2023-07-14T06:37:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 4/6\n\n" +

                "⬛⬛⬛⬛🟨\n" +
                "⬛🟨⬛⬛⬛\n" +
                "⬛🟩🟨🟨🟩\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Winner, 755));

        botResults.Results.ContainsKey(755).Should().BeTrue();

        var messageConfig = new MessageConfig();
        botResults.Results[755].GetWinMessage(messageConfig.WinnerFormat, messageConfig.RunnersUpFormat)
            .Should().Be(
                "Wordle 755 winner is honeystain! Who scored 3/6 (71).\n" +
                "2 - zefiren: 67 points\n" +
                "3 - hamish.w: 64 points\n" +
                "4 - valiantstar: 59 points"
            );
    }
}