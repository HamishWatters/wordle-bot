using FluentAssertions;
using WordleBot;
using WordleBot.Commands;
using WordleBot.Config;

namespace WordleBotTests.Commands;

public class CommandParserTest
{
    private readonly CommandParser _commandParser = new(new CommandConfig());
    
    [Fact]
    public void Parse_List_Optimistic()
    {
        _commandParser.Parse("wordle-bot ls 532", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.List(532));
    }
    
    [Fact]
    public void Parse_List_MissingDay()
    {
        _commandParser.Parse("wordle-bot ls", DateTimeOffset.Parse("2023-12-20T11:00:00Z"))
            .Should()
            .BeEquivalentTo(Command.List(914));
    }

    [Fact]
    public void Parse_List_NanDay()
    {
        _commandParser.Parse("wordle-bot ls five", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_List_NoDay()
    {
        var dateTimeOffset = DateTimeOffset.Parse("2023-12-13T09:37:25Z");
        _commandParser.Parse("wordle-bot ls", dateTimeOffset)
            .Should()
            .BeEquivalentTo(Command.List(907));
    }
    
    [Fact]
    public void Parse_End_Optimistic()
    {
        _commandParser.Parse("wordle-bot end 123", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.End(123));
    }
    
    [Fact]
    public void Parse_End_MissingDay()
    {
        _commandParser.Parse("wordle-bot end", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_End_NanDay()
    {
        _commandParser.Parse("wordle-bot end five", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_NoPrefix()
    {
        _commandParser.Parse("what-bot ls 10", DateTimeOffset.Now)
            .Should()
            .BeNull();
    }

    [Fact]
    public void Parse_Seen()
    {
        _commandParser.Parse("wordle-bot find crane", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.Seen("crane"));
    }

    [Fact]
    public void Parse_Help()
    {
        _commandParser.Parse("wordle-bot help", DateTimeOffset.Now)
            .Should()
            .BeEquivalentTo(Command.Help());
    }
}