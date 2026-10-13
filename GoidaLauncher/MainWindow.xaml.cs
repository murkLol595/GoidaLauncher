using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using GoidaLauncher.Models;
using GoidaLauncher.Pages;
using GoidaLauncher.Services;

namespace GoidaLauncher;

public partial class MainWindow : Window
{
    private static readonly string[] EasterEggLinks =
    {
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "https://www.youtube.com/watch?v=oHg5SJYRHA0",
        "https://www.youtube.com/watch?v=QH2-TGUlwu4",
    };
    private static readonly Random EasterEggRandom = new();

    private readonly LauncherSettings _settings;
    private readonly MinecraftService _minecraft;

    private readonly HomePage _homePage;
    private readonly InstallVersionPage _installPage;
    private readonly SettingsPage _settingsPage;
    private readonly ModrinthPage _modrinthPage;
    private readonly SkinPage _skinPage;

    private System.Windows.Controls.Button? _activeNavButton;

    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowChromeService.ApplyDarkTitleBar(this);

        _settings = SettingsService.Load();
        _minecraft = new MinecraftService(_settings.GameDirectory);

        _homePage = new HomePage(_settings, _minecraft);
        _homePage.RequestInstallPage += ShowInstallPage;
        _homePage.AccountChanged += UpdateTopAccountPill;

        _installPage = new InstallVersionPage(_minecraft);
        _installPage.VersionChosen += (versionId, loader) =>
        {
            _homePage.SetVersionAndLoader(versionId, loader);
            ShowHomePage();
        };

        _settingsPage = new SettingsPage(_settings);
        _modrinthPage = new ModrinthPage();
        _skinPage = new SkinPage(_settings);
        _skinPage.SkinChanged += () =>
        {
            _homePage.RefreshActiveSkin3D();
            UpdateTopAccountPill(_homePage.CurrentAccount);
        };

