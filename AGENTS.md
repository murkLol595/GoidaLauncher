# AGENTS.md — Instructions for Autonomous Coding Agents

Welcome to the **Goida Launcher** project! This file serves as the official specification for AI agents (Google Antigravity, Gemini, etc.) working on this repository.

---

## 📌 Project Overview
- **Name:** Goida Launcher
- **Version:** 6.7
- **Type:** Offline Minecraft Launcher with Mod Loader & 3D Skin support
- **Framework:** .NET 10.0 WPF (C#)
- **Co-developer:** Created and maintained in collaboration with **Google Antigravity / Gemini AI**.

---

## 📋 Common Agent Commands

### 1. Build & Run
```bash
# Debug build
dotnet build GoidaLauncher/GoidaLauncher.csproj

# Run launcher
dotnet run --project GoidaLauncher/GoidaLauncher.csproj
```

### 2. Standalone Self-Contained Publish
```bash
dotnet publish GoidaLauncher/GoidaLauncher.csproj -c Release -r win-x64 --self-contained true --no-restore -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### 3. Build Setup Installer
```bash
& "C:\Users\admin\AppData\Local\Programs\Inno Setup 6\ISCC.exe" installer.iss
# Generates: InstallerOutput\GoidaLauncher-Setup-v6.7.exe
```

---

## 🛡 Coding Standards
- Preserve zero warnings and errors on Release builds.
- Retain complete branding consistency: **Goida Launcher**.
- Asynchronous operations (`async`/`await`) must not block the WPF UI dispatcher.
- Resource packs and skin rendering must remain compatible with Minecraft 1.8 through 1.21+.
