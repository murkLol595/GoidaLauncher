using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

/// <summary>
/// Интеграция с официальным Fabric Meta API (meta.fabricmc.net).
/// Fabric не требует своего инсталлятора: "профиль" загрузчика — это обычный
/// version-json в том же формате, что у ванильной версии, с полем inheritsFrom.
/// Мы скачиваем этот профиль и сливаем его с данными ванильной версии,
/// после чего результат можно скачивать/запускать через MinecraftService как обычно.
/// </summary>
public sealed class FabricService
{
    private const string MetaBaseUrl = "https://meta.fabricmc.net/v2/versions/loader";

    private readonly HttpClient _http;

    public FabricService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
    }

    /// <summary>Возвращает рекомендуемую (последнюю стабильную) версию Fabric Loader для данной версии Minecraft.</summary>
    public async Task<string?> GetRecommendedLoaderVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        var url = $"{MetaBaseUrl}/{Uri.EscapeDataString(mcVersion)}";
        var json = await _http.GetStringAsync(url, ct);
        var entries = JsonSerializer.Deserialize<List<FabricLoaderListEntry>>(json) ?? new();

        if (entries.Count == 0)
            return null;

        var stable = entries.FirstOrDefault(e => e.Loader.Stable);
        return (stable ?? entries[0]).Loader.Version;
    }

    /// <summary>
    /// Скачивает профиль Fabric для указанной версии Minecraft/загрузчика и сливает его
    /// с уже полученными данными ванильной версии (либы, ассеты, клиент).
    /// </summary>
    public async Task<VersionDetails> GetMergedVersionDetailsAsync(
        VersionDetails vanilla,
        string mcVersion,
        string loaderVersion,
        CancellationToken ct = default)
    {
        var url = $"{MetaBaseUrl}/{Uri.EscapeDataString(mcVersion)}/{Uri.EscapeDataString(loaderVersion)}/profile/json";
        var json = await _http.GetStringAsync(url, ct);
        var profile = JsonSerializer.Deserialize<VersionDetails>(json)
                      ?? throw new InvalidDataException("Не удалось разобрать профиль Fabric");

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
            // Очень старый формат (маловероятно для Fabric, но на всякий случай)
            merged.MinecraftArguments = vanilla.MinecraftArguments;
        }

        return merged;
    }
}
