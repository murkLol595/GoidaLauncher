using System.Collections.Concurrent;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher.Pages;

public partial class ModrinthPage : UserControl
{
    private static readonly (string Tag, string Title)[] Categories =
    {
        ("mod", "Mods"),
        ("resourcepack", "Resource Packs"),
        ("datapack", "Data Packs"),
        ("shader", "Shaders"),
        ("modpack", "Modpacks"),
        ("plugin", "Plugins"),
    };

    private static readonly ConcurrentDictionary<string, ImageSource> IconCache = new();

    private readonly ModrinthService _modrinth = new();
    private readonly List<Button> _categoryButtons = new();

    private string _gameDirectory = "";
    private string? _gameVersion;
    private string? _loaderId;

    private string _selectedProjectType = "mod";
    private int _currentPage = 1;
    private const int PageSize = 20;
    private int _totalHits;
    private int _totalPages = 1;
    private bool _isSearching;

    public ModrinthPage()
    {
        InitializeComponent();
        BuildCategoryPills();
    }

    private void BuildCategoryPills()
    {
        CategoryPillsPanel.Children.Clear();
        _categoryButtons.Clear();

        foreach (var (tag, title) in Categories)
        {
            var btn = new Button
            {
                Content = title,
                Tag = tag,
                Cursor = Cursors.Hand,
                Margin = new Thickness(2, 0, 2, 0),
                Padding = new Thickness(14, 6, 14, 6),
                FontSize = 13,
                BorderThickness = new Thickness(1),
            };

            var template = new ControlTemplate(typeof(Button));
            var borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.Name = "border";
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
            borderFactory.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderBrushProperty, new System.Windows.Data.Binding("BorderBrush") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.BorderThicknessProperty, new System.Windows.Data.Binding("BorderThickness") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            borderFactory.SetBinding(Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });

            var contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;
            btn.Template = template;

            btn.Click += (_, _) =>
            {
                if (_selectedProjectType == tag) return;
                _selectedProjectType = tag;
                HighlightCategoryPills();
                _currentPage = 1;
                _ = SearchAsync();
            };

            _categoryButtons.Add(btn);
            CategoryPillsPanel.Children.Add(btn);
        }

