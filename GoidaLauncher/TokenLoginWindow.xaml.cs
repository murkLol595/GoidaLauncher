using System.Windows;
using System.Windows.Media;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher;

public partial class TokenLoginWindow : Window
{
    private readonly string _gameRootDir;
    public LauncherAccount? ResultAccount { get; private set; }

    public TokenLoginWindow(string gameRootDir)
    {
        InitializeComponent();
        _gameRootDir = gameRootDir;
        SourceInitialized += (_, _) => WindowChromeService.ApplyDarkTitleBar(this);
        Loaded += (_, _) => TokenBox.Focus();
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        var rawToken = TokenBox.Text;
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            ShowStatus("Введите или вставьте токен.", isError: true);
            return;
        }

        SetControlsEnabled(false);
        ShowStatus("⏳ Проверка токена через Mojang API...", isPending: true);

        try
        {
            var authResult = await AuthService.ValidateAndGetProfileAsync(rawToken);

            if (!authResult.Success)
            {
                ShowStatus($"❌ {authResult.ErrorMessage}", isError: true);
                SetControlsEnabled(true);
                return;
            }

            string? downloadedSkin = null;
            if (DownloadSkinCheck.IsChecked == true && !string.IsNullOrWhiteSpace(authResult.SkinUrl))
            {
                ShowStatus($"⏳ Токен подтверждён! Скачивание скина игрока {authResult.Username}...", isPending: true);
                downloadedSkin = await AuthService.DownloadSkinAsync(authResult.SkinUrl, _gameRootDir, authResult.Username!);
            }

            ShowStatus($"✅ Успешно! Авторизован: {authResult.Username}", isSuccess: true);

            ResultAccount = new LauncherAccount
            {
                Username = authResult.Username!,
                Uuid = authResult.Uuid!,
                AccessToken = authResult.AccessToken,
                Type = AccountType.LicenseToken,
                SkinUrl = authResult.SkinUrl,
                ActiveSkinFile = downloadedSkin,
            };

            await Task.Delay(600);
            DialogResult = true;
        }
        catch (Exception ex)
        {
            ShowStatus($"❌ Ошибка: {ex.Message}", isError: true);
            SetControlsEnabled(true);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void SetControlsEnabled(bool enabled)
    {
        LoginButton.IsEnabled = enabled;
        CancelButton.IsEnabled = enabled;
        TokenBox.IsEnabled = enabled;
        DownloadSkinCheck.IsEnabled = enabled;
    }

    private void ShowStatus(string message, bool isError = false, bool isSuccess = false, bool isPending = false)
    {
        StatusBorder.Visibility = Visibility.Visible;
        StatusText.Text = message;

        if (isError)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(45, 20, 20));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(248, 113, 113));
        }
        else if (isSuccess)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(20, 45, 25));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(74, 222, 128));
        }
        else if (isPending)
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(35, 38, 48));
            StatusText.Foreground = new SolidColorBrush(Color.FromRgb(251, 191, 36));
        }
        else
        {
            StatusBorder.Background = new SolidColorBrush(Color.FromRgb(30, 32, 41));
            StatusText.Foreground = (Brush)FindResource("TextPrimary");
        }
    }
}
