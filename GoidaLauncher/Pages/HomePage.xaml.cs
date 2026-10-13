using System.IO;
using System.Windows;
using System.Windows.Controls;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher.Pages;

public partial class HomePage : UserControl
{
    private readonly MinecraftService _minecraft;
    private readonly FabricService _fabric = new();
    private readonly QuiltService _quilt = new();
    private DiscordRpcService? _discordRpc;
    private readonly LauncherSettings _settings;
    private System.Diagnostics.Process? _runningGameProcess;

    /// <summary>События перехода по страницам.</summary>
    public event Action? RequestInstallPage;
    public event Action<LauncherAccount>? AccountChanged;

    public LauncherAccount CurrentAccount => GetCurrentAccount();

    public HomePage(LauncherSettings settings, MinecraftService minecraft)
    {
        InitializeComponent();

        _settings = settings;
        _minecraft = minecraft;

        InitAccounts();
        RefreshBuildCombo();

        BuildCombo.SelectionChanged += (_, _) =>
        {
            if (BuildCombo.SelectedItem is InstalledBuild b)
            {
                UpdateLoaderIcon(b.Loader);
            }
            UpdateLaunchButtonText();
        };

        Loaded += (_, _) =>
        {
            RefreshActiveSkin3D();
        };

        Unloaded += (_, _) => _discordRpc?.Dispose();
    }

