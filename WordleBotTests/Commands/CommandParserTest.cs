using FluentAssertions;
using WordleBot;
using WordleBot.Commands;

namespace WordleBotTests.Commands;

public class CommandParserTest
{
    private readonly CommandParser _commandParser = new(new CommandConfig());
    
    [Fact]
    public void Parse_List_Optimistic()
    {
        _commandParser.Parse("wordle-bot ls 532")
            .Should()
            .BeEquivalentTo(Command.List(532));
    }
    
    [Fact]
    public void Parse_List_MissingDay()
    {
        _commandParser.Parse("wordle-bot ls")
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_List_NanDay()
    {
        _commandParser.Parse("wordle-bot ls five")
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }
    
    [Fact]
    public void Parse_End_Optimistic()
    {
        _commandParser.Parse("wordle-bot end 123")
            .Should()
            .BeEquivalentTo(Command.End(123));
    }
    
    [Fact]
    public void Parse_End_MissingDay()
    {
        _commandParser.Parse("wordle-bot end")
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_End_NanDay()
    {
        _commandParser.Parse("wordle-bot end five")
            .Should()
            .BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_NoPrefix()
    {
        _commandParser.Parse("what-bot ls 10")
            .Should()
            .BeNull();
    }
}