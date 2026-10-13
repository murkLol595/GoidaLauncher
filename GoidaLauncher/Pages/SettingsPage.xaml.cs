using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher.Pages;

public partial class SettingsPage : UserControl
{
    private static readonly (string Name, string Hex)[] AccentPresets =
    {
        ("Неон Фиолетовый (Millida)", "#7C3AED"),
        ("Электрик Синий", "#2563EB"),
        ("Неоновый Циан", "#06B6D4"),
        ("Изумрудный (Goida)", "#10B981"),
        ("Закатный Розовый", "#EC4899"),
        ("Огненный Красный", "#EF4444"),
        ("Янтарный Неон", "#F59E0B"),
        ("Лавандовый", "#A855F7"),
    };

    private readonly LauncherSettings _settings;
    private string _selectedAccentHex;
    private bool _isInitializing = true;

    public SettingsPage(LauncherSettings settings)
    {
        InitializeComponent();

        _settings = settings;
        _selectedAccentHex = settings.AccentColorHex;

        MinRamBox.Text = settings.MinRamMb.ToString();
        MaxRamBox.Text = settings.MaxRamMb.ToString();
        JavaPathBox.Text = settings.JavaPath ?? "";
        GameDirBox.Text = settings.GameDirectory;
        CustomJvmArgsBox.Text = settings.CustomJvmArgs ?? "";
        DiscordRpcCheck.IsChecked = settings.DiscordRpcEnabled;
        DiscordAppIdBox.Text = settings.DiscordApplicationId ?? "";

        LogoGoidaRadio.IsChecked = !string.Equals(settings.LogoTitle, "MILLIDA", StringComparison.OrdinalIgnoreCase);
        LogoMillidaRadio.IsChecked = !LogoGoidaRadio.IsChecked;

        AnimatedBgCheck.IsChecked = settings.AnimatedBackgroundEnabled;
        GlassmorphismCheck.IsChecked = settings.GlassmorphismEnabled;
        GlowEffectCheck.IsChecked = settings.GlowEffectEnabled;

        if (settings.CornerRadiusValue >= 12) RadiusModernRadio.IsChecked = true;
        else if (settings.CornerRadiusValue is >= 7 and < 12) RadiusClassicRadio.IsChecked = true;
        else RadiusSharpRadio.IsChecked = true;

        BuildThemeCards();
        BuildColorSwatches();

        _isInitializing = false;
    }

    private void BuildThemeCards()
    {
        ThemesPanel.Children.Clear();

        foreach (var preset in ThemeService.Presets)
        {
            bool isSelected = string.Equals(preset.Id, _settings.ThemePreset, StringComparison.OrdinalIgnoreCase);

            var card = new Border
            {
                Width = 145,
                Height = 52,
                Margin = new Thickness(0, 0, 10, 10),
                CornerRadius = new CornerRadius(10),
                Background = new SolidColorBrush(Color.FromArgb(0xCC, 0x16, 0x1A, 0x2C)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                BorderBrush = isSelected
                    ? new SolidColorBrush(preset.PrimaryAccent)
                    : new SolidColorBrush(Color.FromArgb(0x35, 0xFF, 0xFF, 0xFF)),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = preset.Description,
                Tag = preset.Id,
                Padding = new Thickness(10, 8, 10, 8)
            };

            var stack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

            var topRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 3) };

            var dot1 = new System.Windows.Shapes.Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = new SolidColorBrush(preset.PrimaryAccent),
                Margin = new Thickness(0, 0, 4, 0)
            };
            var dot2 = new System.Windows.Shapes.Ellipse
            {
                Width = 11,
                Height = 11,
                Fill = new SolidColorBrush(preset.SecondaryAccent),
                Margin = new Thickness(0, 0, 6, 0)
            };

            var title = new TextBlock
            {
                Text = preset.Name,
                FontSize = 11,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = isSelected ? Brushes.White : (Brush)FindResource("TextPrimary"),
                VerticalAlignment = VerticalAlignment.Center
            };

            topRow.Children.Add(dot1);
            topRow.Children.Add(dot2);
            topRow.Children.Add(title);

