namespace GoidaLauncher.Models;

/// <summary>
/// Сохранённый профиль запуска — версия игры + загрузчик модов под одним именем,
/// чтобы быстро переключаться между сборками (по аналогии с "инстансами" в Prism Launcher).
/// </summary>
public sealed class GameProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Новый профиль";
    public string VersionId { get; set; } = "";

    /// <summary>"Vanilla", "Fabric" или "Forge".</summary>
    public string ModLoader { get; set; } = "Vanilla";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

    public override string ToString() => Name;
}
