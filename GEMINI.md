# Project Context for Google Gemini / Antigravity Agent

## 🚀 Goida Launcher v6.7
Goida Launcher is a modern, high-performance offline Minecraft launcher built on .NET 10.0 and WPF (Windows Presentation Foundation), co-developed with **Google Antigravity** and **Gemini AI**.

---

### 🛠 Tech Stack & Architecture
- **Language & Runtime:** C# 14 / .NET 10.0 Windows Desktop (`net10.0-windows`)
- **UI Framework:** WPF with hardware-accelerated Direct3D / Media3D viewport
- **Platform:** Windows x64 (10 / 11)
- **Installer:** Inno Setup 6 (LZMA2 ultra compression, single setup executable)

---

### 📂 Directory Structure
- `GoidaLauncher/`
  - `MainWindow.xaml` / `.cs` — Core host window with navigation and title bar
  - `Pages/`
    - `HomePage.xaml` / `.cs` — Play bar, version selection, 3D skin viewer, console output
    - `InstallVersionPage.xaml` / `.cs` — Mod loader & version manifest installer
    - `ModrinthPage.xaml` / `.cs` — Modrinth mod and resource pack browser
    - `SkinPage.xaml` / `.cs` — Skin manager & PNG skin loader
    - `SettingsPage.xaml` / `.cs` — RAM allocation, JVM flags, visual customization
    - `SkinViewer3D.xaml` / `.cs` — 3D interactive player model viewport
  - `Services/`
    - `MinecraftService.cs` — Game download pipeline, assets/libraries verification, process launch
    - `FabricService.cs`, `ForgeService.cs`, `QuiltService.cs` — Automated loader installers
    - `ModrinthService.cs` — Search & download mods/resource packs
    - `SkinService.cs` — Offline skin resource pack generation
    - `ThemeService.cs` — Dynamic themes & visual accents
    - `DiscordRpcService.cs` — Native IPC Discord Rich Presence
  - `Models/` — Data contracts and profiles

---

### 🤖 AI Agent Guidelines
1. **Performance First:** Keep WPF visual tree lean, use cached frozen brushes and background dispatchers for file/network I/O.
2. **Build Verification:** Always run `dotnet build -c Release` to ensure zero warnings and errors.
3. **Packaging:** Deploy through `dotnet publish -c Release -r win-x64 --self-contained true` followed by Inno Setup compilation via `installer.iss`.
