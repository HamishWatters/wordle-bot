using Discord;
using Serilog;

namespace WordleBot.Bot;

public class DisplayNameProvider(ILogger log, IDiscordClient discordClient): IDisplayNameProvider
{
    private readonly Dictionary<ulong, string> _cache = new();
    
    public async Task<string> GetAsync(ulong userId)
    {
        if (_cache.TryGetValue(userId, out var cachedName))
        {
            return cachedName;
        }

        var name = await FindAsync(userId);
        _cache[userId] = name;
        return name;
    }

    public void ClearCache()
    {
        _cache.Clear();
    }

    private async Task<string> FindAsync(ulong userId)
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