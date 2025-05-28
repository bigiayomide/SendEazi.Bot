# SendEazi.Bot

This repository contains the source code for `SendEazi.Bot`, a .NET chat bot. The solution includes the bot host, core libraries, infrastructure and a comprehensive test project.

## Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/) (preview) or later
- Optional: a PostgreSQL and Redis instance if you want to run the bot locally

## Building the Solution

Restore dependencies and build all projects:

```bash
 dotnet build SendEazi.Bot.sln
```

## Running Unit Tests

Execute all tests with:

```bash
 dotnet test
```

## Configuration

Configuration values are read from `Bot.Host/appsettings.json`, `appsettings.Development.json` and environment variables. When running the bot locally you may need to set the following settings (either via environment variables or by editing `appsettings.json`):

- `ConnectionStrings:DefaultConnection` – PostgreSQL connection string
- `ConnectionStrings:MassTransitConnection` – PostgreSQL connection for MassTransit
- `ConnectionStrings:Redis` – Redis connection string
- `AzureOpenAI:Endpoint` / `AzureOpenAI:ApiKey` – Azure OpenAI service credentials
- `FormRecognizer:Endpoint` / `FormRecognizer:ApiKey` – Azure Form Recognizer credentials
- `WhatsApp:AccessToken` / `VerifyToken` – WhatsApp Business API credentials

Additional keys exist for speech services and SMS providers. Adjust them as needed for your environment.
