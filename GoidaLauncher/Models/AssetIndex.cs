using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class AssetIndex
{
    [JsonPropertyName("objects")]
    public Dictionary<string, AssetObject> Objects { get; set; } = new();
}

public sealed class AssetObject
{
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}
