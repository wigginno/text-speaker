# Text Speaker

A simple desktop app for TTS using Azure AI Speech services. Choose a voice and convert your text into an audio file (mp3).

![image](https://github.com/user-attachments/assets/39951665-5a26-4b95-83b0-155fd0fa910a)

## Download/Install

Check the [releases page](https://github.com/wigginno/text-speaker/releases) to download prebuilt binaries for Windows, Linux, and MacOS. Extract the zip and run the TextSpeaker executable. I might code sign these binaries later.

Your other option is to grab the [.net SDK](https://dotnet.microsoft.com/en-us/download) 9 and [build from source](#building-from-source).

## Features

*   Convert text to speech using Azure AI Services
*   Input text directly or load from `.txt` files
*   Select language, region, voice type (Neural/Standard), and voice
*   Voice list automatically filtered based on your selections
*   Save synthesized audio output to an MP3 file
*   Configure Azure credentials easily via a Settings window

## Requirements

*   An Azure account with an active speech service resource
*   Your key and region for that resource
*   .NET 9 Runtime installed to run the application (.NET 8 or 10 might work too, haven't tested)

## Configuration

On first run:
1.  Click the `Settings` button in the main window
2.  Enter your Azure Speech Key and the Region name (e.g., `eastus`, `westus2`)
3.  Click `Save`
4.  Close the Settings window

The app will automatically refresh its configuration and attempt to load the available voices using your new credentials.

Settings are stored locally in `settings.json`. The path is usually in your user's application data folder (e.g., `%APPDATA%\TextSpeaker` on Windows, `~/.config/TextSpeaker` on Linux, `~/Library/Application Support/TextSpeaker` on macOS). You can see the exact path in the Settings window status bar.

## Usage

1.  Enter or paste text into the main text box, or click `select .txt file` to load from a file.
2.  Use the dropdowns to select the desired Language, Region (if applicable), Voice Type, and the specific Voice. The lists will update as you make selections.
3.  Click `save as mp3`.
4.  A file dialog will pop up. Choose where to save the MP3 file and click Save.

## Building from Source

```bash
git clone https://github.com/wigginno/text-speaker.git
cd text-speaker/TextSpeaker
dotnet build
dotnet run
```
