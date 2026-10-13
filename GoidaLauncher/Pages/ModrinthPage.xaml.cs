using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GoidaLauncher.Models;
using GoidaLauncher.Services;

namespace GoidaLauncher.Pages;

public partial class ModrinthPage : UserControl
{
    private readonly ModrinthService _modrinth = new();
    private string _gameDirectory = "";
    private string? _gameVersion;
    private string? _loaderId;

    public ModrinthPage()
    {
        InitializeComponent();
    }

    /// <summary>Вызывается оболочкой каждый раз перед показом страницы — подтягивает текущую версию/загрузчик с "Играть".</summary>
    public void UpdateContext(string gameDirectory, string? gameVersion, string? loaderId)
    {
        if (_gameDirectory == gameDirectory && _gameVersion == gameVersion && _loaderId == loaderId)
            return; // ничего не изменилось — не дёргаем поиск заново зря

        _gameDirectory = gameDirectory;
        _gameVersion = gameVersion;
        _loaderId = loaderId;

        ContextText.Text = gameVersion is null
            ? "Версия игры не выбрана — файлы будут скачаны без фильтра по версии."
            : $"Версия: {gameVersion}" + (loaderId is not null ? $" · {loaderId}" : "");

        if (ResultsList.Items.Count == 0)
            _ = SearchAsync(); // первый показ страницы — сразу ищем
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e) => await SearchAsync();

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await SearchAsync();
    }

    private string SelectedProjectType =>
        (ProjectTypeCombo.SelectedItem as ComboBoxItem)?.Tag as string ?? "mod";

    private async Task SearchAsync()
    {
        ResultsList.Items.Clear();
        StatusText.Text = "Ищу...";

        try
        {
            var projectType = SelectedProjectType;
            var loaderFilter = projectType == "mod" ? _loaderId : null;

            var hits = await _modrinth.SearchAsync(SearchBox.Text?.Trim() ?? "", projectType, _gameVersion, loaderFilter);

            if (hits.Count == 0)
            {
                StatusText.Text = "Ничего не найдено.";
                return;
            }

            foreach (var hit in hits)
                ResultsList.Items.Add(BuildResultCard(hit, projectType));

            StatusText.Text = $"Найдено: {hits.Count}";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка поиска: {ex.Message}";
        }
    }

    private UIElement BuildResultCard(ModrinthHit hit, string projectType)
    {
        var border = new Border
        {
            Background = System.Windows.Media.Brushes.Transparent,
            BorderBrush = new System.Windows.Media.SolidColorBrush(
                (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A3D45")!),
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(8),
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textPanel = new StackPanel();
        textPanel.Children.Add(new TextBlock
        {
            Text = hit.Title,
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Foreground = (System.Windows.Media.Brush)FindResource("TextPrimary"),
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = hit.Description,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary"),
            Margin = new Thickness(0, 2, 0, 2),
        });
        textPanel.Children.Add(new TextBlock
        {
            Text = $"⬇ {hit.DownloadsFormatted} скачиваний",
            FontSize = 11,
            Foreground = (System.Windows.Media.Brush)FindResource("TextSecondary"),
        });
        Grid.SetColumn(textPanel, 0);
        grid.Children.Add(textPanel);

        var installButton = new Button
        {
            Content = "Установить",
            Width = 100,
            Height = 30,
            Style = (Style)FindResource("SecondaryButton"),
            VerticalAlignment = VerticalAlignment.Center,
            Tag = hit,
        };
        installButton.Click += async (s, _) => await InstallAsync(hit, projectType, installButton);
        Grid.SetColumn(installButton, 1);
        grid.Children.Add(installButton);

        border.Child = grid;
        return border;
    }

    private async Task InstallAsync(ModrinthHit hit, string projectType, Button button)
    {
        button.IsEnabled = false;
        var originalContent = button.Content;
        button.Content = "...";

        try
        {
            var loaderFilter = projectType == "mod" ? _loaderId : null;
            var file = await _modrinth.GetBestFileAsync(hit.ProjectId, _gameVersion, loaderFilter);

            if (file is null)
            {
                StatusText.Text = $"Нет подходящей версии «{hit.Title}» для выбранной версии игры/загрузчика.";
                button.Content = originalContent;
                button.IsEnabled = true;
                return;
            }

            var subfolder = projectType switch
            {
                "resourcepack" => "resourcepacks",
                "shader" => "shaderpacks",
                _ => "mods",
            };
            var destFolder = Path.Combine(_gameDirectory, subfolder);

            await _modrinth.DownloadToFolderAsync(file, destFolder);

            StatusText.Text = $"«{hit.Title}» установлен в {subfolder}/.";
            button.Content = "Готово ✓";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Ошибка установки «{hit.Title}»: {ex.Message}";
            button.Content = originalContent;
            button.IsEnabled = true;
        }
    }
}
