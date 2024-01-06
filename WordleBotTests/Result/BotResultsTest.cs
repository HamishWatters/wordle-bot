using System.Globalization;
using FluentAssertions;
using WordleBot;
using WordleBot.Config;
using WordleBot.Result;

namespace WordleBotTests.Result;

public class BotResultsTest
{
    private const string IsoFormat = "yyyy-MM-ddThh:mm:ssZ";

    private readonly BotResults _botResults;
    private readonly DateTimeOffset _baseDate = DateTimeOffset.Now;

    public BotResultsTest()
    {
        _botResults = new BotResults(_ => false);
    }
    
    [Fact]
    public void ReceiveMessage_ValidFirst_Optimistic()
    {
        _botResults.ReceiveWordleMessage(265253239295967232, _baseDate,
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
        _botResults.Results[755].Results.ContainsKey(265253239295967232).Should().BeTrue();
        var userResult = _botResults.Results[755].Results[265253239295967232];
        userResult.Attempts.Should().Be(4);
        userResult.Timestamp.Should().Be(_baseDate);
        userResult.Score.Should().Be(59);
    }

    [Fact]
    public void ReceiveMessage_RandomText_Optimistic()
    {
        _botResults.ReceiveWordleMessage(265253239295967232, _baseDate, "Hello World")
            .Should()
            .BeEquivalentTo(new MessageResult(MessageResultType.Continue));
    }

    [Fact]
    public void ReceiveMessage_Failure_Optimistic()
    {
        _botResults.ReceiveWordleMessage(244231303904362497, _baseDate,
                "Wordle 763 X/6\n\n" +

                "🟨🟨⬛⬛🟩\n" +
                "⬛🟩🟩⬛🟩\n" +
                "⬛🟩🟩⬛🟩\n" +
                "⬛🟩🟩⬛🟩\n" +
                "⬛🟩🟩⬛🟩\n" +
                "⬛🟩🟩⬛🟩"
            )
            .Should()
            .BeEquivalentTo(new MessageResult(MessageResultType.Continue, 763));

        _botResults.Results.Count.Should().Be(1);
        _botResults.Results.ContainsKey(763).Should().BeTrue();
        _botResults.Results[763].Results.Count.Should().Be(1);
        _botResults.Results[763].Results.ContainsKey(244231303904362497).Should().BeTrue();
        var userResult = _botResults.Results[763].Results[244231303904362497];
        userResult.Attempts.Should().Be(10);
        userResult.Timestamp.Should().Be(_baseDate);
        userResult.Score.Should().Be(36);
    }

    [Fact]
    public void Integration_FullDay755_Optimistic()
    {
        var botResults = new BotResults(day => day.Results.ContainsKey(248825525441658880) &&
                                               day.Results.ContainsKey(244231303904362497) &&
                                               day.Results.ContainsKey(265253239295967232) &&
                                               day.Results.ContainsKey(288328937577119746));

        botResults.ReceiveWordleMessage(248825525441658880,
                DateTimeOffset.ParseExact("2023-07-14T00:03:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 3/6\n\n" +

                "⬛⬛⬛🟩🟨\n" +
                "⬛⬛🟩🟩⬛\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage(265253239295967232,
                DateTimeOffset.ParseExact("2023-07-14T00:39:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 4/6\n\n" +

                "⬜🟨⬜🟨⬜\n" +
                "⬜🟩🟨🟨⬜\n" +
                "🟩🟩🟩⬜🟩\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage(244231303904362497,
                DateTimeOffset.ParseExact("2023-07-14T01:22:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 3/6\n\n" +

                "⬛⬛⬛⬛🟨\n" +
                "⬛⬛🟩⬛⬛\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Continue, 755));

        botResults.ReceiveWordleMessage(288328937577119746,
                DateTimeOffset.ParseExact("2023-07-14T06:37:00Z", IsoFormat, CultureInfo.InvariantCulture),
                "Wordle 755 4/6\n\n" +

                "⬛⬛⬛⬛🟨\n" +
                "⬛🟨⬛⬛⬛\n" +
                "⬛🟩🟨🟨🟩\n" +
                "🟩🟩🟩🟩🟩")
            .Should().BeEquivalentTo(new MessageResult(MessageResultType.Winner, 755));

        botResults.Results.ContainsKey(755).Should().BeTrue();

        var messageConfig = new MessageConfig();
        botResults.Results[755].GetWinMessage(messageConfig.WinnerFormat, messageConfig.TodaysAnswerFormat, messageConfig.RunnersUpFormat, GetNames())
            .Should().Be(
                "Wordle 755 winner is honeystain! Who scored 3/6 (71).\n" +
                "2 - zefiren: 67 points\n" +
                "3 - hamish.w: 64 points\n" +
                "4 - valiantstar: 59 points"
            );
    }
    

    private static Dictionary<ulong, string> GetNames()
    {
        var map = new Dictionary<ulong, string>();
        map[265253239295967232] = "hamish.w";
        map[248825525441658880] = "honeystain";
        map[244231303904362497] = "zefiren";
        map[288328937577119746] = "valiantstar";
        return map;
    }
}