        HighlightCategoryPills();
    }

    private void HighlightCategoryPills()
    {
        foreach (var btn in _categoryButtons)
        {
            var tag = btn.Tag as string;
            if (tag == _selectedProjectType)
            {
                btn.Background = new SolidColorBrush(Color.FromRgb(0x16, 0x3E, 0x2D));
                btn.BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                btn.FontWeight = FontWeights.Bold;
            }
            else
            {
                btn.Background = Brushes.Transparent;
                btn.BorderBrush = Brushes.Transparent;
                btn.Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
                btn.FontWeight = FontWeights.SemiBold;
            }
        }
    }

    public void UpdateContext(string gameDirectory, string? gameVersion, string? loaderId)
    {
        if (_gameDirectory == gameDirectory && _gameVersion == gameVersion && _loaderId == loaderId)
            return;

        _gameDirectory = gameDirectory;
        _gameVersion = gameVersion;
        _loaderId = loaderId;

        ContextText.Text = gameVersion is null
            ? "Версия не выбрана"
            : $"Версия: {gameVersion}" + (loaderId is not null ? $" · {loaderId}" : "");

        if (ResultsPanel.Children.Count == 0)
        {
            _currentPage = 1;
            _ = SearchAsync();
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _currentPage = 1;
        await SearchAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            _currentPage = 1;
            await SearchAsync();
        }
    }

    private async void FilterVersionCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            _currentPage = 1;
            await SearchAsync();
        }
    }

    private async void PrevPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage > 1)
        {
            _currentPage--;
            await SearchAsync();
        }
    }

    private async void NextPageButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPage < _totalPages)
        {
            _currentPage++;
            await SearchAsync();
        }
    }

    private async Task SearchAsync()
    {
        if (_isSearching) return;
        _isSearching = true;

        ResultsPanel.Children.Clear();
        StatusText.Text = "Поиск дополнений...";
        SearchButton.IsEnabled = false;

        try
        {
            var projectType = _selectedProjectType;
            var loaderFilter = projectType == "mod" ? _loaderId : null;
            var verFilter = FilterVersionCheck.IsChecked == true ? _gameVersion : null;

            int offset = (_currentPage - 1) * PageSize;
            var query = SearchBox.Text?.Trim() ?? "";

            var response = await _modrinth.SearchAsync(query, projectType, verFilter, loaderFilter, offset, PageSize);

            _totalHits = response.TotalHits;
            _totalPages = Math.Max(1, (int)Math.Ceiling((double)_totalHits / PageSize));

            if (_currentPage > _totalPages)
                _currentPage = _totalPages;

            UpdatePaginationUi();

            if (response.Hits.Count == 0)
            {
                StatusText.Text = "Ничего не найдено по вашему запросу.";
                var emptyNotice = new Border
                {
                    Padding = new Thickness(24),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                var emptyStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "🔍",
                    FontSize = 36,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 0, 0, 8)
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "Ничего не найдено",
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center
                });
                emptyStack.Children.Add(new TextBlock
                {
                    Text = "Попробуйте изменить запрос или снять галочку фильтра по версии игры.",
                    FontSize = 12,
                    Foreground = (Brush)FindResource("TextSecondary"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 4, 0, 0)
                });
                emptyNotice.Child = emptyStack;
                ResultsPanel.Children.Add(emptyNotice);
                return;
            }

            foreach (var hit in response.Hits)
            {
                ResultsPanel.Children.Add(BuildResultCard(hit, projectType));
            }

            int startItem = offset + 1;
            int endItem = Math.Min(offset + response.Hits.Count, _totalHits);
            StatusText.Text = $"Найдено проектов: {_totalHits:N0} (показано {startItem}–{endItem})";
            ResultsScrollViewer.ScrollToTop();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка поиска: {ex.Message}";
        }
        finally
        {
            _isSearching = false;
            SearchButton.IsEnabled = true;
        }
    }

    private void UpdatePaginationUi()
    {
        PageIndicatorText.Text = $"Страница {_currentPage} из {_totalPages}";
        PrevPageButton.IsEnabled = _currentPage > 1;
        NextPageButton.IsEnabled = _currentPage < _totalPages;
    }

    private UIElement BuildResultCard(ModrinthHit hit, string projectType)
    {
        var card = new Border
        {
            Background = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x2A)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x2D, 0x44)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Margin = new Thickness(0, 0, 0, 8),
            Padding = new Thickness(12),
        };

        card.MouseEnter += (_, _) => card.Background = new SolidColorBrush(Color.FromRgb(0x1A, 0x23, 0x38));
        card.MouseLeave += (_, _) => card.Background = new SolidColorBrush(Color.FromRgb(0x14, 0x1A, 0x2A));

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Левая колонка: Иконка мода
        var iconBorder = new Border
        {
            Width = 56,
            Height = 56,
            CornerRadius = new CornerRadius(10),
            Background = new SolidColorBrush(Color.FromRgb(0x1C, 0x24, 0x36)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x47, 0x55, 0x69)),
            BorderThickness = new Thickness(1),
            Margin = new Thickness(0, 0, 14, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        LoadIconAsync(iconBorder, hit.IconUrl, projectType);
        Grid.SetColumn(iconBorder, 0);
        grid.Children.Add(iconBorder);

        // Центральная колонка: Текстовые данные
        var textStack = new StackPanel { VerticalAlignment = VerticalAlignment.Center };

        var headerWrap = new WrapPanel { Orientation = Orientation.Horizontal };
        var titleBlock = new TextBlock
        {
            Text = hit.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 8, 2),
            VerticalAlignment = VerticalAlignment.Center
        };
        headerWrap.Children.Add(titleBlock);

        if (!string.IsNullOrWhiteSpace(hit.Author))
        {
            headerWrap.Children.Add(new TextBlock
            {
                Text = $"от {hit.Author}",
                FontSize = 11,
                Foreground = (Brush)FindResource("TextSecondary"),
                Margin = new Thickness(0, 0, 8, 2),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        if (hit.Categories != null)
        {
            foreach (var cat in hit.Categories.Take(3))
            {
                var badge = new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(0x28, 0x38, 0xBD, 0xF8)),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x40, 0x38, 0xBD, 0xF8)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(6, 1, 6, 1),
                    Margin = new Thickness(0, 0, 4, 2),
                    VerticalAlignment = VerticalAlignment.Center
                };
                badge.Child = new TextBlock
                {
                    Text = cat,
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x7D, 0xD3, 0xFC))
                };
                headerWrap.Children.Add(badge);
            }
        }
        textStack.Children.Add(headerWrap);

        // Описание
        textStack.Children.Add(new TextBlock
        {
            Text = hit.Description,
            FontSize = 12,
            Foreground = (Brush)FindResource("TextSecondary"),
            TextWrapping = TextWrapping.Wrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxHeight = 36,
            Margin = new Thickness(0, 3, 0, 5)
        });

        // Статистика
        var metaStack = new StackPanel { Orientation = Orientation.Horizontal };
        metaStack.Children.Add(new TextBlock
        {
            Text = $"⬇ {hit.DownloadsFormatted} скачиваний",
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8))
        });
        textStack.Children.Add(metaStack);

        Grid.SetColumn(textStack, 1);
        grid.Children.Add(textStack);

        // Правая колонка: Кнопка установки
        var installButton = new Button
        {
            Content = "📥  Установить",
            Width = 118,
            Height = 36,
            Style = (Style)FindResource("GoidaPillButton"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(12, 0, 0, 0),
            Tag = hit
        };
        installButton.Click += async (_, _) => await InstallAsync(hit, projectType, installButton);
        Grid.SetColumn(installButton, 2);
        grid.Children.Add(installButton);

        card.Child = grid;
        return card;
    }

    private static void LoadIconAsync(Border iconContainer, string? iconUrl, string projectType)
    {
        string defaultEmoji = projectType switch
        {
            "resourcepack" => "🎨",
            "shader" => "✨",
            "datapack" => "📦",
            "modpack" => "🗂",
            "plugin" => "⚙️",
            _ => "🧩"
        };

        var fallbackText = new TextBlock
        {
            Text = defaultEmoji,
            FontSize = 24,
            FontFamily = new FontFamily("Segoe UI Emoji, Segoe UI"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        iconContainer.Child = fallbackText;

        if (string.IsNullOrWhiteSpace(iconUrl))
            return;

        if (IconCache.TryGetValue(iconUrl, out var cachedSource))
        {
            iconContainer.Background = new ImageBrush(cachedSource) { Stretch = Stretch.UniformToFill };
            iconContainer.Child = null;
            return;
        }

        if (!Uri.TryCreate(iconUrl, UriKind.Absolute, out var uri))
            return;

        try
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.DecodePixelWidth = 112;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;

            bmp.DownloadCompleted += (_, _) =>
            {
                iconContainer.Dispatcher.Invoke(() =>
                {
                    IconCache[iconUrl] = bmp;
                    iconContainer.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                    iconContainer.Child = null;
                });
            };

            bmp.DownloadFailed += (_, _) => { };
            bmp.DecodeFailed += (_, _) => { };
            bmp.EndInit();

            if (!bmp.IsDownloading && bmp.PixelWidth > 0)
            {
                IconCache[iconUrl] = bmp;
                iconContainer.Background = new ImageBrush(bmp) { Stretch = Stretch.UniformToFill };
                iconContainer.Child = null;
            }
        }
        catch
        {
            // Keep fallback
        }
    }

    private async Task InstallAsync(ModrinthHit hit, string projectType, Button button)
    {
        button.IsEnabled = false;
        var originalContent = button.Content;
        button.Content = "⏳ Скачивание...";

        try
        {
            var loaderFilter = projectType == "mod" ? _loaderId : null;
            var verFilter = FilterVersionCheck.IsChecked == true ? _gameVersion : null;

            var file = await _modrinth.GetBestFileAsync(hit.ProjectId, verFilter, loaderFilter);

            if (file is null)
            {
                StatusText.Text = $"Нет совместимого файла для «{hit.Title}».";
                button.Content = originalContent;
                button.IsEnabled = true;
                return;
            }

            if (projectType == "modpack" || file.Filename.EndsWith(".mrpack", StringComparison.OrdinalIgnoreCase))
            {
                button.Content = "⏳ Загрузка...";
                StatusText.Text = $"Скачивание сборки «{hit.Title}»...";

                var tempDir = Path.Combine(Path.GetTempPath(), "GoidaLauncher_Modpacks");
                Directory.CreateDirectory(tempDir);
                var tempFile = Path.Combine(tempDir, file.Filename);

                try
                {
                    await _modrinth.DownloadFileToPathAsync(file.Url, tempFile);

                    var index = await ModpackService.InstallMrpackAsync(tempFile, _gameDirectory, (status, pct) =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            StatusText.Text = status;
                            button.Content = $"{pct:0}%";
                        });
                    });

                    StatusText.Text = $"✓ Сборка «{hit.Title}» установлена ({index.Files.Count} модов и конфиги)!";
                    button.Content = "✓ Готово";
                    button.BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                    button.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        try { File.Delete(tempFile); } catch { }
                    }
                }
            }
            else
            {
                var subfolder = projectType switch
                {
                    "resourcepack" => "resourcepacks",
                    "shader" => "shaderpacks",
                    "datapack" => "datapacks",
                    "plugin" => "plugins",
                    _ => "mods",
                };

                var destFolder = Path.Combine(_gameDirectory, subfolder);
                await _modrinth.DownloadToFolderAsync(file, destFolder);

                StatusText.Text = $"✓ «{hit.Title}» установлен в {subfolder}/ ({file.Filename}).";
                button.Content = "✓ Готово";
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
                button.Foreground = new SolidColorBrush(Color.FromRgb(0x34, 0xD3, 0x99));
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка установки «{hit.Title}»: {ex.Message}";
            button.Content = originalContent;
            button.IsEnabled = true;
        }
    }
}
