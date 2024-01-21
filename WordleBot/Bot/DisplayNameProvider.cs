using Discord;
using Serilog;

namespace WordleBot.Bot;

public class DisplayNameProvider(ILogger log, IDiscordClient discordClient): IDisplayNameProvider
{
    
    public async Task<string> GetAsync(ulong userId)
    {
        try
        {
            var user = await discordClient.GetUserAsync(userId);
            if (user == null)
            {
                return "???";
            }

            if (user.GlobalName != null)
            {
                return user.GlobalName;
            }

            return user.Username;
        }
        catch (Exception e)
        {
            log.Warning(e, $"Error resolving name '{userId}'");
            return "???";
        }
    }
}