            var desc = new TextBlock
            {
                Text = preset.Description,
                FontSize = 9,
                Foreground = (Brush)FindResource("TextSecondary"),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            stack.Children.Add(topRow);
            stack.Children.Add(desc);
            card.Child = stack;

            card.MouseLeftButtonUp += ThemeCard_Click;
            ThemesPanel.Children.Add(card);
        }
    }

    private void ThemeCard_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border b || b.Tag is not string themeId) return;

        var preset = ThemeService.GetPreset(themeId);
        _settings.ThemePreset = preset.Id;
        _settings.AccentColorHex = "#" + preset.PrimaryAccent.ToString()[3..];
        _selectedAccentHex = _settings.AccentColorHex;

        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);

        BuildThemeCards();
        BuildColorSwatches();
    }

    private void BuildColorSwatches()
    {
        ColorSwatchPanel.Children.Clear();

        foreach (var (name, hex) in AccentPresets)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            bool isSelected = string.Equals(hex, _selectedAccentHex, StringComparison.OrdinalIgnoreCase);

            var swatch = new Border
            {
                Width = 38,
                Height = 38,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(19), // Стильные круглые образцы
                Background = new SolidColorBrush(color),
                BorderThickness = new Thickness(isSelected ? 3 : 1),
                BorderBrush = isSelected ? Brushes.White : new SolidColorBrush(Color.FromArgb(0x40, 255, 255, 255)),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = name,
                Tag = hex,
            };

            if (isSelected)
            {
                swatch.Child = new TextBlock
                {
                    Text = "✓",
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            swatch.MouseLeftButtonUp += Swatch_Click;
            ColorSwatchPanel.Children.Add(swatch);
        }
    }

    private void Swatch_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is not Border border || border.Tag is not string hex) return;

        _selectedAccentHex = hex;
        _settings.AccentColorHex = hex;

        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);

        BuildColorSwatches();
    }

    private void LogoRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.LogoTitle = LogoGoidaRadio.IsChecked == true ? "GOIDA" : "MILLIDA";
        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);
    }

    private void RadiusRadio_Checked(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.CornerRadiusValue = RadiusModernRadio.IsChecked == true ? 14 : (RadiusClassicRadio.IsChecked == true ? 8 : 4);
        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);
    }

    private void DesignOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing) return;

        _settings.AnimatedBackgroundEnabled = AnimatedBgCheck.IsChecked == true;
        _settings.GlassmorphismEnabled = GlassmorphismCheck.IsChecked == true;
        _settings.GlowEffectEnabled = GlowEffectCheck.IsChecked == true;

        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);
    }

    private void ResetThemeButton_Click(object sender, RoutedEventArgs e)
    {
        _settings.ThemePreset = "Millida";
        _settings.LogoTitle = "GOIDA";
        _settings.AccentColorHex = "#7C3AED";
        _selectedAccentHex = "#7C3AED";
        _settings.CornerRadiusValue = 14;
        _settings.GlassmorphismEnabled = true;
        _settings.AnimatedBackgroundEnabled = true;
        _settings.GlowEffectEnabled = true;

        _isInitializing = true;
        LogoMillidaRadio.IsChecked = true;
        LogoGoidaRadio.IsChecked = false;
        RadiusModernRadio.IsChecked = true;
        AnimatedBgCheck.IsChecked = true;
        GlassmorphismCheck.IsChecked = true;
        GlowEffectCheck.IsChecked = true;
        _isInitializing = false;

        ThemeService.ApplyTheme(_settings, Window.GetWindow(this) as MainWindow);
        SettingsService.Save(_settings);

        BuildThemeCards();
        BuildColorSwatches();
    }

    private void OpenGameDirButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            System.IO.Directory.CreateDirectory(_settings.GameDirectory);
            Process.Start(new ProcessStartInfo(_settings.GameDirectory) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Не удалось открыть папку: {ex.Message}", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(MinRamBox.Text, out var minRam)) _settings.MinRamMb = minRam;
        if (int.TryParse(MaxRamBox.Text, out var maxRam)) _settings.MaxRamMb = maxRam;

        _settings.JavaPath = string.IsNullOrWhiteSpace(JavaPathBox.Text) ? null : JavaPathBox.Text.Trim();
        _settings.CustomJvmArgs = string.IsNullOrWhiteSpace(CustomJvmArgsBox.Text) ? null : CustomJvmArgsBox.Text.Trim();
        _settings.DiscordRpcEnabled = DiscordRpcCheck.IsChecked == true;
        _settings.DiscordApplicationId = string.IsNullOrWhiteSpace(DiscordAppIdBox.Text) ? null : DiscordAppIdBox.Text.Trim();
        _settings.AccentColorHex = _selectedAccentHex;

        SettingsService.Save(_settings);

        SavedHintText.Text = "✓ Сохранено";
        await Task.Delay(2000);
        if (SavedHintText.Text == "✓ Сохранено") SavedHintText.Text = "";
    }
}
