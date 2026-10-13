using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

/// <summary>
/// Интеграция с Quilt Meta API (meta.quiltmc.org) — Quilt является API-совместимым форком
/// Fabric Loader, и их Meta API специально спроектирован по образцу Fabric Meta, так что
/// логика здесь почти дословно повторяет FabricService.
/// </summary>
public sealed class QuiltService
{
    private const string MetaBaseUrl = "https://meta.quiltmc.org/v3/versions/loader";

    private readonly HttpClient _http;

    public QuiltService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
    }

    public async Task<string?> GetRecommendedLoaderVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        var url = $"{MetaBaseUrl}/{Uri.EscapeDataString(mcVersion)}";
        var json = await _http.GetStringAsync(url, ct);
        var entries = JsonSerializer.Deserialize<List<QuiltLoaderListEntry>>(json) ?? new();

        return entries.Count > 0 ? entries[0].Loader.Version : null;
    }

    public async Task<VersionDetails> GetMergedVersionDetailsAsync(
        VersionDetails vanilla,
        string mcVersion,
        string loaderVersion,
        CancellationToken ct = default)
    {
        var url = $"{MetaBaseUrl}/{Uri.EscapeDataString(mcVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/json";
        var json = await _http.GetStringAsync(url, ct);
        var profile = JsonSerializer.Deserialize<VersionDetails>(json)
                      ?? throw new InvalidDataException("Не удалось разобрать профиль Quilt");

        var merged = new VersionDetails
        {
            Id = profile.Id,
            MainClass = profile.MainClass,
            Type = vanilla.Type,
            AssetsIndexId = vanilla.AssetsIndexId,
            AssetIndex = vanilla.AssetIndex,
            Downloads = vanilla.Downloads,
            JavaVersion = vanilla.JavaVersion,
            Libraries = vanilla.Libraries.Concat(profile.Libraries).ToList(),
        };

        if (vanilla.Arguments is not null || profile.Arguments is not null)
        {
            merged.Arguments = new ArgumentsSpec
            {
                Game = vanilla.Arguments?.Game ?? new List<JsonElement>(),
                Jvm = (vanilla.Arguments?.Jvm ?? new List<JsonElement>())
                    .Concat(profile.Arguments?.Jvm ?? new List<JsonElement>())
                    .ToList(),
            };
        }
        else
        {
            merged.MinecraftArguments = vanilla.MinecraftArguments;
        }

        return merged;
    }
}
