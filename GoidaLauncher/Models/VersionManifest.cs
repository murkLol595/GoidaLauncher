using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class VersionManifest
{
    [JsonPropertyName("latest")]
    public LatestVersions Latest { get; set; } = new();

    [JsonPropertyName("versions")]
    public List<VersionManifestEntry> Versions { get; set; } = new();
}

public sealed class LatestVersions
{
    [JsonPropertyName("release")]
    public string Release { get; set; } = "";

    [JsonPropertyName("snapshot")]
    public string Snapshot { get; set; } = "";
}

public sealed class VersionManifestEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("releaseTime")]
    public DateTimeOffset ReleaseTime { get; set; }

    /// <summary>Заполняется на стороне UI — не приходит из манифеста Mojang.</summary>
    public bool IsDownloaded { get; set; }

    /// <summary>Технический разделитель для ComboBox ("── Не скачано ──"), не настоящая версия.</summary>
    public bool IsSeparator { get; set; }

    public static VersionManifestEntry Separator(string label) => new() { Id = label, IsSeparator = true };

    public override string ToString() => $"{Id} ({Type})";
}
