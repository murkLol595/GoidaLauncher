using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class FabricLoaderListEntry
{
    [JsonPropertyName("loader")]
    public FabricLoaderInfo Loader { get; set; } = new();
}

public sealed class FabricLoaderInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("stable")]
    public bool Stable { get; set; }
}