    /// <summary>
    /// Обновляет отображение 3D-модели игрока и его бейджей в центре сцены.
    /// </summary>
    public void RefreshActiveSkin3D()
    {
        var skinFile = _settings.ActiveSkinPackFile;
        var skinPath = string.IsNullOrWhiteSpace(skinFile)
            ? null
            : Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), skinFile);

        PlayerModel3D.SetSkin(skinPath);
    }

    // ---------------------------------------------------------------
    // Аккаунты (управление в верхней карточке профиля через стрелку ▼)
    // ---------------------------------------------------------------

    private void InitAccounts()
    {
        _settings.Accounts ??= new List<LauncherAccount>();

        if (_settings.Accounts.Count == 0)
        {
            var nicks = _settings.RecentNicknames.ToList();
            if (!string.IsNullOrWhiteSpace(_settings.Nickname) && !nicks.Contains(_settings.Nickname))
                nicks.Insert(0, _settings.Nickname);
            if (nicks.Count == 0)
                nicks.Add("Player");

            foreach (var n in nicks)
            {
                _settings.Accounts.Add(new LauncherAccount
                {
                    Username = n,
                    Type = AccountType.Offline
                });
            }
        }

        var active = GetCurrentAccount();
        ApplyAccount(active);
    }

    public LauncherAccount GetCurrentAccount()
    {
        _settings.Accounts ??= new List<LauncherAccount>();
        var found = _settings.Accounts.FirstOrDefault(a => a.Id == _settings.ActiveAccountId)
                    ?? _settings.Accounts.FirstOrDefault(a => string.Equals(a.Username, _settings.Nickname, StringComparison.OrdinalIgnoreCase))
                    ?? _settings.Accounts.FirstOrDefault();

        if (found != null)
        {
            _settings.ActiveAccountId = found.Id;
            _settings.Nickname = found.Username;
            return found;
        }

        var def = new LauncherAccount
        {
            Username = string.IsNullOrWhiteSpace(_settings.Nickname) ? "Player" : _settings.Nickname,
            Type = AccountType.Offline
        };
        _settings.Accounts.Add(def);
        _settings.ActiveAccountId = def.Id;
        _settings.Nickname = def.Username;
        return def;
    }

    public void ApplyAccount(LauncherAccount account)
    {
        _settings.ActiveAccountId = account.Id;
        _settings.Nickname = account.Username;

        RefreshActiveSkin3D();
        AccountChanged?.Invoke(account);
    }

    // ---------------------------------------------------------------
    // Быстрые переходы
    // ---------------------------------------------------------------

    private void GoInstallButton_Click(object sender, RoutedEventArgs e) => RequestInstallPage?.Invoke();

    private void ToggleConsoleButton_Click(object sender, RoutedEventArgs e)
    {
        if (ConsolePanel.Visibility == Visibility.Visible)
        {
            ConsolePanel.Visibility = Visibility.Collapsed;
            ToggleConsoleButton.Content = "Развернуть";
        }
        else
        {
            ConsolePanel.Visibility = Visibility.Visible;
            ToggleConsoleButton.Content = "Свернуть";
        }
    }

    // ---------------------------------------------------------------
    // Данные для других страниц
    // ---------------------------------------------------------------

    public string? CurrentVersionId => (BuildCombo.SelectedItem as InstalledBuild)?.McVersion;

    public string? CurrentLoaderForModrinth => (BuildCombo.SelectedItem as InstalledBuild)?.Loader switch
    {
        "Fabric" => "fabric",
        "Quilt" => "quilt",
        "Forge" => "forge",
        _ => null,
    };

    // ---------------------------------------------------------------
    // Список установленных сборок
    // ---------------------------------------------------------------

    private void RefreshBuildCombo(InstalledBuild? preferSelect = null)
    {
        var items = _minecraft.GetInstalledBuilds()
            .OrderByDescending(b => b.McVersion)
            .ToList();

        if (preferSelect is not null &&
            !items.Any(b => b.McVersion == preferSelect.McVersion && b.Loader == preferSelect.Loader))
        {
            items.Insert(0, preferSelect);
        }

        BuildCombo.ItemsSource = items;
        NoBuildsHint.Visibility = items.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

        var toSelect = preferSelect is not null
            ? items.FirstOrDefault(b => b.McVersion == preferSelect.McVersion && b.Loader == preferSelect.Loader)
            : items.FirstOrDefault(b => b.McVersion == _settings.LastVersionId && b.Loader == _settings.ModLoader)
              ?? items.FirstOrDefault();

        BuildCombo.SelectedItem = toSelect;

        UpdateLoaderIcon(toSelect?.Loader);
        UpdateLaunchButtonText();
    }

    private void UpdateLoaderIcon(string? loader)
    {
        if (LoaderIconText == null) return;
        LoaderIconText.Text = loader switch
        {
            "Fabric" => "⚡",
            "Forge" => "⚒",
            "Quilt" => "🟣",
            "NeoForge" => "🔥",
            _ => "🟩"
        };
    }

    public void SetVersionAndLoader(string versionId, string loader)
    {
        RefreshBuildCombo(new InstalledBuild("", versionId, loader));
        Log($"Выбрано: {versionId} ({loader}).");
    }

    private void ApplyUiToSettings()
    {
        var currentAccount = GetCurrentAccount();
        _settings.ActiveAccountId = currentAccount.Id;
        _settings.Nickname = currentAccount.Username;

        if (BuildCombo.SelectedItem is InstalledBuild build)
        {
            _settings.LastVersionId = build.McVersion;
            _settings.ModLoader = build.Loader;
        }
    }

    // ---------------------------------------------------------------
    // Запуск игры
    // ---------------------------------------------------------------

    private async void LaunchButton_Click(object sender, RoutedEventArgs e)
    {
        // 1. Если игра уже запущена — кнопка работает как "ЗАКРЫТЬ"
        if (_runningGameProcess is not null && !_runningGameProcess.HasExited)
        {
            try
            {
                Log("Запрос на завершение игры. Закрываю Minecraft...");
                _runningGameProcess.Kill(entireProcessTree: true);
            }
            catch (Exception ex)
            {
                Log($"Не удалось остановить процесс игры: {ex.Message}");
            }
            return;
        }

        // 2. Иначе — запуск игры
        if (BuildCombo.SelectedItem is not InstalledBuild selected)
        {
            MessageBox.Show(OwnerWindow, "Сначала выберите версию (см. вкладку «Установка»).", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var account = GetCurrentAccount();
        if (string.IsNullOrWhiteSpace(account.Username))
        {
            MessageBox.Show(OwnerWindow, "Введите никнейм или войдите в аккаунт.", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        ApplyUiToSettings();
        SettingsService.Save(_settings);

        SetBusy(true);
        LaunchButton.IsEnabled = false;
        LaunchButton.Content = "Запуск...";

        // Автоматически разворачиваем консоль при запуске
        ConsolePanel.Visibility = Visibility.Visible;
        ToggleConsoleButton.Content = "Свернуть";

        try
        {
            if (account.Type == AccountType.LicenseToken)
            {
                Log($"Проверка токена лицензии для {account.Username}...");
                var check = await AuthService.QuickValidateTokenAsync(account.AccessToken);
                if (!check.IsValid)
                {
                    Log($"Внимание: {check.Message}");
                    var res = MessageBox.Show(OwnerWindow,
                        $"Токен лицензии для «{account.Username}» не прошёл проверку:\n\n{check.Message}\n\n" +
                        "Запустить игру в офлайн-режиме под этим ником?",
                        "GoidaLauncher — Проверка лицензии",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (res != MessageBoxResult.Yes)
                    {
                        return;
                    }

                    account = new LauncherAccount
                    {
                        Username = account.Username,
                        Type = AccountType.Offline
                    };
                }
                else
                {
                    Log($"Лицензия подтверждена! UUID: {account.Uuid}. Онлайн-режим активен.");
                }
            }

            Log($"Подготовка версии {selected.McVersion}...");
            var details = await _minecraft.GetVersionDetailsByIdAsync(selected.McVersion);

            var loader = selected.Loader;

            if (loader == "Fabric")
            {
                Log("Получаю данные Fabric Loader...");
                var loaderVersion = await _fabric.GetRecommendedLoaderVersionAsync(selected.McVersion);

                if (loaderVersion is null)
                {
                    Log($"Fabric не поддерживает версию {selected.McVersion}. Запускаю ванильную версию.");
                    loader = "Vanilla";
                }
                else
                {
                    details = await _fabric.GetMergedVersionDetailsAsync(details, selected.McVersion, loaderVersion);
                    Log($"Fabric Loader {loaderVersion} подключён.");
                }
            }
            else if (loader == "Quilt")
            {
                Log("Получаю данные Quilt Loader...");
                var loaderVersion = await _quilt.GetRecommendedLoaderVersionAsync(selected.McVersion);

                if (loaderVersion is null)
                {
                    Log($"Quilt не поддерживает версию {selected.McVersion}. Запускаю ванильную версию.");
                    loader = "Vanilla";
                }
                else
                {
                    details = await _quilt.GetMergedVersionDetailsAsync(details, selected.McVersion, loaderVersion);
                    Log($"Quilt Loader {loaderVersion} подключён.");
                }
            }
            else if (loader == "Forge")
            {
                Log("Получаю рекомендуемую версию Forge...");
                var forgeService = new ForgeService(_settings.GameDirectory, _minecraft.LibrariesDirectory);
                var forgeVersion = await forgeService.GetRecommendedForgeVersionAsync(selected.McVersion);

                if (forgeVersion is null)
                {
                    Log($"Forge не выпускался для версии {selected.McVersion}. Запускаю ванильную версию.");
                    loader = "Vanilla";
                }
                else
                {
                    Log($"Устанавливаю Forge {forgeVersion}...");
                    await _minecraft.EnsureClientJarDownloadedAsync(details);
                    var clientJarPath = _minecraft.GetClientJarPath(details);

                    var forgeLog = new Progress<string>(Log);
                    details = await forgeService.InstallAndMergeAsync(details, selected.McVersion, forgeVersion, clientJarPath, forgeLog);
                    Log($"Forge {forgeVersion} установлен.");
                }
            }
            else if (loader == "NeoForge")
            {
                Log("Получаю данные NeoForge...");
                var neoService = new NeoForgeService(_settings.GameDirectory, _minecraft.LibrariesDirectory);
                var neoVersion = await neoService.GetRecommendedNeoForgeVersionAsync(selected.McVersion);

                if (neoVersion is null)
                {
                    Log($"NeoForge не поддерживает версию {selected.McVersion} (NeoForge доступен для Minecraft 1.20.2+). Запускаю ванильную версию.");
                    loader = "Vanilla";
                }
                else
                {
                    Log($"Устанавливаю NeoForge {neoVersion}...");
                    await _minecraft.EnsureClientJarDownloadedAsync(details);
                    var clientJarPath = _minecraft.GetClientJarPath(details);

                    var neoLog = new Progress<string>(Log);
                    details = await neoService.InstallAndMergeAsync(details, selected.McVersion, neoVersion, clientJarPath, neoLog);
                    Log($"NeoForge {neoVersion} установлен.");
                }
            }

            var progress = new Progress<ProgressInfo>(p =>
            {
                StatusText.Text = p.Status;
                DownloadProgress.Value = p.Percent;
            });

            await _minecraft.DownloadVersionAsync(details, progress);
            await _minecraft.WriteInstallMetaAsync(details.Id, selected.McVersion, loader);

            var instanceDir = _minecraft.GetInstanceDirectory(selected.McVersion, loader);

            if (!string.IsNullOrWhiteSpace(_settings.ActiveSkinPackFile))
            {
                SkinService.SyncActiveSkinForCustomSkinLoader(
                    _settings.GameDirectory, _settings.ActiveSkinPackFile, instanceDir, account.Username);
            }

            Log("Запуск Minecraft...");
            var options = new LaunchOptions
            {
                Nickname = account.Username,
                Uuid = account.Type == AccountType.LicenseToken ? account.Uuid : null,
                AccessToken = account.Type == AccountType.LicenseToken ? account.AccessToken : null,
                UserType = account.Type == AccountType.LicenseToken ? "msa" : "legacy",
                MinRamMb = _settings.MinRamMb,
                MaxRamMb = _settings.MaxRamMb,
                JavaPath = _settings.JavaPath,
                ExtraJvmArgs = _settings.CustomJvmArgs,
                GameDirectoryOverride = instanceDir,
            };

            var process = _minecraft.Launch(details, options);
            _runningGameProcess = process;
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (_, args) => { if (args.Data is not null) LogFromGame(args.Data); };
            process.ErrorDataReceived += (_, args) => { if (args.Data is not null) LogFromGame(args.Data); };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            Log("Игра запущена. Лаунчер можно свернуть или закрыть игру кнопкой «ЗАКРЫТЬ».");
            UpdateLaunchButtonState(isGameRunning: true);

            RememberNickname(account.Username);
            SettingsService.Save(_settings);

            _ = StartDiscordRpcAsync(selected.McVersion, loader);
            process.Exited += (_, _) => Dispatcher.Invoke(async () =>
            {
                _runningGameProcess = null;
                UpdateLaunchButtonState(isGameRunning: false);
                Log("Игра завершена.");
                await ClearDiscordRpcAsync();
            });

            RefreshBuildCombo(new InstalledBuild(details.Id, selected.McVersion, loader));
        }
        catch (Exception ex)
        {
            Log($"Ошибка запуска: {ex.Message}");
            MessageBox.Show(OwnerWindow,
                "Не удалось запустить игру:\n\n" + ex.Message,
                "GoidaLauncher", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            SetBusy(false);
            if (_runningGameProcess is not null && !_runningGameProcess.HasExited)
            {
                UpdateLaunchButtonState(isGameRunning: true);
            }
            else
            {
                UpdateLaunchButtonState(isGameRunning: false);
            }
        }
    }

    private void UpdateLaunchButtonState(bool isGameRunning)
    {
        if (isGameRunning)
        {
            LaunchButton.Content = "ЗАКРЫТЬ";
            LaunchButton.Style = (Style)Application.Current.FindResource("StopGameButton");
            LaunchButton.IsEnabled = true;
            BuildCombo.IsEnabled = false;
            StatusText.Text = "Игра запущена";
        }
        else
        {
            BuildCombo.IsEnabled = true;
            LaunchButton.IsEnabled = true;
            DownloadProgress.Value = 0;
            UpdateLaunchButtonText();
        }
    }

    private void UpdateLaunchButtonText()
    {
        if (_runningGameProcess is not null && !_runningGameProcess.HasExited)
        {
            LaunchButton.Content = "ЗАКРЫТЬ";
            LaunchButton.Style = (Style)Application.Current.FindResource("StopGameButton");
            return;
        }

        LaunchButton.Style = (Style)Application.Current.FindResource("PrimaryPlayButton");

        if (BuildCombo.SelectedItem is InstalledBuild build)
        {
            bool isDownloaded = _minecraft.IsVersionDownloadedForLoader(build.McVersion, build.Loader);
            LaunchButton.Content = isDownloaded ? "ИГРАТЬ" : "СКАЧАТЬ И ИГРАТЬ";
            StatusText.Text = isDownloaded ? "Готов к запуску" : "Требуется скачивание";
        }
        else
        {
            LaunchButton.Content = "ИГРАТЬ";
            StatusText.Text = "Выберите версию";
        }
    }

    private void RememberNickname(string nickname)
    {
        _settings.RecentNicknames.RemoveAll(n => string.Equals(n, nickname, StringComparison.OrdinalIgnoreCase));
        _settings.RecentNicknames.Insert(0, nickname);
        if (_settings.RecentNicknames.Count > 8)
            _settings.RecentNicknames.RemoveRange(8, _settings.RecentNicknames.Count - 8);
    }

    private async Task StartDiscordRpcAsync(string versionId, string loader)
    {
        if (!_settings.DiscordRpcEnabled || string.IsNullOrWhiteSpace(_settings.DiscordApplicationId))
            return;

        try
        {
            _discordRpc?.Dispose();
            _discordRpc = new DiscordRpcService(_settings.DiscordApplicationId);

            if (await _discordRpc.ConnectAsync())
            {
                var loaderSuffix = loader == "Vanilla" ? "" : $" ({loader})";
                await _discordRpc.SetActivityAsync(
                    details: $"Играет в Minecraft {versionId}{loaderSuffix}",
                    state: "через GoidaLauncher",
                    startedAt: DateTimeOffset.Now);
            }
        }
        catch { }
    }

    private async Task ClearDiscordRpcAsync()
    {
        if (_discordRpc is null) return;
        try { await _discordRpc.ClearActivityAsync(); }
        catch { }
    }

    private Window? OwnerWindow => Window.GetWindow(this);

    private void SetBusy(bool busy)
    {
        BuildCombo.IsEnabled = !busy;
        DownloadProgress.Value = busy ? DownloadProgress.Value : 0;
    }

    private void Log(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            LogBox.ScrollToEnd();
        });
    }

    private void LogFromGame(string message)
    {
        Dispatcher.Invoke(() =>
        {
            LogBox.AppendText($"[game] {message}\n");
            LogBox.ScrollToEnd();
        });
    }
}
