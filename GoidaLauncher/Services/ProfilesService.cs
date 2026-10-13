using System.IO;
using System.Text.Json;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public static class ProfilesService
{
    private static readonly string ProfilesPath = Path.Combine(SettingsService.LauncherRootDirectory, "profiles.json");

    public static List<GameProfile> Load()
    {
        try
        {
            if (!File.Exists(ProfilesPath))
                return new List<GameProfile>();

            var json = File.ReadAllText(ProfilesPath);
            return JsonSerializer.Deserialize<List<GameProfile>>(json) ?? new List<GameProfile>();
        }
        catch
        {
            return new List<GameProfile>();
        }
    }

    public static void Save(List<GameProfile> profiles)
    {
        Directory.CreateDirectory(SettingsService.LauncherRootDirectory);
        var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ProfilesPath, json);
    }
}
