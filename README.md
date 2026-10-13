# 🛡 Goida Launcher v6.7

<p align="center">
  <img src="GoidaLauncher/Assets/icon.png" width="128" height="128" alt="Goida Launcher Logo"/>
</p>

<p align="center">
  <strong>Современный, высокопроизводительный и кастомизируемый оффлайн-лаунчер Minecraft для Windows</strong>
</p>

<p align="center">
  <a href="https://github.com/murkLol595/GoidaLauncher/releases/latest"><img src="https://img.shields.io/badge/Release-v6.7-10B981?style=for-the-badge&logo=github" alt="Release v6.7"/></a>
  <img src="https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 10.0"/>
  <img src="https://img.shields.io/badge/Platform-Windows%20x64-0078D4?style=for-the-badge&logo=windows" alt="Windows x64"/>
  <img src="https://img.shields.io/badge/Built%20With-Google%20Antigravity%20%7C%20Gemini%20AI-4285F4?style=for-the-badge&logo=google" alt="AI Developed"/>
</p>

---

## 🤖 Разработано с помощью ИИ (Google Antigravity & Gemini)

**Goida Launcher** спроектирован и разработан в тесном сотрудничестве с продвинутым ИИ-агентом **Google Antigravity** на базе моделей **Google Gemini**.

В рамках совместной разработки с ИИ были реализованы и оптимизированы ключевые технологии:
- 🧠 **Архитектура и сервисный слой:** асинхронные пайплайны скачивания ассетов и валидации библиотек Minecraft.
- 👕 **Интерактивный 3D-движок скинов:** аппаратный рендеринг 3D-модели персонажа на WPF Viewport3D с поддержкой вращения мышью в реальном времени.
- ⚡ **Глубокая оптимизация производительности:** устранение просадок FPS интерфейса, кеширование стилей и заморозка кистей (Freeze), минимизация нагрузки на CPU/GPU.
- 📦 **Система автономного установщика:** создание конфигурации и автоматизированной сборки Inno Setup 6 для развёртывания в один клик.
- 🛡 **Интеграция спецификаций агентов:** проект включает файлы [`AGENTS.md`](AGENTS.md) и [`GEMINI.md`](GEMINI.md) со стандартами разработки для автономных ИИ-ассистентов.

---

## ✨ Возможности версии 6.7

- 🎮 **Удобная главная сцена:**
  - Быстрый запуск выбранной версии одним кликом.
  - Окно статуса и консоль вывода игры прямо на главной странице.
  - Возможность принудительного закрытия зависшего процесса игры из лаунчера.
- 👕 **3D предпросмотр скинов:**
  - Полноценная 3D-модель персонажа с текстурой вашего скина.
  - Вращение модели мышью на 360 градусов.
  - Автоматическая генерация универсального ресурспака скина для всех сборок без аккаунта Mojang.
- 📥 **Установка любых версий и загрузчиков:**
  - **Vanilla, Fabric, Quilt, Forge, NeoForge** — установка в один клик через официальные Meta API.
  - Фильтры по типу версий (Релизы, Снапшоты, Бета) и мгновенный поиск.
- 📦 **Полная изоляция сборок:**
  - Каждая связка «Версия + Загрузчик» имеет собственную папку `instances/` со своими `mods/`, `config/`, `saves/`, `resourcepacks/`.
  - Моды разных сборок никогда не конфликтуют между собой.
- 🧩 **Каталог Modrinth:**
  - Поиск и установка модов и ресурспаков прямо в лаунчере в один клик.
- 🔑 **Гибкая система авторизации:**
  - Быстрый оффлайн-вход по никнейму с историей последних ников.
  - Вход по токену через удобное диалоговое окно.
- 🎨 **Кастомизация и эффекты:**
  - Фирменная тёмная неоновая тема **Goida Neon**.
  - Живой анимированный фон с плавающими неоновыми сферами.
  - Эффект матового стекла (Glassmorphism) и неоновое свечение (Glow).
  - Настройка скругления углов (14px, 8px, 4px) и акцентных цветов.
- 🎧 **Discord Rich Presence:**
  - Нативный показ статуса игры в Discord через IPC без лишних зависимостей.

---

## 📥 Установка

Самый простой способ — скачать готовый официальный установщик:

👉 **[Скачать GoidaLauncher-Setup-v6.7.exe](https://github.com/murkLol595/GoidaLauncher/releases/latest)**

* Установщик автоматически настроит лаунчер, добавит ярлыки на рабочий стол и в меню «Пуск».
* Лаунчер полностью автономен (все компоненты .NET 10.0 уже зашиты внутрь).

---

## 🛠 Сборка из исходного кода

### Требования:
- Windows 10/11 x64
- [.NET SDK 10.0](https://dotnet.microsoft.com/download)
- Visual Studio 2022 (17.12+) с компонентом *.NET Desktop Development* (опционально)
- Установленная Java (Java 21 для Minecraft 1.20.5+, Java 17 для 1.17–1.20.4, Java 8 для 1.12–1.16)

### Сборка и запуск:
```bash
# Клонирование репозитория
git clone https://github.com/murkLol595/GoidaLauncher.git
cd GoidaLauncher

# Сборка проекта
dotnet build -c Release

# Запуск
dotnet run --project GoidaLauncher
```

### Публикация автономного EXE:
```bash
dotnet publish GoidaLauncher/GoidaLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

### Сборка инсталлятора (Inno Setup):
```bash
& "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

---

## 📁 Структура проекта

```text
GoidaLauncher.sln
├── AGENTS.md                  # Инструкции для ИИ-агентов (Antigravity/Gemini)
├── GEMINI.md                  # Спецификация проекта для Google Gemini
├── installer.iss              # Скрипт сборки инсталлятора Inno Setup
└── GoidaLauncher/
    ├── App.xaml / .cs         # Глобальные стили, темы оформления и ресурсы
    ├── MainWindow.xaml / .cs  # Хост-окно с навигацией и статус-баром
    ├── Pages/
    │   ├── HomePage.xaml / .cs           # Главная страница: запуск, 3D персонаж, консоль
    │   ├── InstallVersionPage.xaml / .cs # Каталог версий и загрузчиков
    │   ├── ModrinthPage.xaml / .cs       # Поиск и установка модов Modrinth
    │   ├── SkinPage.xaml / .cs           # Управление скинами игрока
    │   ├── SettingsPage.xaml / .cs       # Настройки памяти, Java, тем и интерфейса
    │   └── SkinViewer3D.xaml / .cs       # 3D движок рендеринга модели персонажа
    ├── Services/
    │   ├── MinecraftService.cs    # Скачивание, проверка файлов и запуск игры
    │   ├── FabricService.cs       # Авто-установка Fabric
    │   ├── ForgeService.cs        # Авто-установка Forge (processors pipeline)
    │   ├── QuiltService.cs        # Авто-установка Quilt
    │   ├── ModrinthService.cs     # API поиска модов Modrinth
    │   ├── SkinService.cs         # Генерация оффлайн ресурспаков скинов
    │   ├── ThemeService.cs        # Движок тем оформления и акцентов
    │   └── DiscordRpcService.cs   # Нативный Discord Rich Presence
    └── Models/                    # Модели профилей, версий и манифестов
```

---

## 📄 Лицензия & Авторство

* **Проект:** Goida Launcher v6.7
* **Автор:** [murkLol595](https://github.com/murkLol595)
* **AI-партнёр:** Google Antigravity & DeepMind Gemini
