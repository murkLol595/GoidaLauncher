using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class QuiltLoaderListEntry
{
    [JsonPropertyName("loader")]
    public QuiltLoaderInfo Loader { get; set; } = new();
}

public sealed class QuiltLoaderInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";
}
