using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

/// <summary>
/// Интеграция с официальным репозиторием NeoForge (maven.neoforged.net).
/// NeoForge является современным продолжением Forge для версий Minecraft 1.20.2+.
/// Формат его инсталлятора полностью совместим с Forge (install_profile.json + processors),
/// поэтому сборка выполняется через общий движок ForgeService.
/// </summary>
public sealed class NeoForgeService
{
    private const string NeoForgeVersionsApi = "https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge";
    private const string NeoForgeMavenReleases = "https://maven.neoforged.net/releases/";

    private readonly HttpClient _http;
    private readonly ForgeService _forgeInstaller;

    public NeoForgeService(string gameRootDirectory, string librariesDirectory)
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
        _http.Timeout = TimeSpan.FromMinutes(10);

        _forgeInstaller = new ForgeService(gameRootDirectory, librariesDirectory);
    }

    /// <summary>
    /// Возвращает рекомендуемую стабильную версию NeoForge под указанную версию Minecraft.
    /// Например: для 1.20.4 ищет префикс 20.4.*, для 1.21.1 ищет 21.1.*.
    /// </summary>
    public async Task<string?> GetRecommendedNeoForgeVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(NeoForgeVersionsApi, ct);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("versions", out var versionsProp) ||
                versionsProp.ValueKind != JsonValueKind.Array)
                return null;

            string prefix;
            if (mcVersion.StartsWith("1."))
            {
                var parts = mcVersion.Split('.');
                if (parts.Length == 2)
                    prefix = $"{parts[1]}.0.";
                else if (parts.Length >= 3)
                    prefix = $"{parts[1]}.{parts[2]}.";
                else
                    return null;
            }
            else
            {
                prefix = $"{mcVersion}.";
            }

            var matching = new List<string>();
            foreach (var v in versionsProp.EnumerateArray())
            {
                var ver = v.GetString();
                if (!string.IsNullOrEmpty(ver) && ver.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    matching.Add(ver);
            }

            if (matching.Count == 0) return null;

            // Предпочитаем стабильные релизы (без beta/alpha), иначе берем последний доступный
            var stable = matching.LastOrDefault(v =>
                !v.Contains("beta", StringComparison.OrdinalIgnoreCase) &&
                !v.Contains("alpha", StringComparison.OrdinalIgnoreCase));

            return stable ?? matching.Last();
        }
        catch
        {
            return null;
        }
    }

    public async Task<VersionDetails> InstallAndMergeAsync(
        VersionDetails vanilla,
        string mcVersion,
        string neoForgeVersion,
        string vanillaClientJarPath,
        IProgress<string> log,
        CancellationToken ct = default)
    {
        var installerUrl = $"{NeoForgeMavenReleases}net/neoforged/neoforge/{Uri.EscapeDataString(neoForgeVersion)}/neoforge-{Uri.EscapeDataString(neoForgeVersion)}-installer.jar";
        return await _forgeInstaller.InstallAndMergeAsync(
            vanilla,
            mcVersion,
            neoForgeVersion,
            vanillaClientJarPath,
            log,
            ct,
            customInstallerUrl: installerUrl,
            customLoaderName: "NeoForge");
    }
}
