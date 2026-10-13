using System.IO;
using System.Text.Json;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public static class SettingsService
{
    private static readonly string RootDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GoidaLauncher");

    private static readonly string SettingsPath = Path.Combine(RootDir, "settings.json");

    public static string LauncherRootDirectory => RootDir;

    public static LauncherSettings Load()
    {
        try
        {
            Directory.CreateDirectory(RootDir);

            if (!File.Exists(SettingsPath))
            {
                var fresh = new LauncherSettings
                {
                    GameDirectory = Path.Combine(RootDir, "minecraft")
                };
                Save(fresh);
                return fresh;
            }

            var json = File.ReadAllText(SettingsPath);
            var settings = JsonSerializer.Deserialize<LauncherSettings>(json);
            if (settings is null)
                throw new InvalidDataException("Пустой файл настроек");

            if (string.IsNullOrWhiteSpace(settings.GameDirectory))
                settings.GameDirectory = Path.Combine(RootDir, "minecraft");

            if (string.IsNullOrWhiteSpace(settings.ThemePreset))
                settings.ThemePreset = "Goida";

            if (string.IsNullOrWhiteSpace(settings.LogoTitle))
                settings.LogoTitle = "GOIDA";

            if (string.IsNullOrWhiteSpace(settings.AccentColorHex))
                settings.AccentColorHex = "#7C3AED";

            return settings;
        }
        catch
        {
            // Если файл повреждён — начинаем с настроек по умолчанию
            return new LauncherSettings
            {
                GameDirectory = Path.Combine(RootDir, "minecraft")
            };
        }
    }

    public static void Save(LauncherSettings settings)
    {
        Directory.CreateDirectory(RootDir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }
}
