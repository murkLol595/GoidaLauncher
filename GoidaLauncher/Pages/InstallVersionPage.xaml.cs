using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher.Pages;

public partial class InstallVersionPage : UserControl
{
    private static readonly (string Id, string Name, string Icon, string Subtitle)[] Loaders =
    {
        ("Fabric", "Fabric", "⚡", "косметика / моды"),
        ("Quilt", "Quilt", "🟣", "быстрый загрузчик"),
        ("Forge", "Forge", "⚒", "классические моды"),
        ("NeoForge", "NeoForge", "🔥", "современный Forge"),
        ("Vanilla", "Vanilla", "🟩", "чистый Minecraft"),
    };

    public event Action<string, string>? VersionChosen;

    private readonly MinecraftService _minecraft;
    private VersionManifest? _manifest;
    private string _selectedLoader = "Fabric";
    private string _selectedVersion = "";
    private Border? _activeLoaderCard;
    private Button? _activeVersionButton;

    public InstallVersionPage(MinecraftService minecraft)
    {
        InitializeComponent();
        _minecraft = minecraft;

        BuildLoaderCards();

        Loaded += async (_, _) =>
        {
            if (_manifest is null)
                await LoadAsync();
        };
    }

    private void BuildLoaderCards()
    {
        LoaderGrid.Children.Clear();

        foreach (var (id, name, icon, subtitle) in Loaders)
        {
            var card = new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1E, 0x30)),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x3E, 0x4E, 0x7E)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Margin = new Thickness(4),
                Padding = new Thickness(10, 12, 10, 12),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = id,
            };

            var stack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };

            var iconText = new TextBlock
            {
                Text = icon,
                FontSize = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 4)
            };

            var nameText = new TextBlock
            {
                Text = name,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimary"),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var subText = new TextBlock
            {
                Text = subtitle,
                FontSize = 10,
                Foreground = (Brush)FindResource("TextSecondary"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 2, 0, 0)
            };

            stack.Children.Add(iconText);
            stack.Children.Add(nameText);
            stack.Children.Add(subText);
            card.Child = stack;

            card.MouseLeftButtonUp += (_, _) =>
            {
                _selectedLoader = id;
                HighlightActiveLoader(card);
                RefreshVersionList();
            };

            if (id == _selectedLoader)
            {
                _activeLoaderCard = card;
            }

            LoaderGrid.Children.Add(card);
        }

        if (_activeLoaderCard != null)
        {
            HighlightActiveLoader(_activeLoaderCard);
        }
    }

    private void HighlightActiveLoader(Border card)
    {
        _activeLoaderCard = card;
        var accent = (Brush)FindResource("AccentGreen");

        foreach (var child in LoaderGrid.Children)
        {
            if (child is not Border b) continue;
            if (b == _activeLoaderCard)
            {
                b.BorderBrush = accent;
                b.BorderThickness = new Thickness(2);
                b.Background = new SolidColorBrush(Color.FromArgb(0x28, 0x25, 0x63, 0xEB));
            }
            else
            {
                b.BorderBrush = new SolidColorBrush(Color.FromArgb(0x60, 0x3E, 0x4E, 0x7E));
                b.BorderThickness = new Thickness(1);
                b.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x1E, 0x30));
            }
        }
    }

    private async Task LoadAsync()
    {
        StatusText.Text = "Загружаю список версий Minecraft...";
        try
        {
            _manifest = await _minecraft.GetVersionManifestAsync();
            if (_manifest.Versions.Count > 0 && string.IsNullOrWhiteSpace(_selectedVersion))
            {
                var latestRel = _manifest.Versions.FirstOrDefault(v => v.Type == "release")?.Id;
                _selectedVersion = latestRel ?? _manifest.Latest.Release;
            }

            RefreshVersionList();
            StatusText.Text = "Готов к установке";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка: {ex.Message}";
        }
    }

    private void ShowSnapshotsCheck_Changed(object sender, RoutedEventArgs e) => RefreshVersionList();

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshVersionList();

    private void RefreshVersionList()
    {
        VersionWrapPanel.Children.Clear();
        if (_manifest is null) return;

        bool showSnapshots = ShowSnapshotsCheck.IsChecked == true;
        string filter = SearchBox.Text.Trim();

        var items = _manifest.Versions
            .Where(v => showSnapshots || v.Type == "release")
            .Where(v => string.IsNullOrWhiteSpace(filter) || v.Id.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var accent = (Brush)FindResource("AccentGreen");

        foreach (var v in items)
        {
            bool downloaded = _minecraft.IsVersionDownloadedForLoader(v.Id, _selectedLoader);
            bool isSelected = string.Equals(v.Id, _selectedVersion, StringComparison.OrdinalIgnoreCase);

            var pill = new Button
            {
                Width = 100,
                Height = 38,
                Margin = new Thickness(0, 0, 8, 8),
                Tag = v.Id,
                Style = (Style)FindResource("GoidaPillButton")
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (downloaded)
            {
                stack.Children.Add(new TextBlock
                {
                    Text = "✓ ",
                    Foreground = accent,
                    FontWeight = FontWeights.Bold,
                    FontSize = 11
                });
            }

            stack.Children.Add(new TextBlock
            {
                Text = v.Id,
                FontSize = 12,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.SemiBold,
                Foreground = isSelected ? (Brush)new SolidColorBrush(Colors.White) : (Brush)FindResource("TextPrimary")
            });

            pill.Content = stack;

            if (isSelected)
            {
                pill.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x25, 0x63, 0xEB));
                pill.BorderBrush = accent;
                pill.BorderThickness = new Thickness(2);
                _activeVersionButton = pill;
            }

            pill.Click += (_, _) =>
            {
                _selectedVersion = v.Id;
                SelectVersionPill(pill);
                UpdateSummary();
            };

            pill.MouseDoubleClick += (_, _) =>
            {
                _selectedVersion = v.Id;
                ConfirmInstallButton_Click(this, new RoutedEventArgs());
            };

            VersionWrapPanel.Children.Add(pill);
        }

        UpdateSummary();
    }

    private void SelectVersionPill(Button pill)
    {
        var accent = (Brush)FindResource("AccentGreen");

        if (_activeVersionButton != null)
        {
            _activeVersionButton.Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x23, 0x37));
            _activeVersionButton.BorderBrush = new SolidColorBrush(Color.FromRgb(0x2A, 0x37, 0x56));
            _activeVersionButton.BorderThickness = new Thickness(1);
        }

        pill.Background = new SolidColorBrush(Color.FromArgb(0x40, 0x25, 0x63, 0xEB));
        pill.BorderBrush = accent;
        pill.BorderThickness = new Thickness(2);
        _activeVersionButton = pill;
    }

    private void UpdateSummary()
    {
        SelectedSummaryText.Text = $"Выбрано: {_selectedVersion} ({_selectedLoader})";
    }

    private void ConfirmInstallButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedVersion))
        {
            MessageBox.Show(Window.GetWindow(this), "Выберите версию из списка.", "GoidaLauncher",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        VersionChosen?.Invoke(_selectedVersion, _selectedLoader);
    }
}
