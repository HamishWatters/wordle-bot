namespace WordleBot;

public class Config
{
    private const ulong DefaultGuild = 326451862582722561;
    private const ulong DefaultWordleChannel = 945629466800029736;
    private const ulong DefaultWinnerChannel = 992503715820994651;
    
    public string ApiToken { get; set; } = ""; // discord API token
    public bool TestMode { get; set; } = true; // if true we log messages for discord, false means we actually send them
    public IList<string> RequiredUsers { get; set; }
    public IList<string> Admins { get; set; } = new[] {"hamish.w"};
    public ulong GuildChannel { get; set; } = DefaultGuild;
    public ulong WordleChannel { get; set; } = DefaultWordleChannel;
    public ulong WinnerChannel { get; set; } = DefaultWinnerChannel;
    public MessageConfig MessageConfig { get; set; }
}