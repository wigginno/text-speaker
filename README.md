# TextSpeaker

TextSpeaker is a fast, simple, and modern desktop application for converting text to speech using Azure AI Speech. Built with Avalonia UI for .NET, it provides a streamlined interface for generating high-quality MP3 audio from your text.

## Features

- Input text via file or direct paste
- Select from available Azure voices
- Output speech as MP3 files
- Cross-platform Avalonia UI desktop application
- Simple, keyboard-friendly interface

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Azure AI Speech subscription key and service region

## Setup / Configuration

TextSpeaker requires Azure AI Speech credentials. For development, use the following commands to store your credentials securely with [dotnet user-secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets):

```sh
dotnet user-secrets init --project ./TextSpeaker/TextSpeaker.csproj
dotnet user-secrets set "AzureSpeech:SubscriptionKey" "<your-subscription-key>" --project ./TextSpeaker/TextSpeaker.csproj
dotnet user-secrets set "AzureSpeech:ServiceRegion" "<your-service-region>" --project ./TextSpeaker/TextSpeaker.csproj
```

> **Note:** These commands are for development only. Do not use user-secrets for production deployment.

## Building

```sh
dotnet build
```

## Running

```sh
dotnet run --project ./TextSpeaker/TextSpeaker.csproj
```

## Dependencies

- [Avalonia UI](https://avaloniaui.net/) - cross-platform .NET UI framework
- [Azure AI Speech SDK](https://learn.microsoft.com/en-us/azure/ai-services/speech-service/) - text-to-speech engine
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) - MVVM utilities for .NET
- .NET 8

## License

See [LICENSE](./LICENSE) for details.
