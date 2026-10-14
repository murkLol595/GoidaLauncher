using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class ModrinthSearchResponse
{
    [JsonPropertyName("hits")]
    public List<ModrinthHit> Hits { get; set; } = new();

    [JsonPropertyName("offset")]
    public int Offset { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; }

    [JsonPropertyName("total_hits")]
    public int TotalHits { get; set; }
}

public sealed class ModrinthHit
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = "";

    [JsonPropertyName("slug")]
    public string Slug { get; set; } = "";

    [JsonPropertyName("title")]
    public string Title { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("project_type")]
    public string ProjectType { get; set; } = "";

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; }

    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }

    [JsonPropertyName("author")]
    public string? Author { get; set; }

    [JsonPropertyName("categories")]
    public List<string>? Categories { get; set; }

    public string DownloadsFormatted =>
        Downloads >= 1_000_000 ? $"{Downloads / 1_000_000.0:0.#}M" :
        Downloads >= 1_000 ? $"{Downloads / 1_000.0:0.#}K" : Downloads.ToString();
}

public sealed class ModrinthVersion
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("version_number")]
    public string VersionNumber { get; set; } = "";

    [JsonPropertyName("game_versions")]
    public List<string> GameVersions { get; set; } = new();

    [JsonPropertyName("loaders")]
    public List<string> Loaders { get; set; } = new();

    [JsonPropertyName("files")]
    public List<ModrinthVersionFile> Files { get; set; } = new();
}

public sealed class ModrinthVersionFile
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("filename")]
    public string Filename { get; set; } = "";

    [JsonPropertyName("primary")]
    public bool Primary { get; set; }
}
