using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class ForgePromotions
{
    [JsonPropertyName("promos")]
    public Dictionary<string, string> Promos { get; set; } = new();
}

public sealed class ForgeInstallProfile
{
    [JsonPropertyName("json")]
    public string VersionJsonPath { get; set; } = "";

    [JsonPropertyName("minecraft")]
    public string Minecraft { get; set; } = "";

    [JsonPropertyName("data")]
    public Dictionary<string, ForgeDataEntry> Data { get; set; } = new();

    [JsonPropertyName("processors")]
    public List<ForgeProcessor> Processors { get; set; } = new();

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();
}

public sealed class ForgeDataEntry
{
    [JsonPropertyName("client")]
    public string Client { get; set; } = "";

    [JsonPropertyName("server")]
    public string Server { get; set; } = "";
}

public sealed class ForgeProcessor
{
    [JsonPropertyName("jar")]
    public string Jar { get; set; } = "";

    [JsonPropertyName("classpath")]
    public List<string> Classpath { get; set; } = new();

    [JsonPropertyName("args")]
    public List<string> Args { get; set; } = new();

    [JsonPropertyName("sides")]
    public List<string>? Sides { get; set; }
}
