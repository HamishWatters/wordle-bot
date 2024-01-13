# Wordle Bot
A Discord bot for tracking members Wordle answers and announcing a winner per day

### Requirements 
#### Running
- [.NET 8 runtime or SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Discord bot API key](https://discord.com/developers/docs/intro)

#### Build
- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Building
I build locally using a Rider configuration with
 - Configuration `Release`
 - Target framework `.net8.0`
 - Deployment mode `Self-Contained`
 - Target runtime `linux-arm64`

This can also be done using the `dotnet publish` 
[command](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish)
or Visual Studio

### Running
- Copy the built binaries onto the target platform for running the bot. I use a Raspberry Pi 4
- Create a JSON config file (see below)
- Run the bot like so: `./dotnet WordleBot.dll config.json`

#### Configuration
```
{
  "apiToken": "secretToken", // API key for your discord bot
  "testMode": false, // For development, if true then the bot will log any outgoing messages rather than send them to discord
  "requiredUsers": [1, 2, 3], // Array of discord user IDs for users required to have played before the bot announces the result
  "admins": [1], // Array of discord user IDs for users who can execute admin commands
  "guildChannel": 123, // Discord guild ID
  "wordleChannel": 456, // Discord channel ID for channel where Wordle results are shared
  "winnerChannel": 789, // Discord channel ID where bot will share the winner once everyone has played
}
```
See `docs/config-schema.json` for complete schema
