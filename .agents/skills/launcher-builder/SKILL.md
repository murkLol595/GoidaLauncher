---
name: launcher-builder
description: Automates building, publishing, and packaging Goida Launcher into Inno Setup installer.
---

# Goida Launcher Builder Skill

Use this skill when compiling new versions or updating installer packages for Goida Launcher.

## Steps:
1. Run `dotnet publish GoidaLauncher/GoidaLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`.
2. Compile installer using Inno Setup: `ISCC.exe installer.iss`.
3. Output executable will be placed in `InstallerOutput/GoidaLauncher-Setup-v7.5.exe`.
