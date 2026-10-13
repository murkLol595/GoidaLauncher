using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public sealed class ModrinthService
{
    private const string BaseUrl = "https://api.modrinth.com/v2";

    private readonly HttpClient _http;

    public ModrinthService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0 (github.com goidalauncher)");
    }

    /// <param name="projectType">"mod", "resourcepack" или "shader".</param>
    public async Task<List<ModrinthHit>> SearchAsync(
        string query,
        string projectType,
        string? gameVersion,
        string? loader,
        CancellationToken ct = default)
    {
        var facets = new List<string> { $"[\"project_type:{projectType}\"]" };

        if (!string.IsNullOrWhiteSpace(gameVersion))
            facets.Add($"[\"versions:{gameVersion}\"]");

        if (!string.IsNullOrWhiteSpace(loader) && projectType == "mod")
            facets.Add($"[\"categories:{loader}\"]");

        var facetsParam = "[" + string.Join(",", facets) + "]";

        var url = $"{BaseUrl}/search" +
                   $"?query={Uri.EscapeDataString(query)}" +
                   $"&facets={Uri.EscapeDataString(facetsParam)}" +
                   "&limit=30";

        var json = await _http.GetStringAsync(url, ct);
        var result = JsonSerializer.Deserialize<ModrinthSearchResponse>(json) ?? new ModrinthSearchResponse();
        return result.Hits;
    }

    /// <summary>
    /// Возвращает файл наиболее подходящей версии проекта (с учётом версии игры и загрузчика, если заданы).
    /// </summary>
    public async Task<ModrinthVersionFile?> GetBestFileAsync(
        string projectId,
        string? gameVersion,
        string? loader,
        CancellationToken ct = default)
    {
        var url = $"{BaseUrl}/project/{projectId}/version";
        var query = new List<string>();

        if (!string.IsNullOrWhiteSpace(gameVersion))
            query.Add($"game_versions={Uri.EscapeDataString($"[\"{gameVersion}\"]")}");

        if (!string.IsNullOrWhiteSpace(loader))
            query.Add($"loaders={Uri.EscapeDataString($"[\"{loader}\"]")}");

        if (query.Count > 0)
            url += "?" + string.Join("&", query);

        var json = await _http.GetStringAsync(url, ct);
        var versions = JsonSerializer.Deserialize<List<ModrinthVersion>>(json) ?? new();

        var version = versions.FirstOrDefault();
        if (version is null) return null;

        return version.Files.FirstOrDefault(f => f.Primary) ?? version.Files.FirstOrDefault();
    }

    public async Task DownloadToFolderAsync(ModrinthVersionFile file, string destinationFolder, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationFolder);
        var destPath = Path.Combine(destinationFolder, file.Filename);

        using var response = await _http.GetAsync(file.Url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        await using var fs = File.Create(destPath);
        await response.Content.CopyToAsync(fs, ct);
    }
}
