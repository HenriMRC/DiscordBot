# Copilot Instructions

Follow the repository guidance in `AGENTS.md`.

Key reminders:

- This is a .NET 10 Discord bot with NUnit tests.
- Keep behavior changes covered by focused tests.
- Use the existing service/hosting/model folders instead of creating parallel architecture.
- Do not expose or log Discord tokens.
- Validate with `dotnet test discordbot.sln --configuration Debug` before handing work back.
