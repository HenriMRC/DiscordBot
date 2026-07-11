# Discord Bot

.NET Discord bot for EUR to BRL exchange-rate notifications. The bot polls Wise, remembers per-guild alert ranges, and posts updates to the configured Discord text channel.

## Requirements

- .NET SDK 10.0.x
- A Discord bot token
- A text channel matching the configured channel name, `bot-cambio` by default

## Quick Start

```powershell
dotnet restore discordbot.sln
dotnet build discordbot.sln --configuration Debug
dotnet test discordbot.sln --configuration Debug
```

Run with an environment variable:

```powershell
$env:DISCORD_BOT_TOKEN = "<token>"
dotnet run --project discordbot -- --log-level debug
```

Run with a local ignored token file:

```powershell
Set-Content -NoNewline -Path discordbot.token -Value "<token>"
dotnet run --project discordbot
```

Run with an argument:

```powershell
dotnet run --project discordbot -- --token "<token>"
```

Prefer the environment variable or token file for local development so the token is not stored in shell history.

## Configuration

The bot reads `config.json` from the repository root at runtime. Use `config.example.json` as the safe template:

```json
{
  "channelName": "bot-cambio",
  "channels": {}
}
```

`channels` is populated with Discord guild IDs and rate ranges as commands are used.

## Discord Commands

- `update`: posts the latest known rate.
- `min:<decimal>`: sets the lower alert threshold for the current guild.
- `max:<decimal>`: sets the upper alert threshold for the current guild.

## Project Map

- `discordbot/Program.cs`: app entry point, argument parsing, dependency injection, logging setup.
- `discordbot/hosting/`: Discord lifecycle and hosted-service integration.
- `discordbot/services/`: command handling, notifications, config persistence, and Wise rate lookup.
- `discordbot/models/`: serializable configuration and range model.
- `discordbot/runtime/`: runtime state/settings.
- `discordbot.tests/`: NUnit coverage for parsing, config, runtime, logging, and model behavior.

## AI-Assisted Development

This repo includes `AGENTS.md` and `.github/copilot-instructions.md` so AI coding tools can quickly understand the architecture, commands, and safety rules.

Before handing changes back, run:

```powershell
dotnet test discordbot.sln --configuration Debug
dotnet format discordbot.sln --verify-no-changes --verbosity minimal
```

Never commit Discord tokens. `discordbot.token` is ignored by git.
