using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public sealed class VersionDetails
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("mainClass")]
    public string MainClass { get; set; } = "";

    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("assets")]
    public string AssetsIndexId { get; set; } = "";

    [JsonPropertyName("assetIndex")]
    public AssetIndexRef? AssetIndex { get; set; }

    [JsonPropertyName("downloads")]
    public Downloads? Downloads { get; set; }

    [JsonPropertyName("libraries")]
    public List<Library> Libraries { get; set; } = new();

    [JsonPropertyName("minecraftArguments")]
    public string? MinecraftArguments { get; set; }

    [JsonPropertyName("arguments")]
    public ArgumentsSpec? Arguments { get; set; }

    [JsonPropertyName("javaVersion")]
    public JavaVersionSpec? JavaVersion { get; set; }

    [JsonPropertyName("inheritsFrom")]
    public string? InheritsFrom { get; set; }
}

public sealed class JavaVersionSpec
{
    [JsonPropertyName("majorVersion")]
    public int MajorVersion { get; set; } = 8;
}

public sealed class AssetIndexRef
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public sealed class Downloads
{
    [JsonPropertyName("client")]
    public DownloadArtifact? Client { get; set; }
}

public sealed class DownloadArtifact
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("sha1")]
    public string Sha1 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("path")]
    public string? Path { get; set; }
}

public sealed class Library
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("downloads")]
    public LibraryDownloads? Downloads { get; set; }

    [JsonPropertyName("natives")]
    public Dictionary<string, string>? Natives { get; set; }

    [JsonPropertyName("rules")]
    public List<Rule>? Rules { get; set; }

    /// <summary>
    /// Используется библиотеками Fabric/Forge вместо блока "downloads": базовый URL maven-репозитория,
    /// от которого нужно достроить путь по координатам <see cref="Name"/> (group:artifact:version).
    /// </summary>
    [JsonPropertyName("url")]
    public string? MavenRepositoryUrl { get; set; }
}

public sealed class LibraryDownloads
{
    [JsonPropertyName("artifact")]
    public DownloadArtifact? Artifact { get; set; }

    [JsonPropertyName("classifiers")]
    public Dictionary<string, DownloadArtifact>? Classifiers { get; set; }
}

public sealed class Rule
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "allow";

    [JsonPropertyName("os")]
    public OsRule? Os { get; set; }

    [JsonPropertyName("features")]
    public Dictionary<string, bool>? Features { get; set; }
}

public sealed class OsRule
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("arch")]
    public string? Arch { get; set; }
}

public sealed class ArgumentsSpec
{
    [JsonPropertyName("game")]
    public List<JsonElement> Game { get; set; } = new();

    [JsonPropertyName("jvm")]
    public List<JsonElement> Jvm { get; set; } = new();
}
