using FluentAssertions;
using WordleBot.Bot.Commands;
using WordleBot.Commands;
using WordleBot.Config;

namespace WordleBotTests.Bot.Commands;

public class CommandServiceTest
{
    private readonly CommandService _commandService = new(new CommandConfig());
    
    [Fact]
    public void Parse_List_Optimistic()
    {
        _commandService.TryParseCommand("wordle-bot ls 532", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.List(532));
    }
    
    [Fact]
    public void Parse_List_NoDay()
    {
        _commandService.TryParseCommand("wordle-bot ls", DateTimeOffset.Parse("2023-12-20T11:00:00Z"), out var command)
            .Should()
            .BeTrue();
        command.Should().BeEquivalentTo(Command.List(914));
    }

    [Fact]
    public void Parse_List_NanDay()
    {
        _commandService.TryParseCommand("wordle-bot ls five", DateTimeOffset.Now, out var command)
            .Should()
            .BeTrue();
        command.Should().BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_End_Optimistic()
    {
        _commandService.TryParseCommand("wordle-bot end 123", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.End(123));
    }
    
    [Fact]
    public void Parse_End_MissingDay()
    {
        _commandService.TryParseCommand("wordle-bot end", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.Unknown);
    }

    [Fact]
    public void Parse_End_NanDay()
    {
        _commandService.TryParseCommand("wordle-bot end five", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.Unknown());
    }

    [Fact]
    public void Parse_NoPrefix()
    {
        _commandService.TryParseCommand("what-bot ls 10", DateTimeOffset.Now, out _)
            .Should()
            .BeFalse();
    }

    [Fact]
    public void Parse_Find()
    {
        _commandService.TryParseCommand("wordle-bot find crane", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.Find("crane", false));
    }

    [Fact]
    public void Parse_Seen_Spoiler()
    {
        _commandService.TryParseCommand("wordle-bot find ||claim||", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
            command.Should().BeEquivalentTo(Command.Find("claim", true));
    }

    [Fact]
    public void Parse_Help()
    {
        _commandService.TryParseCommand("wordle-bot help", DateTimeOffset.Now, out var command)
            .Should().BeTrue();
        command.Should().BeEquivalentTo(Command.Help());
    }
}