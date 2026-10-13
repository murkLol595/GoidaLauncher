using System.IO;

namespace GoidaLauncher.Services;

/// <summary>
/// Библиотека скинов (обычные PNG в общей папке skins/) + синхронизация с мод-лоадером
/// CustomSkinLoader (xfl03/MCCustomSkinLoader, есть под Fabric/Forge/Quilt/NeoForge).
///
/// У CustomSkinLoader свой формат, без всяких системных свойств — он ищет файл по
/// строго заданному пути внутри папки игры: CustomSkinLoader/LocalSkin/skins/{Ник}.png
/// (имя файла = точный ник игрока), и по умолчанию сверяет свой конфиг
/// CustomSkinLoader/CustomSkinLoader.json, где LocalSkin должен стоять первым в
/// "loadlist" — иначе мод по умолчанию сперва попробует найти скин через Mojang.
/// Лаунчер сам кладёт файл под нужным именем и пишет такой конфиг перед каждым запуском.
/// </summary>
public static class SkinService
{
    private const string LoadlistJson =
        """
        {
          "loadlist": [
            {
              "name": "LocalSkin",
              "type": "Legacy",
              "checkPNG": false,
              "skin": "LocalSkin/skins/{USERNAME}.png",
              "model": "auto",
              "cape": "LocalSkin/capes/{USERNAME}.png",
              "elytra": "LocalSkin/elytras/{USERNAME}.png"
            }
          ]
        }
        """;

    public static string GetSkinsDirectory(string gameRootDirectory)
    {
        var dir = Path.Combine(gameRootDirectory, "skins");
        Directory.CreateDirectory(dir);
        return dir;
    }

    /// <summary>Копирует выбранный PNG в общую библиотеку скинов. Возвращает имя файла (без пути).</summary>
    public static string ImportSkin(string sourcePngPath, string gameRootDirectory, string skinName)
    {
        var safeName = string.Concat(skinName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "skin";

        var skinsDir = GetSkinsDirectory(gameRootDirectory);
        var fileName = $"{safeName}.png";
        var destPath = Path.Combine(skinsDir, fileName);

        int suffix = 2;
        while (File.Exists(destPath))
        {
            fileName = $"{safeName}-{suffix}.png";
            destPath = Path.Combine(skinsDir, fileName);
            suffix++;
        }

        File.Copy(sourcePngPath, destPath);
        return fileName;
    }

    public static List<string> ListSkins(string gameRootDirectory)
    {
        var dir = GetSkinsDirectory(gameRootDirectory);
        return Directory.GetFiles(dir, "*.png")
            .Select(Path.GetFileName)
            .Where(f => f is not null)
            .Select(f => f!)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static void DeleteSkin(string gameRootDirectory, string fileName)
    {
        var path = Path.Combine(GetSkinsDirectory(gameRootDirectory), fileName);
        if (File.Exists(path)) File.Delete(path);
    }

    /// <summary>
    /// Кладёт активный скин туда, где его ищет CustomSkinLoader (под именем ника игрока),
    /// и гарантирует, что LocalSkin стоит первым в его конфиге. Вызывать перед каждым запуском.
    /// </summary>
    public static void SyncActiveSkinForCustomSkinLoader(
        string gameRootDirectory, string activeSkinFileName, string instanceDirectory, string nickname)
    {
        try
        {
            var sourcePath = Path.Combine(GetSkinsDirectory(gameRootDirectory), activeSkinFileName);
            if (!File.Exists(sourcePath)) return;

            var cslDir = Path.Combine(instanceDirectory, "CustomSkinLoader");
            var localSkinsDir = Path.Combine(cslDir, "LocalSkin", "skins");
            Directory.CreateDirectory(localSkinsDir);

            var safeNickname = string.Concat(nickname.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
            if (string.IsNullOrWhiteSpace(safeNickname)) safeNickname = nickname;

            var destPath = Path.Combine(localSkinsDir, $"{safeNickname}.png");
            if (!File.Exists(destPath) || new FileInfo(destPath).Length != new FileInfo(sourcePath).Length)
                File.Copy(sourcePath, destPath, overwrite: true);

            var configPath = Path.Combine(cslDir, "CustomSkinLoader.json");
            if (!File.Exists(configPath))
                File.WriteAllText(configPath, LoadlistJson);
        }
        catch
        {
            // Не критично — игра просто запустится с обычным скином в этот раз
        }
    }
}
