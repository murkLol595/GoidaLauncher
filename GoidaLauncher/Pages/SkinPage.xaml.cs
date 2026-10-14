using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoidaLauncher.Models;
using GoidaLauncher.Services;
using Microsoft.Win32;

namespace GoidaLauncher.Pages;

public partial class SkinPage : UserControl
{
    private readonly LauncherSettings _settings;
    public event Action? SkinChanged;

    public SkinPage(LauncherSettings settings)
    {
        InitializeComponent();
        _settings = settings;
        Loaded += (_, _) => RefreshSkinsList();
    }

    private void RefreshSkinsList()
    {
        SkinsPanel.Children.Clear();

        // 1. Первая карточка — "+ Новый скин" (как в официальном лаунчере)
        SkinsPanel.Children.Add(BuildNewSkinCard());

        // 2. Пользовательские скины из библиотеки (без стандартных Mojang Стива/Алекс)
        var skins = SkinService.ListSkins(_settings.GameDirectory);
        foreach (var fileName in skins)
        {
            SkinsPanel.Children.Add(BuildSkinCard(fileName));
        }

        // Обновляем 3D просмотр активного скина
        var activePath = string.IsNullOrWhiteSpace(_settings.ActiveSkinPackFile)
            ? null
            : Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), _settings.ActiveSkinPackFile);

        Viewer3D.SetSkin(activePath);
    }

    private UIElement BuildNewSkinCard()
    {
        var card = new Border
        {
            Width = 135,
            Height = 225,
            Background = new SolidColorBrush(Color.FromArgb(0x35, 0x16, 0x20, 0x33)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x3E, 0x54, 0x7E)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(10),
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Загрузить новый скин с компьютера (.png)"
        };

        var stack = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        var circle = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(28),
            Background = new SolidColorBrush(Color.FromArgb(0x25, 0xFF, 0xFF, 0xFF)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x50, 0xFF, 0xFF, 0xFF)),
            BorderThickness = new Thickness(1.5),
            Margin = new Thickness(0, 0, 0, 12),
            Child = new TextBlock
            {
                Text = "+",
                FontSize = 30,
                FontWeight = FontWeights.Light,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -2, 0, 0)
            }
        };

        var label = new TextBlock
        {
            Text = "Новый скин",
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("TextPrimary"),
            HorizontalAlignment = HorizontalAlignment.Center
        };

        stack.Children.Add(circle);
        stack.Children.Add(label);
        card.Child = stack;

        card.MouseEnter += (_, _) =>
        {
            card.Background = new SolidColorBrush(Color.FromArgb(0x60, 0x22, 0x30, 0x4D));
            card.BorderBrush = (Brush)FindResource("AccentGreen");
        };
        card.MouseLeave += (_, _) =>
        {
            card.Background = new SolidColorBrush(Color.FromArgb(0x35, 0x16, 0x20, 0x33));
            card.BorderBrush = new SolidColorBrush(Color.FromArgb(0x55, 0x3E, 0x54, 0x7E));
        };

        card.MouseLeftButtonUp += (_, _) => UploadSkinButton_Click(this, new RoutedEventArgs());
        return card;
    }

    private UIElement BuildSkinCard(string fileName)
    {
        bool isActive = string.Equals(_settings.ActiveSkinPackFile, fileName, StringComparison.OrdinalIgnoreCase);
        var path = Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), fileName);

        var card = new Border
        {
            Width = 135,
            Height = 225,
            Background = (Brush)FindResource("BgPanel"),
            CornerRadius = new CornerRadius(12),
            Margin = new Thickness(10),
            Padding = new Thickness(10, 8, 10, 8),
            Effect = (System.Windows.Media.Effects.Effect)FindResource("CardShadow"),
            BorderThickness = new Thickness(isActive ? 2 : 1),
            BorderBrush = isActive ? (Brush)FindResource("AccentGreen") : (Brush)FindResource("PanelBorderBrush"),
            Cursor = System.Windows.Input.Cursors.Hand
        };

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Заголовок имени
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Моделька
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Кнопки

        // 1. Имя скина
        var title = new TextBlock
        {
            Text = Path.GetFileNameWithoutExtension(fileName),
            FontSize = 12,
            FontWeight = isActive ? FontWeights.Bold : FontWeights.SemiBold,
            Foreground = isActive ? Brushes.White : (Brush)FindResource("TextPrimary"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6)
        };
        Grid.SetRow(title, 0);
        grid.Children.Add(title);

        // 2. Фронтальный рендер модельки (как в официальном лаунчере)
        try
        {
            var preview = CreateSkinFrontPreview(path);
            var img = new Image
            {
                Source = preview,
                Width = 72,
                Height = 144,
                Stretch = Stretch.Uniform,
                SnapsToDevicePixels = true,
                UseLayoutRounding = true,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
            Grid.SetRow(img, 1);
            grid.Children.Add(img);
        }
        catch
        {
            var err = new TextBlock
            {
                Text = "(ошибка)",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetRow(err, 1);
            grid.Children.Add(err);
        }

        // 3. Нижний ряд кнопок
        var actions = new Grid { Margin = new Thickness(0, 6, 0, 0) };
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        actions.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var selectBtn = new Button
        {
            Content = isActive ? "✓ Выбран" : "Выбрать",
            Style = (Style)FindResource("SecondaryButton"),
            Height = 26,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            IsEnabled = !isActive,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        if (isActive)
        {
            selectBtn.Background = (Brush)FindResource("AccentGreen");
            selectBtn.Foreground = Brushes.White;
        }
        selectBtn.Click += (_, _) =>
        {
            _settings.ActiveSkinPackFile = fileName;
            SettingsService.Save(_settings);
            RefreshSkinsList();
            SkinChanged?.Invoke();
        };
        Grid.SetColumn(selectBtn, 0);
        actions.Children.Add(selectBtn);

        var delBtn = new Button
        {
            Content = "🗑",
            Style = (Style)FindResource("SecondaryButton"),
            Width = 26,
            Height = 26,
            FontSize = 11,
            Margin = new Thickness(6, 0, 0, 0),
            ToolTip = "Удалить скин из библиотеки"
        };
        delBtn.Click += (_, _) =>
        {
            var owner = Window.GetWindow(this);
            var confirm = MessageBox.Show(owner, $"Удалить скин «{Path.GetFileNameWithoutExtension(fileName)}»?", "GoidaLauncher",
                MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirm != MessageBoxResult.Yes) return;

            SkinService.DeleteSkin(_settings.GameDirectory, fileName);
            if (isActive)
            {
                _settings.ActiveSkinPackFile = null;
                SettingsService.Save(_settings);
            }
            RefreshSkinsList();
            SkinChanged?.Invoke();
        };
        Grid.SetColumn(delBtn, 1);
        actions.Children.Add(delBtn);

        Grid.SetRow(actions, 2);
        grid.Children.Add(actions);

        card.Child = grid;
        return card;
    }

    /// <summary>
    /// Собирает 100% чёткий фронтальный пиксель-арт рендер модельки игрока напрямую в память (WriteableBitmap)
    /// без интерполяции, мыла или билинейной фильтрации.
    /// </summary>
    private static ImageSource CreateSkinFrontPreview(string skinPngPath)
    {
        var srcBitmap = new BitmapImage();
        srcBitmap.BeginInit();
        srcBitmap.CacheOption = BitmapCacheOption.OnLoad;
        srcBitmap.UriSource = new Uri(skinPngPath, UriKind.Absolute);
        srcBitmap.EndInit();

        var formatted = new FormatConvertedBitmap(srcBitmap, PixelFormats.Bgra32, null, 0);
        int w = formatted.PixelWidth;
        int h = formatted.PixelHeight;
        if (w < 64 || (h != 32 && h != 64)) return srcBitmap;

        uint[] skin = new uint[w * h];
        formatted.CopyPixels(skin, w * 4, 0);

        // Пропорции фронтального скина Minecraft: 16x32 пикселя
        // Масштабируем 8x -> 128x256 для идеальной четкости
        const int scale = 8;
        const int targetW = 16 * scale; // 128
        const int targetH = 32 * scale; // 256
        uint[] target = new uint[targetW * targetH];

        void Blit(int sx, int sy, int sw, int sh, int dx, int dy, bool mirror = false)
        {
            for (int y = 0; y < sh; y++)
            {
                int srcY = sy + y;
                if (srcY >= h) continue;

                for (int x = 0; x < sw; x++)
                {
                    int srcX = mirror ? (sx + sw - 1 - x) : (sx + x);
                    if (srcX >= w) continue;

                    uint color = skin[srcY * w + srcX];
                    byte a = (byte)((color >> 24) & 0xFF);
                    if (a < 10) continue; // Прозрачный фон

                    for (int py = 0; py < scale; py++)
                    {
                        int targetY = (dy + y) * scale + py;
                        if (targetY >= targetH) continue;

                        for (int px = 0; px < scale; px++)
                        {
                            int targetX = (dx + x) * scale + px;
                            if (targetX >= targetW) continue;

                            int idx = targetY * targetW + targetX;
                            if (a >= 250)
                            {
                                target[idx] = color;
                            }
                            else
                            {
                                uint bg = target[idx];
                                byte bgA = (byte)((bg >> 24) & 0xFF);
                                if (bgA == 0)
                                {
                                    target[idx] = color;
                                }
                                else
                                {
                                    float alpha = a / 255f;
                                    byte r = (byte)(((color >> 16) & 0xFF) * alpha + ((bg >> 16) & 0xFF) * (1 - alpha));
                                    byte g = (byte)(((color >> 8) & 0xFF) * alpha + ((bg >> 8) & 0xFF) * (1 - alpha));
                                    byte b = (byte)((color & 0xFF) * alpha + (bg & 0xFF) * (1 - alpha));
                                    target[idx] = (0xFFu << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 1. Голова (базовый слой)
        Blit(8, 8, 8, 8, 4, 0);
        // Голова (оверлей / волосы / шлем)
        Blit(40, 8, 8, 8, 4, 0);

        // 2. Тело (базовый слой)
        Blit(20, 20, 8, 12, 4, 8);
        if (h >= 64) Blit(20, 36, 8, 12, 4, 8); // Тело (оверлей)

        // 3. Правая рука
        Blit(44, 20, 4, 12, 0, 8);
        if (h >= 64) Blit(44, 36, 4, 12, 0, 8);

        // 4. Левая рука
        if (h >= 64)
        {
            Blit(36, 52, 4, 12, 12, 8);
            Blit(52, 52, 4, 12, 12, 8);
        }
        else
        {
            Blit(44, 20, 4, 12, 12, 8, mirror: true);
        }

        // 5. Правая нога
        Blit(4, 20, 4, 12, 4, 20);
        if (h >= 64) Blit(4, 36, 4, 12, 4, 20);

        // 6. Левая нога
        if (h >= 64)
        {
            Blit(20, 52, 4, 12, 8, 20);
            Blit(4, 52, 4, 12, 8, 20);
        }
        else
        {
            Blit(4, 20, 4, 12, 8, 20, mirror: true);
        }

        var wb = new WriteableBitmap(targetW, targetH, 96, 96, PixelFormats.Bgra32, null);
        wb.WritePixels(new Int32Rect(0, 0, targetW, targetH), target, targetW * 4, 0);
        wb.Freeze();
        return wb;
    }

    private void UploadSkinButton_Click(object sender, RoutedEventArgs e)
    {
        var owner = Window.GetWindow(this);

        var dialog = new OpenFileDialog
        {
            Title = "Выберите PNG со скином (64×64 или 64×32)",
            Filter = "PNG-изображения (*.png)|*.png",
        };
        if (dialog.ShowDialog(owner) != true) return;

        var nameDialog = new TextInputWindow("Название скина:", Path.GetFileNameWithoutExtension(dialog.FileName)) { Owner = owner };
        if (nameDialog.ShowDialog() != true) return;

        try
        {
            var fileName = SkinService.ImportSkin(dialog.FileName, _settings.GameDirectory, nameDialog.Value);

            // Первый загруженный скин сразу делаем активным для удобства
            if (string.IsNullOrWhiteSpace(_settings.ActiveSkinPackFile))
            {
                _settings.ActiveSkinPackFile = fileName;
                SettingsService.Save(_settings);
            }

            RefreshSkinsList();
            SkinChanged?.Invoke();
        }
        catch (Exception ex)
        {
            MessageBox.Show(owner, $"Не удалось загрузить скин: {ex.Message}", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OpenSkinsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = SkinService.GetSkinsDirectory(_settings.GameDirectory);
            Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo(dir) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(Window.GetWindow(this), $"Не удалось открыть папку: {ex.Message}", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }
}
