using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GoidaLauncher.Services;

public sealed class MrpackIndex
{
    [JsonPropertyName("formatVersion")]
    public int FormatVersion { get; set; } = 1;

    [JsonPropertyName("game")]
    public string Game { get; set; } = "minecraft";

    [JsonPropertyName("versionId")]
    public string VersionId { get; set; } = "";

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("files")]
    public List<MrpackFileEntry> Files { get; set; } = new();

    [JsonPropertyName("dependencies")]
    public Dictionary<string, string> Dependencies { get; set; } = new();
}

public sealed class MrpackFileEntry
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("hashes")]
    public Dictionary<string, string>? Hashes { get; set; }

    [JsonPropertyName("env")]
    public MrpackEnv? Env { get; set; }

    [JsonPropertyName("downloads")]
    public List<string> Downloads { get; set; } = new();

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }
}

public sealed class MrpackEnv
{
    [JsonPropertyName("client")]
    public string? Client { get; set; }

    [JsonPropertyName("server")]
    public string? Server { get; set; }
}

public static class ModpackService
{
    /// <summary>
    /// Распаковывает .mrpack архив, извлекает конфигурации (overrides) и параллельно скачивает все моды в целевую папку инстанса.
    /// </summary>
    public static async Task<MrpackIndex> InstallMrpackAsync(
        string mrpackPath,
        string targetDirectory,
        Action<string, double>? progressCallback = null,
        CancellationToken ct = default)
    {
        if (!File.Exists(mrpackPath))
            throw new FileNotFoundException("Файл модпака не найден", mrpackPath);

        Directory.CreateDirectory(targetDirectory);

        progressCallback?.Invoke("Чтение манифеста сборки...", 2);

        using var archive = ZipFile.OpenRead(mrpackPath);
        var indexEntry = archive.GetEntry("modrinth.index.json")
            ?? throw new InvalidDataException("В архиве модпака отсутствует файл манифеста modrinth.index.json.");

        MrpackIndex index;
        await using (var stream = indexEntry.Open())
        {
            index = await JsonSerializer.DeserializeAsync<MrpackIndex>(stream, cancellationToken: ct)
                    ?? throw new InvalidDataException("Не удалось прочитать modrinth.index.json.");
        }

        progressCallback?.Invoke("Распаковка конфигураций и настроек сборки...", 5);

        // 1. Распаковка overrides и client-overrides
        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name)) continue; // директория

            string? relPath = null;
            if (entry.FullName.StartsWith("overrides/", StringComparison.OrdinalIgnoreCase))
            {
                relPath = entry.FullName["overrides/".Length..];
            }
            else if (entry.FullName.StartsWith("client-overrides/", StringComparison.OrdinalIgnoreCase))
            {
                relPath = entry.FullName["client-overrides/".Length..];
            }

            if (!string.IsNullOrWhiteSpace(relPath))
            {
                var destPath = Path.Combine(targetDirectory, relPath.Replace('/', Path.DirectorySeparatorChar));
                var parentDir = Path.GetDirectoryName(destPath);
                if (!string.IsNullOrWhiteSpace(parentDir))
                    Directory.CreateDirectory(parentDir);

                entry.ExtractToFile(destPath, overwrite: true);
            }
        }

        // 2. Отбор файлов для скачивания (клиентские моды)
        var clientFiles = index.Files
            .Where(f => !string.Equals(f.Env?.Client, "unsupported", StringComparison.OrdinalIgnoreCase) && f.Downloads.Count > 0)
            .ToList();

        if (clientFiles.Count == 0)
        {
            progressCallback?.Invoke($"Сборка «{index.Name}» установлена (конфигурации применены)!", 100);
            return index;
        }

        progressCallback?.Invoke($"Подготовка к загрузке {clientFiles.Count} модов...", 10);

        // 3. Параллельная загрузка модов (до 6 одновременных потоков)
        using var http = new HttpClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0 (github.com goidalauncher)");

        int completed = 0;
        int total = clientFiles.Count;

        await Parallel.ForEachAsync(clientFiles, new ParallelOptions
        {
            MaxDegreeOfParallelism = 6,
            CancellationToken = ct
        }, async (file, token) =>
        {
            var destPath = Path.Combine(targetDirectory, file.Path.Replace('/', Path.DirectorySeparatorChar));
            var parentDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrWhiteSpace(parentDir))
                Directory.CreateDirectory(parentDir);

            // Проверяем, существует ли уже файл с нужным размером
            if (File.Exists(destPath) && new FileInfo(destPath).Length == file.FileSize)
            {
                int current = Interlocked.Increment(ref completed);
                double pct = 10 + ((double)current / total) * 88;
                progressCallback?.Invoke($"Скачивание модов ({current}/{total})...", pct);
                return;
            }

            string downloadUrl = file.Downloads[0];

            using var response = await http.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();

            var tempDownload = destPath + ".tmp";
            await using (var fs = File.Create(tempDownload))
            {
                await response.Content.CopyToAsync(fs, token);
            }

            File.Move(tempDownload, destPath, overwrite: true);

            int done = Interlocked.Increment(ref completed);
            double progressPct = 10 + ((double)done / total) * 88;
            progressCallback?.Invoke($"Скачивание модов ({done}/{total})...", progressPct);
        });

        progressCallback?.Invoke($"Сборка «{index.Name}» успешно установлена ({clientFiles.Count} модов)!", 100);
        return index;
    }
}
