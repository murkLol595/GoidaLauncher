using System.Diagnostics;
using System.IO;
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
        ("Неон Фиолетовый", "#7C3AED"),
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

        // Память
        MinRamBox.Text = settings.MinRamMb.ToString();
        MaxRamBox.Text = settings.MaxRamMb.ToString();

        int currentMaxRam = settings.MaxRamMb > 0 ? settings.MaxRamMb : 2048;
        if (currentMaxRam < 1024) currentMaxRam = 1024;
        if (currentMaxRam > (int)RamSlider.Maximum)
        {
            RamSlider.Maximum = Math.Max(16384, ((currentMaxRam + 2047) / 2048) * 2048);
        }
        RamSlider.Value = currentMaxRam;
        UpdateRamDisplay(currentMaxRam);

        // Пути и аргументы
        JavaPathBox.Text = settings.JavaPath ?? "";
        GameDirBox.Text = settings.GameDirectory;
        CustomJvmArgsBox.Text = settings.CustomJvmArgs ?? "";
        DiscordRpcCheck.IsChecked = settings.DiscordRpcEnabled;
        DiscordAppIdBox.Text = settings.DiscordApplicationId ?? "";

        // Разрешение окна
        SelectResolutionItem(settings.WindowWidth, settings.WindowHeight);

        // Брендинг и эффекты
        LogoGoidaRadio.IsChecked = !string.Equals(settings.LogoTitle, "GOIDA CLASSIC", StringComparison.OrdinalIgnoreCase);
        LogoClassicRadio.IsChecked = !LogoGoidaRadio.IsChecked;

        AnimatedBgCheck.IsChecked = settings.AnimatedBackgroundEnabled;
        GlassmorphismCheck.IsChecked = settings.GlassmorphismEnabled;
        GlowEffectCheck.IsChecked = settings.GlowEffectEnabled;

        // Оптимизация игры и лаунчера
        GameOptimizationCheck.IsChecked = settings.GameOptimizationEnabled;
        HighPriorityCheck.IsChecked = settings.HighProcessPriority;
        ForceDiscreteGpuCheck.IsChecked = settings.ForceDiscreteGpu;
        PauseLauncherCheck.IsChecked = settings.PauseLauncherOnGameLaunch;
        LowMemoryModeCheck.IsChecked = settings.LowMemoryMode;

        if (settings.CornerRadiusValue >= 12) RadiusModernRadio.IsChecked = true;
        else if (settings.CornerRadiusValue is >= 7 and < 12) RadiusClassicRadio.IsChecked = true;
        else RadiusSharpRadio.IsChecked = true;

        BuildThemeCards();
        BuildColorSwatches();

        _isInitializing = false;
    }

    private void SelectResolutionItem(string? width, string? height)
    {
        string targetTag = $"{width}x{height}";
        foreach (var item in ResolutionCombo.Items)
        {
            if (item is ComboBoxItem cbi && string.Equals(cbi.Tag as string, targetTag, StringComparison.OrdinalIgnoreCase))
            {
                ResolutionCombo.SelectedItem = cbi;
                return;
            }
        }
        ResolutionCombo.SelectedIndex = 0;
    }

    private void RamSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing) return;
        int val = (int)Math.Round(RamSlider.Value);
        _isInitializing = true;
        MaxRamBox.Text = val.ToString();
        _isInitializing = false;
        UpdateRamDisplay(val);
    }

    private void MaxRamBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isInitializing) return;
        if (int.TryParse(MaxRamBox.Text, out int val) && val >= 512)
        {
            _isInitializing = true;
            if (val > (int)RamSlider.Maximum)
            {
                RamSlider.Maximum = Math.Max(16384, ((val + 2047) / 2048) * 2048);
            }
            RamSlider.Value = Math.Min(val, RamSlider.Maximum);
            _isInitializing = false;
            UpdateRamDisplay(val);
        }
    }

    private void RamPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string mbStr && int.TryParse(mbStr, out int mb))
        {
            if (mb > (int)RamSlider.Maximum) RamSlider.Maximum = mb;
            RamSlider.Value = mb;
            MaxRamBox.Text = mb.ToString();
            UpdateRamDisplay(mb);
        }
    }

    private void UpdateRamDisplay(int mb)
    {
        double gb = mb / 1024.0;
        RamDisplayLabel.Text = $"{mb} МБ ({gb:0.#} ГБ)";
    }

    private void BrowseJavaButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Выберите исполняемый файл Java (javaw.exe)",
            Filter = "Java (javaw.exe;java.exe)|javaw.exe;java.exe|Исполняемые файлы (*.exe)|*.exe|Все файлы (*.*)|*.*",
            CheckFileExists = true
        };

        if (dlg.ShowDialog(Window.GetWindow(this)) == true)
        {
            JavaPathBox.Text = dlg.FileName;
        }
    }

    private void BrowseGameDirButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Выберите рабочую папку Minecraft",
            InitialDirectory = Directory.Exists(GameDirBox.Text) ? GameDirBox.Text : null
        };

        if (dlg.ShowDialog(Window.GetWindow(this)) == true)
        {
            GameDirBox.Text = dlg.FolderName;
        }
    }

    private void BuildThemeCards()
    {
        ThemesPanel.Children.Clear();

        foreach (var preset in ThemeService.Presets)
        {
            bool isSelected = string.Equals(preset.Id, _settings.ThemePreset, StringComparison.OrdinalIgnoreCase);

            var card = new Border
            {
                Width = 150,
                Height = 54,
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
                Width = 36,
                Height = 36,
                Margin = new Thickness(0, 0, 8, 8),
                CornerRadius = new CornerRadius(18),
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
                    FontSize = 13,
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

        _settings.LogoTitle = LogoGoidaRadio.IsChecked == true ? "GOIDA" : "GOIDA CLASSIC";
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
        _settings.ThemePreset = "Goida";
        _settings.LogoTitle = "GOIDA";
        _settings.AccentColorHex = "#7C3AED";
        _selectedAccentHex = "#7C3AED";
        _settings.CornerRadiusValue = 14;
        _settings.GlassmorphismEnabled = true;
        _settings.AnimatedBackgroundEnabled = true;
        _settings.GlowEffectEnabled = true;

        _isInitializing = true;
        LogoClassicRadio.IsChecked = false;
        LogoGoidaRadio.IsChecked = true;
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
            Directory.CreateDirectory(_settings.GameDirectory);
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
        _settings.GameDirectory = string.IsNullOrWhiteSpace(GameDirBox.Text) ? _settings.GameDirectory : GameDirBox.Text.Trim();
        _settings.CustomJvmArgs = string.IsNullOrWhiteSpace(CustomJvmArgsBox.Text) ? null : CustomJvmArgsBox.Text.Trim();
        _settings.DiscordRpcEnabled = DiscordRpcCheck.IsChecked == true;
        _settings.DiscordApplicationId = string.IsNullOrWhiteSpace(DiscordAppIdBox.Text) ? null : DiscordAppIdBox.Text.Trim();
        _settings.AccentColorHex = _selectedAccentHex;

        // Сохранение настроек оптимизации
        _settings.GameOptimizationEnabled = GameOptimizationCheck.IsChecked == true;
        _settings.HighProcessPriority = HighPriorityCheck.IsChecked == true;
        _settings.ForceDiscreteGpu = ForceDiscreteGpuCheck.IsChecked == true;
        _settings.PauseLauncherOnGameLaunch = PauseLauncherCheck.IsChecked == true;
        _settings.LowMemoryMode = LowMemoryModeCheck.IsChecked == true;

        if (ResolutionCombo.SelectedItem is ComboBoxItem cbi && cbi.Tag is string resTag)
        {
            var parts = resTag.Split('x');
            if (parts.Length == 2)
            {
                _settings.WindowWidth = parts[0];
                _settings.WindowHeight = parts[1];
            }
        }

        SettingsService.Save(_settings);

        SavedToastBorder.Visibility = Visibility.Visible;
        await Task.Delay(2500);
        SavedToastBorder.Visibility = Visibility.Collapsed;
    }

    private void AutoOptimizeButton_Click(object sender, RoutedEventArgs e)
    {
        int totalMb = MemoryOptimizer.GetTotalPhysicalMemoryMb();
        var (minMb, maxMb) = MemoryOptimizer.GetRecommendedRamPreset();

        _settings.MinRamMb = minMb;
        _settings.MaxRamMb = maxMb;
        _settings.GameOptimizationEnabled = true;
        _settings.HighProcessPriority = true;
        _settings.ForceDiscreteGpu = true;
        _settings.PauseLauncherOnGameLaunch = true;

        _isInitializing = true;
        MinRamBox.Text = minMb.ToString();
        MaxRamBox.Text = maxMb.ToString();
        if (maxMb > (int)RamSlider.Maximum) RamSlider.Maximum = maxMb;
        RamSlider.Value = maxMb;
        UpdateRamDisplay(maxMb);

        GameOptimizationCheck.IsChecked = true;
        HighPriorityCheck.IsChecked = true;
        ForceDiscreteGpuCheck.IsChecked = true;
        PauseLauncherCheck.IsChecked = true;
        _isInitializing = false;

        SettingsService.Save(_settings);

        MessageBox.Show(Window.GetWindow(this),
            $"🚀 Авто-оптимизация успешно применена!\n\n" +
            $"• ОЗУ в системе: {totalMb:N0} МБ (~{totalMb / 1024.0:0.#} ГБ)\n" +
            $"• Выделено игре: {maxMb} МБ (оптимальный предел без пауз GC)\n" +
            $"• Включены Aikar's Flags (параллельный сборщик мусора G1GC)\n" +
            $"• Включён высокий приоритет процесса Minecraft\n" +
            $"• Активирован принудительный запуск на мощной видеокарте\n" +
            $"• Включено энергосбережение лаунчера во время игры",
            "Goida Launcher — Оптимизация",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void TrimMemoryButton_Click(object sender, RoutedEventArgs e)
    {
        long beforeBytes = Process.GetCurrentProcess().WorkingSet64;
        MemoryOptimizer.TrimProcessMemory();
        long afterBytes = Process.GetCurrentProcess().WorkingSet64;

        double beforeMb = beforeBytes / (1024.0 * 1024.0);
        double afterMb = afterBytes / (1024.0 * 1024.0);

        MessageBox.Show(Window.GetWindow(this),
            $"🧹 Память лаунчера успешно очищена!\n\n" +
            $"До очистки: {beforeMb:0.#} МБ\n" +
            $"После очистки: {afterMb:0.#} МБ\n" +
            $"Освобождено: {Math.Max(0, beforeMb - afterMb):0.#} МБ физической памяти.",
            "Goida Launcher",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }
}
