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

        var skins = SkinService.ListSkins(_settings.GameDirectory);
        if (skins.Count == 0)
        {
            SkinsPanel.Children.Add(new TextBlock
            {
                Text = "Пока ни одного скина не загружено.",
                Foreground = (Brush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 20, 0, 0),
            });
        }
        else
        {
            foreach (var fileName in skins)
                SkinsPanel.Children.Add(BuildSkinCard(fileName));
        }

        var activePath = string.IsNullOrWhiteSpace(_settings.ActiveSkinPackFile)
            ? null
            : Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), _settings.ActiveSkinPackFile);
        Viewer3D.SetSkin(activePath);
    }

    private UIElement BuildSkinCard(string fileName)
    {
        bool isActive = string.Equals(_settings.ActiveSkinPackFile, fileName, StringComparison.OrdinalIgnoreCase);
        var path = Path.Combine(SkinService.GetSkinsDirectory(_settings.GameDirectory), fileName);

        var card = new Border
        {
            Width = 150,
            Background = (Brush)FindResource("BgPanel"),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 12, 12),
            Padding = new Thickness(10),
            Effect = (System.Windows.Media.Effects.Effect)FindResource("CardShadow"),
            BorderThickness = new Thickness(isActive ? 2 : 0),
            BorderBrush = (Brush)FindResource("AccentGreen"),
        };

        var stack = new StackPanel();

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit();

            var image = new Image
            {
                Source = bitmap,
                Height = 96,
                Stretch = Stretch.Uniform,
                Margin = new Thickness(0, 0, 0, 8),
            };
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);
            stack.Children.Add(image);
        }
        catch
        {
            stack.Children.Add(new TextBlock
            {
                Text = "(не удалось открыть)",
                Foreground = (Brush)FindResource("TextSecondary"),
                FontSize = 11,
                Margin = new Thickness(0, 0, 0, 8),
            });
        }

        stack.Children.Add(new TextBlock
        {
            Text = Path.GetFileNameWithoutExtension(fileName),
            Foreground = (Brush)FindResource("TextPrimary"),
            FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
            TextTrimming = TextTrimming.CharacterEllipsis,
            Margin = new Thickness(0, 0, 0, 8),
        });

        var buttonsRow = new StackPanel { Orientation = Orientation.Horizontal };

        var selectButton = new Button
        {
            Content = isActive ? "Активен ✓" : "Выбрать",
            Style = (Style)FindResource("SecondaryButton"),
            IsEnabled = !isActive,
            Width = 96,
            Height = 28,
            FontSize = 11,
        };
        selectButton.Click += (_, _) =>
        {
            _settings.ActiveSkinPackFile = fileName;
            SettingsService.Save(_settings);
            RefreshSkinsList();
            SkinChanged?.Invoke();
        };

        var deleteButton = new Button
        {
            Content = "🗑",
            Style = (Style)FindResource("SecondaryButton"),
            Width = 32,
            Height = 28,
            Margin = new Thickness(6, 0, 0, 0),
        };
        deleteButton.Click += (_, _) =>
        {
            var owner = Window.GetWindow(this);
            var confirm = MessageBox.Show(owner, $"Удалить скин «{fileName}»?", "GoidaLauncher",
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

        buttonsRow.Children.Add(selectButton);
        buttonsRow.Children.Add(deleteButton);
        stack.Children.Add(buttonsRow);

        card.Child = stack;
        return card;
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
}
