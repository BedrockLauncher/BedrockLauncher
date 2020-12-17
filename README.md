# BedrockLauncher
Forked from MCMrARM's w10 launcher

This tool allows you to install several versions of Minecraft: Windows 10 Edition (Bedrock) side-by-side.
This is useful if you want to test beta versions, releases or anything else side-by-side without needing to uninstall and reinstall the game.

## Disclaimer
This tool will **not** help you to pirate the game; it requires that you have a Microsoft account which can be used to download Minecraft from the Store.

## Prerequisites
- A Microsoft account connected to Microsoft Store which **owns Minecraft for Windows 10**
- **Administrator permissions** on your user account (or access to an account that has)
- **Developer mode** enabled for app installation in Windows 10 Settings
- If you want to be able to use beta versions, you'll additionally need to **subscribe to the Minecraft Beta program using Xbox Insider Hub**.

## Setup
- Download the latest release from the [Releases](https://github.com/XlynxX/BedrockLauncher/releases) section. Unzip it somewhere.
- Run `BedrockLauncher.exe` to start the launcher.

## Compiling the launcher yourself
You'll need Visual Studio with Windows 10 SDK version 10.0.17763 and .NET Framework 4.7.2 SDK installed. You can find these in the Visual Studio Installer if you don't have them out of the box.
The project should build out of the box with VS as long as you haven't done anything bizarre.

## Frequently Asked Questions
**Does this allow running multiple instances of Minecraft: Bedrock at the same time?**

No, it allows you to _install_ multiple versions, but only one version can run at a time.
