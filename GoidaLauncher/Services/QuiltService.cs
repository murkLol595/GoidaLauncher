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

    private List<string>? _allLoadersCache;

    public async Task<string?> GetRecommendedLoaderVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        try
        {
            // 1. Получаем список загрузчиков, поддерживающих данную версию MC
            var mcUrl = $"{MetaBaseUrl}/{Uri.EscapeDataString(mcVersion)}";
            using var mcResp = await _http.GetAsync(mcUrl, ct);
            if (!mcResp.IsSuccessStatusCode)
                return null;

            var mcJson = await mcResp.Content.ReadAsStringAsync(ct);
            var mcEntries = JsonSerializer.Deserialize<List<QuiltLoaderListEntry>>(mcJson);
            if (mcEntries == null || mcEntries.Count == 0)
                return null;

            var compatibleVersions = new HashSet<string>(
                mcEntries.Select(e => e.Loader.Version).Where(v => !string.IsNullOrEmpty(v)),
                StringComparer.OrdinalIgnoreCase);

            if (compatibleVersions.Count == 0)
                return null;

            // 2. Получаем хронологический список всех загрузчиков Quilt (отсортирован от новых к старым)
            if (_allLoadersCache == null)
            {
                var allUrl = MetaBaseUrl;
                var allJson = await _http.GetStringAsync(allUrl, ct);
                using var doc = JsonDocument.Parse(allJson);
                var list = new List<string>();
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("version", out var vProp))
                    {
                        var v = vProp.GetString();
                        if (!string.IsNullOrEmpty(v)) list.Add(v);
                    }
                }
                _allLoadersCache = list;
            }

            string? firstMatch = null;
            string? firstStableMatch = null;

            foreach (var ver in _allLoadersCache)
            {
                if (!compatibleVersions.Contains(ver)) continue;

                firstMatch ??= ver;
                if (!ver.Contains("beta", StringComparison.OrdinalIgnoreCase) &&
                    !ver.Contains("alpha", StringComparison.OrdinalIgnoreCase))
                {
                    firstStableMatch ??= ver;
                    break;
                }
            }

            return firstStableMatch ?? firstMatch ?? mcEntries[0].Loader.Version;
        }
        catch
        {
            return null;
        }
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
                Game = (vanilla.Arguments?.Game ?? new List<JsonElement>())
                    .Concat(profile.Arguments?.Game ?? new List<JsonElement>())
                    .ToList(),
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
