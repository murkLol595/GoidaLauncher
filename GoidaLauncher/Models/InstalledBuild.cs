namespace GoidaLauncher.Models;

/// <summary>Уже скачанная на диск сборка: версия игры + под каким загрузчиком она стоит.</summary>
public sealed record InstalledBuild(string FolderId, string McVersion, string Loader)
{
    public string LoaderLabel => Loader switch
    {
        "Fabric" => "Fabric",
        "Quilt" => "Quilt",
        "Forge" => "Forge",
        "NeoForge" => "NeoForge",
        _ => "Vanilla",
    };

    public string Display => FolderId.Length == 0
        ? $"{McVersion} ({LoaderLabel}) — будет скачано"
        : $"{McVersion} ({LoaderLabel})";
}