        ThemeService.ApplyTheme(_settings, this);
        UpdateTopAccountPill(_homePage.CurrentAccount);
        ShowHomePage();
    }

    /// <summary>
    /// Применяет визуальные параметры темы к главному окну.
    /// </summary>
    public void ApplyThemeVisuals(LauncherSettings settings, ThemeDefinition? theme = null)
    {
        theme ??= ThemeService.GetPreset(settings.ThemePreset);

        if (LogoTitleText != null)
        {
            LogoTitleText.Text = string.IsNullOrWhiteSpace(settings.LogoTitle) ? "GOIDA" : settings.LogoTitle;
        }

        if (BackgroundCanvas != null)
        {
            BackgroundCanvas.Visibility = settings.AnimatedBackgroundEnabled ? Visibility.Visible : Visibility.Collapsed;
        }

        if (Blob1Stop != null) Blob1Stop.Color = theme.Blob1;
        if (Blob2Stop != null) Blob2Stop.Color = theme.Blob2;
        if (Blob3Stop != null) Blob3Stop.Color = theme.Blob3;
        if (Blob5Stop != null) Blob5Stop.Color = theme.Blob5;

        if (_activeNavButton != null)
        {
            var accent = Application.Current.Resources["AccentColorRaw"] is Color c ? c : theme.PrimaryAccent;
            _activeNavButton.Background = new SolidColorBrush(Color.FromArgb(0x38, accent.R, accent.G, accent.B));
            _activeNavButton.Foreground = new SolidColorBrush(accent);
        }
    }

    /// <summary>
    /// Обновляет мини-карточку профиля в правом верхнем углу (аватар, ник, статус).
    /// </summary>
    public void UpdateTopAccountPill(LauncherAccount? account)
    {
        if (account == null) return;

        TopNicknameText.Text = account.Username;
        if (account.Type == AccountType.LicenseToken)
        {
            TopAccountTypeText.Text = "🟢 Лицензия";
            TopAccountTypeText.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
        }
        else
        {
            TopAccountTypeText.Text = "👤 Офлайн";
            TopAccountTypeText.Foreground = (Brush)FindResource("TextSecondary");
        }

        var activeSkin = _settings.ActiveSkinPackFile;
        var skinPath = string.IsNullOrWhiteSpace(activeSkin)
            ? null
            : Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), activeSkin);

        PlayerAvatarImage.Source = SteveMeshData.CreateHeadAvatar(skinPath);
    }

    // ---------------------------------------------------------------
    // Навигация
    // ---------------------------------------------------------------

    private void SetActiveNavButton(System.Windows.Controls.Button button)
    {
        if (_activeNavButton is not null)
            _activeNavButton.ClearValue(System.Windows.Controls.Control.BackgroundProperty);

        var accent = Application.Current.Resources["AccentColorRaw"] is Color c ? c : Color.FromRgb(0x5C, 0xDB, 0x5C);
        button.Background = new SolidColorBrush(Color.FromArgb(0x38, accent.R, accent.G, accent.B));
        button.Foreground = new SolidColorBrush(accent);
        _activeNavButton = button;
    }

    private void ResetInactiveButtonColors()
    {
        foreach (var btn in new[] { HomeNavButton, InstallNavButton, ModrinthNavButton, SkinNavButton, SettingsNavButton })
        {
            if (btn == _activeNavButton) continue;
            btn.ClearValue(System.Windows.Controls.Control.BackgroundProperty);
            btn.ClearValue(System.Windows.Controls.Control.ForegroundProperty);
        }
    }

    private void ShowHomePage()
    {
        PageTitleText.Text = "Главная панель";
        PageHost.Content = _homePage;
        SetActiveNavButton(HomeNavButton);
        ResetInactiveButtonColors();
    }

    private void ShowInstallPage()
    {
        PageTitleText.Text = "Установка версии";
        PageHost.Content = _installPage;
        SetActiveNavButton(InstallNavButton);
        ResetInactiveButtonColors();
    }

    private void ShowModrinthPage()
    {
        PageTitleText.Text = "Каталог модов Modrinth";
        var versionId = _homePage.CurrentVersionId;
        var loader = _homePage.CurrentLoaderForModrinth;

        var targetDir = versionId is not null
            ? _minecraft.GetInstanceDirectory(versionId, loader ?? "vanilla")
            : _settings.GameDirectory;

        _modrinthPage.UpdateContext(targetDir, versionId, loader);
        PageHost.Content = _modrinthPage;
        SetActiveNavButton(ModrinthNavButton);
        ResetInactiveButtonColors();
    }

    private void ShowSettingsPage()
    {
        PageTitleText.Text = "Настройки лаунчера";
        PageHost.Content = _settingsPage;
        SetActiveNavButton(SettingsNavButton);
        ResetInactiveButtonColors();
    }

    private void ShowSkinPage()
    {
        PageTitleText.Text = "Гардероб скинов";
        PageHost.Content = _skinPage;
        SetActiveNavButton(SkinNavButton);
        ResetInactiveButtonColors();
    }

    private void HomeNavButton_Click(object sender, RoutedEventArgs e) => ShowHomePage();
    private void InstallNavButton_Click(object sender, RoutedEventArgs e) => ShowInstallPage();
    private void ModrinthNavButton_Click(object sender, RoutedEventArgs e) => ShowModrinthPage();
    private void SkinNavButton_Click(object sender, RoutedEventArgs e) => ShowSkinPage();
    private void SettingsNavButton_Click(object sender, RoutedEventArgs e) => ShowSettingsPage();

    private void AccountPill_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.OriginalSource is DependencyObject d && FindParent<System.Windows.Controls.Button>(d) == TopTokenButton)
            return;

        RefreshAccountsMenu();
        AccountMenuPopup.IsOpen = !AccountMenuPopup.IsOpen;
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject? parent = child;
        while (parent != null)
        {
            if (parent is T t) return t;
            parent = VisualTreeHelper.GetParent(parent);
        }
        return null;
    }

    public void RefreshAccountsMenu()
    {
        AccountsListPanel.Children.Clear();
        _settings.Accounts ??= new List<LauncherAccount>();

        if (_settings.Accounts.Count == 0)
        {
            _settings.Accounts.Add(new LauncherAccount { Username = "Player", Type = AccountType.Offline });
        }

        var activeId = _settings.ActiveAccountId;

        foreach (var acc in _settings.Accounts)
        {
            var isCurrent = acc.Id == activeId || (_settings.Accounts.Count == 1);
            var itemBorder = new System.Windows.Controls.Border
            {
                Background = isCurrent ? new SolidColorBrush(Color.FromArgb(0x30, 0x5C, 0xDB, 0x5C)) : new SolidColorBrush(Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF)),
                BorderBrush = isCurrent ? (Brush)FindResource("AccentGreen") : new SolidColorBrush(Color.FromArgb(0x28, 0x3E, 0x4E, 0x7E)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = System.Windows.Input.Cursors.Hand
            };

            var grid = new System.Windows.Controls.Grid();
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition { Width = GridLength.Auto });

            // 1. Аватарка
            var avBorder = new System.Windows.Controls.Border
            {
                Width = 26,
                Height = 26,
                CornerRadius = new CornerRadius(13),
                ClipToBounds = true,
                Background = new SolidColorBrush(Color.FromRgb(0x25, 0x2F, 0x47)),
                Margin = new Thickness(0, 0, 8, 0)
            };
            var skinPath = !string.IsNullOrWhiteSpace(acc.ActiveSkinFile)
                ? Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), acc.ActiveSkinFile)
                : (!string.IsNullOrWhiteSpace(_settings.ActiveSkinPackFile)
                    ? Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), _settings.ActiveSkinPackFile)
                    : null);
            var avImg = new System.Windows.Controls.Image
            {
                Width = 26,
                Height = 26,
                Source = SteveMeshData.CreateHeadAvatar(skinPath)
            };
            RenderOptions.SetBitmapScalingMode(avImg, BitmapScalingMode.NearestNeighbor);
            avBorder.Child = avImg;
            System.Windows.Controls.Grid.SetColumn(avBorder, 0);
            grid.Children.Add(avBorder);

            // 2. Имя и тип
            var sp = new System.Windows.Controls.StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var nameTxt = new System.Windows.Controls.TextBlock
            {
                Text = acc.Username,
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                Foreground = (Brush)FindResource("TextPrimary")
            };
            var typeTxt = new System.Windows.Controls.TextBlock
            {
                Text = acc.Type == AccountType.LicenseToken ? "🟢 Лицензия (Токен)" : "👤 Офлайн",
                FontSize = 10,
                Foreground = acc.Type == AccountType.LicenseToken ? new SolidColorBrush(Color.FromRgb(74, 222, 128)) : (Brush)FindResource("TextSecondary")
            };
            sp.Children.Add(nameTxt);
            sp.Children.Add(typeTxt);
            System.Windows.Controls.Grid.SetColumn(sp, 1);
            grid.Children.Add(sp);

            // 3. Бейдж активности / кнопка удаления
            var actionsSp = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            if (isCurrent)
            {
                var checkTxt = new System.Windows.Controls.TextBlock
                {
                    Text = "✔",
                    Foreground = (Brush)FindResource("AccentGreen"),
                    FontWeight = FontWeights.Black,
                    FontSize = 14,
                    Margin = new Thickness(0, 0, 8, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                actionsSp.Children.Add(checkTxt);
            }

            if (_settings.Accounts.Count > 1)
            {
                var delBtn = new System.Windows.Controls.Button
                {
                    Content = "✕",
                    Width = 22,
                    Height = 22,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Foreground = (Brush)FindResource("TextSecondary"),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    FontSize = 11,
                    ToolTip = "Удалить аккаунт"
                };
                var targetAcc = acc;
                delBtn.Click += (s, e) =>
                {
                    e.Handled = true;
                    DeleteAccount(targetAcc);
                };
                actionsSp.Children.Add(delBtn);
            }

            System.Windows.Controls.Grid.SetColumn(actionsSp, 2);
            grid.Children.Add(actionsSp);

            itemBorder.Child = grid;

            var selectAcc = acc;
            itemBorder.MouseLeftButtonUp += (s, e) =>
            {
                SelectAccount(selectAcc);
                AccountMenuPopup.IsOpen = false;
            };

            AccountsListPanel.Children.Add(itemBorder);
        }
    }

    public void SelectAccount(LauncherAccount account)
    {
        _settings.ActiveAccountId = account.Id;
        _settings.Nickname = account.Username;
        if (!string.IsNullOrWhiteSpace(account.ActiveSkinFile))
        {
            _settings.ActiveSkinPackFile = account.ActiveSkinFile;
        }
        SettingsService.Save(_settings);

        UpdateTopAccountPill(account);
        _homePage.ApplyAccount(account);
    }

    private void DeleteAccount(LauncherAccount account)
    {
        var res = MessageBox.Show(this, $"Удалить аккаунт «{account.Username}»?", "GoidaLauncher", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (res != MessageBoxResult.Yes) return;

        _settings.Accounts.RemoveAll(a => a.Id == account.Id);
        if (_settings.Accounts.Count == 0)
        {
            _settings.Accounts.Add(new LauncherAccount { Username = "Player", Type = AccountType.Offline });
        }

        var next = _settings.Accounts.FirstOrDefault(a => a.Id == _settings.ActiveAccountId) ?? _settings.Accounts.First();
        SelectAccount(next);
        RefreshAccountsMenu();
    }

    private void AddNicknameButton_Click(object sender, RoutedEventArgs e)
    {
        var nick = NewNicknameInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(nick)) return;

        var existing = _settings.Accounts.FirstOrDefault(a => string.Equals(a.Username, nick, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            SelectAccount(existing);
        }
        else
        {
            var newAcc = new LauncherAccount
            {
                Username = nick,
                Type = AccountType.Offline
            };
            _settings.Accounts.Insert(0, newAcc);
            SelectAccount(newAcc);
        }

        NewNicknameInput.Text = "";
        RefreshAccountsMenu();
        AccountMenuPopup.IsOpen = false;
    }

    private void NewNicknameInput_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            AddNicknameButton_Click(sender, e);
        }
    }

    private void PopupTokenLogin_Click(object sender, RoutedEventArgs e)
    {
        AccountMenuPopup.IsOpen = false;
        OpenTokenLoginDialog();
    }

    private void TopTokenButton_Click(object sender, RoutedEventArgs e) => OpenTokenLoginDialog();

    public void OpenTokenLoginDialog()
    {
        var dialog = new TokenLoginWindow(_settings.GameDirectory)
        {
            Owner = this
        };

        if (dialog.ShowDialog() == true && dialog.ResultAccount != null)
        {
            var newAcc = dialog.ResultAccount;
            _settings.Accounts.RemoveAll(a =>
                a.Id == newAcc.Id ||
                (string.Equals(a.Username, newAcc.Username, StringComparison.OrdinalIgnoreCase) && a.Type == AccountType.LicenseToken));

            _settings.Accounts.Insert(0, newAcc);
            SelectAccount(newAcc);
            RefreshAccountsMenu();
        }
    }

    // ---------------------------------------------------------------
    // Кнопки шапки
    // ---------------------------------------------------------------

    private void OpenFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var versionId = _homePage.CurrentVersionId;
            var loader = _homePage.CurrentLoaderForModrinth;

            var targetDir = versionId is not null
                ? _minecraft.GetInstanceDirectory(versionId, loader ?? "vanilla")
                : _settings.GameDirectory;

            Directory.CreateDirectory(targetDir);
            Process.Start(new ProcessStartInfo(targetDir) { UseShellExecute = true });
        }
        catch { }
    }

    private void LogoButton_Click(object sender, RoutedEventArgs e)
    {
        var url = EasterEggLinks[EasterEggRandom.Next(EasterEggLinks.Length)];
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch { }
    }
}
