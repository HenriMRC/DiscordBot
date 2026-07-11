# AI Agent Guide

This repository is a .NET Discord bot that watches the EUR to BRL exchange rate and posts alerts to configured Discord text channels.

## Project Snapshot

- Runtime: .NET 10.
- App project: `discordbot/discordbot.csproj`.
- Test project: `discordbot.tests/discordbot.tests.csproj`.
- Main entry point: `discordbot/Program.cs`.
- Hosting and Discord lifecycle: `discordbot/hosting/`.
- Business services: `discordbot/services/`.
- Runtime state and argument parsing: `discordbot/runtime/` and `discordbot/Program.cs`.
- Persistent bot config: `config.json`, managed by `FileConfigStore`.

## Commands

Run these from the repository root:

```powershell
dotnet restore discordbot.sln
dotnet build discordbot.sln --configuration Debug
dotnet test discordbot.sln --configuration Debug
dotnet format discordbot.sln --verify-no-changes --verbosity minimal
```

Run the bot locally with one of these token sources:

```powershell
$env:DISCORD_BOT_TOKEN = "<token>"
dotnet run --project discordbot -- --log-level debug
```

```powershell
dotnet run --project discordbot -- --token "<token>"
```

```powershell
Set-Content -NoNewline -Path discordbot.token -Value "<token>"
dotnet run --project discordbot
```

Prefer the environment variable or ignored token file for local runs so secrets do not land in shell history.

## Architecture Notes

- `Program` wires dependency injection, logging, runtime settings, and hosted services.
- `BotHostedService` starts `BotHost`, which manages Discord connection events and the polling loop.
- `DiscordCommandHandler` handles text commands:
  - `update`
  - `min:<decimal>`
  - `max:<decimal>`
- `WiseRateProvider` calls Wise for the current exchange rate.
- `DiscordNotificationService` sends messages to each configured guild channel.
- `FileConfigStore` owns JSON persistence for channel names and per-guild ranges.

## Coding Guidance

- Keep app implementation classes `internal` unless there is a clear external API reason.
- Prefer constructor injection and small interfaces for side effects such as Discord, file IO, HTTP, and time.
- Add or update NUnit tests for parsing, config persistence, runtime state, and pure business logic.
- Avoid live Discord or Wise calls in tests; introduce seams/interfaces before testing external behavior.
- Keep nullable annotations enabled and do not silence warnings without a reason.
- Keep changes scoped. This is a compact project, so broad abstractions should earn their keep.

## Secret And Data Hygiene

- Never commit Discord tokens or other credentials.
- `discordbot.token` is local-only and ignored by git.
- Do not log full tokens, Discord secrets, or private user data.
- `config.json` may contain guild IDs and runtime alert thresholds. Treat it as operational data, not a credential.

## Before Finishing

- Run `dotnet test discordbot.sln --configuration Debug`.
- Run `dotnet format discordbot.sln --verify-no-changes --verbosity minimal` when editing C#.
- Mention any tests or checks that could not be run.
