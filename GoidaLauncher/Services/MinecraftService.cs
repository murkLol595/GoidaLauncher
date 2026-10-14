using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public sealed record ProgressInfo(string Status, double Percent);

public sealed class LaunchOptions
{
    public required string Nickname { get; init; }
    public string? Uuid { get; init; }
    public string? AccessToken { get; init; }
    public string? UserType { get; init; }
    public string? Xuid { get; init; }
    public int MinRamMb { get; init; } = 1024;
    public int MaxRamMb { get; init; } = 2048;
    public string? JavaPath { get; init; }
    public string WindowWidth { get; init; } = "854";
    public string WindowHeight { get; init; } = "480";

    /// <summary>Дополнительные аргументы JVM (через пробел) — добавляются в конец списка перед запуском.</summary>
    public string? ExtraJvmArgs { get; init; }

    /// <summary>Папка запуска (mods/saves/config/resourcepacks) — если не задана, используется общий корень.</summary>
    public string? GameDirectoryOverride { get; init; }

    /// <summary>Включить комплексную оптимизацию JVM (Aikar's Flags, параллельный сборщик G1GC, String Deduplication).</summary>
    public bool OptimizeGame { get; init; } = true;

    /// <summary>Запуск процесса Minecraft с высоким приоритетом в Windows для устранения микрофризов.</summary>
    public bool HighPriority { get; init; } = true;

    /// <summary>Принудительно использовать дискретную видеокарту (NVIDIA/AMD) и многопоточный OpenGL.</summary>
    public bool ForceDiscreteGpu { get; init; } = true;
}

public sealed class MinecraftService
{
    private const string VersionManifestUrl =
        "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";
    private const string ResourcesBaseUrl = "https://resources.download.minecraft.net";

    private readonly HttpClient _http;
    private readonly string _rootDir;

    private readonly string _versionsDir;
    private readonly string _librariesDir;
    private readonly string _assetsDir;
    private readonly string _nativesDir;

    public MinecraftService(string gameRootDirectory)
    {
        _rootDir = gameRootDirectory;
        _versionsDir = Path.Combine(_rootDir, "versions");
        _librariesDir = Path.Combine(_rootDir, "libraries");
        _assetsDir = Path.Combine(_rootDir, "assets");
        _nativesDir = Path.Combine(_rootDir, "natives");

        Directory.CreateDirectory(_rootDir);
        Directory.CreateDirectory(_versionsDir);
        Directory.CreateDirectory(_librariesDir);
        Directory.CreateDirectory(_assetsDir);
        Directory.CreateDirectory(_nativesDir);

        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    // ---------------------------------------------------------------
    // Получение списка версий
    // ---------------------------------------------------------------

    public async Task<VersionManifest> GetVersionManifestAsync(CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync(VersionManifestUrl, ct);
        return JsonSerializer.Deserialize<VersionManifest>(json)
               ?? throw new InvalidDataException("Не удалось разобрать манифест версий Mojang");
    }

    public async Task<VersionDetails> GetVersionDetailsAsync(VersionManifestEntry entry, CancellationToken ct = default)
    {
        var json = await _http.GetStringAsync(entry.Url, ct);
        var details = JsonSerializer.Deserialize<VersionDetails>(json)
                       ?? throw new InvalidDataException($"Не удалось разобрать данные версии {entry.Id}");

        var versionDir = Path.Combine(_versionsDir, entry.Id);
        Directory.CreateDirectory(versionDir);
        await File.WriteAllTextAsync(Path.Combine(versionDir, $"{entry.Id}.json"), json, ct);

        return details;
    }

    /// <summary>
    /// Получает данные ванильной версии по id: если версия уже когда-то запускалась,
    /// читает закешированный json с диска без обращения к манифесту Mojang; иначе —
    /// один раз идёт в манифест, чтобы найти нужный URL, и кеширует результат.
    /// </summary>
    public async Task<VersionDetails> GetVersionDetailsByIdAsync(string mcVersionId, CancellationToken ct = default)
    {
        var localJsonPath = Path.Combine(_versionsDir, mcVersionId, $"{mcVersionId}.json");
        if (File.Exists(localJsonPath))
        {
            var cachedJson = await File.ReadAllTextAsync(localJsonPath, ct);
            var cached = JsonSerializer.Deserialize<VersionDetails>(cachedJson);
            if (cached is not null) return cached;
        }

        var manifest = await GetVersionManifestAsync(ct);
        var entry = manifest.Versions.FirstOrDefault(v => v.Id == mcVersionId)
            ?? throw new InvalidDataException($"Версия {mcVersionId} не найдена в манифесте Mojang.");
        return await GetVersionDetailsAsync(entry, ct);
    }

    /// <summary>Записывает рядом со скачанной сборкой пометку "это версия X под загрузчиком Y" —
    /// нужно, чтобы потом опознать её в списке уже установленных.</summary>
    public async Task WriteInstallMetaAsync(string versionFolderId, string mcVersion, string loader, CancellationToken ct = default)
    {
        var dir = Path.Combine(_versionsDir, versionFolderId);
        Directory.CreateDirectory(dir);
        var json = JsonSerializer.Serialize(new { mcVersion, loader });
        await File.WriteAllTextAsync(Path.Combine(dir, "goida-meta.json"), json, ct);
    }

    /// <summary>Сканирует папку versions/ и возвращает все реально установленные сборки
    /// (версия игры + загрузчик), опираясь на метку из <see cref="WriteInstallMetaAsync"/>.</summary>
    public List<InstalledBuild> GetInstalledBuilds()
    {
        var result = new List<InstalledBuild>();
        if (!Directory.Exists(_versionsDir)) return result;

        foreach (var dir in Directory.GetDirectories(_versionsDir))
        {
            var id = Path.GetFileName(dir);
            var jarPath = Path.Combine(dir, $"{id}.jar");
            if (!File.Exists(jarPath)) continue;

            var metaPath = Path.Combine(dir, "goida-meta.json");
            if (File.Exists(metaPath))
            {
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(metaPath));
                    var mc = doc.RootElement.GetProperty("mcVersion").GetString() ?? id;
                    var loader = doc.RootElement.GetProperty("loader").GetString() ?? "Vanilla";
                    result.Add(new InstalledBuild(id, mc, loader));
                    continue;
                }
                catch { /* повреждённая метка — считаем как ванильную ниже */ }
            }

            // Нет метки (например, версия лаунчера до этой правки) — считаем ванильной сборкой
            result.Add(new InstalledBuild(id, id, "Vanilla"));
        }

        return result;
    }

    // ---------------------------------------------------------------
    // Скачивание версии целиком: клиент, библиотеки, натив-библиотеки, ассеты
    // ---------------------------------------------------------------

    public async Task DownloadVersionAsync(
        VersionDetails details,
        IProgress<ProgressInfo> progress,
        CancellationToken ct = default)
    {
        progress.Report(new ProgressInfo("Скачивание клиента игры...", 0));
        await DownloadClientJarAsync(details, ct);

        progress.Report(new ProgressInfo("Скачивание библиотек...", 10));
        var classpathEntries = await DownloadLibrariesAsync(details, progress, ct);
        _classpathCache[details.Id] = classpathEntries;

        progress.Report(new ProgressInfo("Скачивание ресурсов (assets)...", 55));
        await DownloadAssetsAsync(details, progress, ct);

        progress.Report(new ProgressInfo("Готово к запуску", 100));
    }

    private readonly Dictionary<string, List<string>> _classpathCache = new();

    public string GetClientJarPath(VersionDetails details) =>
        Path.Combine(_versionsDir, details.Id, $"{details.Id}.jar");

    /// <summary>Проверяет, скачан ли уже клиент указанной версии (используется для сортировки в UI).</summary>
    public bool IsVersionDownloaded(string versionId) =>
        File.Exists(Path.Combine(_versionsDir, versionId, $"{versionId}.jar"));

    /// <summary>Проверяет наличие уже скачанной сборки версии под конкретный загрузчик
    /// (папки с загрузчиками именуются динамически, поэтому ищем по подстроке).</summary>
    public bool IsVersionDownloadedForLoader(string mcVersion, string loader)
    {
        if (loader == "Vanilla") return IsVersionDownloaded(mcVersion);
        if (!Directory.Exists(_versionsDir)) return false;

        string marker = loader switch
        {
            "Fabric" => "fabric-loader-",
            "Quilt" => "quilt-loader-",
            "Forge" => "forge-",
            "NeoForge" => "neoforge-",
            _ => "",
        };
        if (marker == "") return false;

        foreach (var dir in Directory.GetDirectories(_versionsDir))
        {
            var name = Path.GetFileName(dir);
            if (name.Contains(marker, StringComparison.OrdinalIgnoreCase) &&
                name.Contains(mcVersion, StringComparison.OrdinalIgnoreCase) &&
                File.Exists(Path.Combine(dir, name + ".jar")))
                return true;
        }
        return false;
    }

    public string LibrariesDirectory => _librariesDir;
    public string VersionsDirectory => _versionsDir;
    public string RootDirectory => _rootDir;

    /// <summary>
    /// Изолированная папка под конкретную сборку (версия+загрузчик) — там живут mods/config/saves,
    /// чтобы моды одной сборки не подмешивались к другой. Ключ стабилен по версии+загрузчику
    /// (не по точной версии загрузчика), чтобы обновление Fabric/Forge не создавало новую "пустую" папку.
    /// Скачанные версии/библиотеки/ассеты остаются общими — их дублировать незачем.
    /// </summary>
    public string GetInstanceDirectory(string mcVersion, string loader)
    {
        var rawKey = $"{mcVersion}-{loader.ToLowerInvariant()}";
        var safeKey = new string(rawKey.Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_').ToArray());

        var dir = Path.Combine(_rootDir, "instances", safeKey);
        Directory.CreateDirectory(Path.Combine(dir, "mods"));
        Directory.CreateDirectory(Path.Combine(dir, "config"));
        Directory.CreateDirectory(Path.Combine(dir, "saves"));
        Directory.CreateDirectory(Path.Combine(dir, "resourcepacks"));
        Directory.CreateDirectory(Path.Combine(dir, "shaderpacks"));
        return dir;
    }

    /// <summary>Скачивает только client.jar без библиотек/ассетов — нужно Forge до запуска процессоров установки.</summary>
    public Task EnsureClientJarDownloadedAsync(VersionDetails details, CancellationToken ct = default) =>
        DownloadClientJarAsync(details, ct);

    private async Task DownloadClientJarAsync(VersionDetails details, CancellationToken ct)
    {
        var client = details.Downloads?.Client
            ?? throw new InvalidDataException("В манифесте версии отсутствует client.jar");

        var targetPath = GetClientJarPath(details);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (File.Exists(targetPath) && new FileInfo(targetPath).Length == client.Size)
            return; // уже скачано

        await DownloadFileAsync(client.Url, targetPath, ct);
    }

    private async Task<List<string>> DownloadLibrariesAsync(
        VersionDetails details,
        IProgress<ProgressInfo> progress,
        CancellationToken ct)
    {
        var classpath = new List<string>();
        var nativeJarsToExtract = new List<string>();

        string osKey = "windows";
        string nativesClassifierSuffix = Environment.Is64BitOperatingSystem ? "natives-windows" : "natives-windows-32";

        int total = details.Libraries.Count;
        int done = 0;

        using var semaphore = new SemaphoreSlim(6);
        var tasks = new List<Task>();

        foreach (var lib in details.Libraries)
        {
            if (!RuleEvaluator.IsAllowed(lib.Rules))
            {
                Interlocked.Increment(ref done);
                continue;
            }

            tasks.Add(ProcessLibraryAsync(lib));
        }

        async Task ProcessLibraryAsync(Library lib)
        {
            await semaphore.WaitAsync(ct);
            try
            {
                // Обычный артефакт (jar с кодом)
                if (lib.Downloads?.Artifact is { } artifact && !string.IsNullOrEmpty(artifact.Url))
                {
                    var relPath = artifact.Path ?? MavenNameToPath(lib.Name);
                    var targetPath = Path.Combine(_librariesDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                    if (!File.Exists(targetPath) || new FileInfo(targetPath).Length != artifact.Size)
                    {
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                await DownloadFileAsync(artifact.Url, targetPath, ct);
                                break;
                            }
                            catch (Exception) when (attempt < 2)
                            {
                                await Task.Delay(250 * (attempt + 1), ct);
                            }
                        }
                    }

                    if (File.Exists(targetPath))
                        lock (classpath) classpath.Add(targetPath);
                }
                else if (!string.IsNullOrEmpty(lib.Name))
                {
                    // Библиотеки Fabric/Forge/Quilt: нет блока "downloads", только координаты Maven + базовый URL репозитория
                    var relPath = MavenNameToPath(lib.Name);
                    var targetPath = Path.Combine(_librariesDir, relPath.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

                    if (!File.Exists(targetPath))
                    {
                        var baseUrl = string.IsNullOrEmpty(lib.MavenRepositoryUrl)
                            ? "https://libraries.minecraft.net/"
                            : lib.MavenRepositoryUrl.TrimEnd('/') + "/";
                        var downloadUrl = baseUrl + relPath;

                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                await DownloadFileAsync(downloadUrl, targetPath, ct);
                                break;
                            }
                            catch (Exception) when (attempt < 2)
                            {
                                await Task.Delay(250 * (attempt + 1), ct);
                            }
                            catch
                            {
                                // Некоторые библиотеки в Forge/Quilt могут быть опциональными или генерироваться процессорами
                            }
                        }
                    }

                    if (File.Exists(targetPath))
                        lock (classpath) classpath.Add(targetPath);
                }

                // Natives (нативные библиотеки для конкретной ОС)
                string? classifierKey = null;
                if (lib.Natives is not null && lib.Natives.TryGetValue(osKey, out var nativeKey))
                    classifierKey = nativeKey;

                if (classifierKey is not null &&
                    lib.Downloads?.Classifiers is not null &&
                    lib.Downloads.Classifiers.TryGetValue(classifierKey, out var nativeArtifact) &&
                    !string.IsNullOrEmpty(nativeArtifact.Url))
                {
                    var nativeJarPath = Path.Combine(_librariesDir, "natives-temp",
                        Path.GetFileName(nativeArtifact.Path ?? $"{lib.Name}-natives.jar"));
                    Directory.CreateDirectory(Path.GetDirectoryName(nativeJarPath)!);

                    if (!File.Exists(nativeJarPath) || new FileInfo(nativeJarPath).Length != nativeArtifact.Size)
                    {
                        for (int attempt = 0; attempt < 3; attempt++)
                        {
                            try
                            {
                                await DownloadFileAsync(nativeArtifact.Url, nativeJarPath, ct);
                                break;
                            }
                            catch (Exception) when (attempt < 2)
                            {
                                await Task.Delay(250 * (attempt + 1), ct);
                            }
                        }
                    }

                    if (File.Exists(nativeJarPath))
                        lock (nativeJarsToExtract) nativeJarsToExtract.Add(nativeJarPath);
                }
            }
            finally
            {
                semaphore.Release();
                int nowDone = Interlocked.Increment(ref done);
                double pct = 10 + (nowDone / (double)Math.Max(total, 1)) * 40; // 10%..50%
                progress.Report(new ProgressInfo($"Библиотеки: {nowDone}/{total}", pct));
            }
        }

        await Task.WhenAll(tasks);

        // Распаковка natives в папку natives/<versionId>
        var versionNativesDir = Path.Combine(_nativesDir, details.Id);
        Directory.CreateDirectory(versionNativesDir);

        foreach (var jarPath in nativeJarsToExtract)
        {
            try
            {
                using var archive = ZipFile.OpenRead(jarPath);
                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name)) continue; // папка
                    if (entry.FullName.StartsWith("META-INF", StringComparison.OrdinalIgnoreCase)) continue;

                    var destPath = Path.Combine(versionNativesDir, entry.Name);
                    if (File.Exists(destPath)) continue;
                    entry.ExtractToFile(destPath, overwrite: true);
                }
            }
            catch (InvalidDataException)
            {
                // Повреждённый/некорректный jar — пропускаем, не роняем весь запуск
            }
        }

        _versionNativesDirCache[details.Id] = versionNativesDir;

        return classpath;
    }

    private readonly Dictionary<string, string> _versionNativesDirCache = new();

    private async Task DownloadAssetsAsync(
        VersionDetails details,
        IProgress<ProgressInfo> progress,
        CancellationToken ct)
    {
        if (details.AssetIndex is null)
            return;

        var indexDir = Path.Combine(_assetsDir, "indexes");
        Directory.CreateDirectory(indexDir);
        var indexPath = Path.Combine(indexDir, $"{details.AssetIndex.Id}.json");

        string indexJson;
        if (File.Exists(indexPath) && new FileInfo(indexPath).Length == details.AssetIndex.Size)
        {
            indexJson = await File.ReadAllTextAsync(indexPath, ct);
        }
        else
        {
            indexJson = await _http.GetStringAsync(details.AssetIndex.Url, ct);
            await File.WriteAllTextAsync(indexPath, indexJson, ct);
        }

        var assetIndex = JsonSerializer.Deserialize<AssetIndex>(indexJson)
                          ?? throw new InvalidDataException("Не удалось разобрать asset index");

        var objectsDir = Path.Combine(_assetsDir, "objects");
        Directory.CreateDirectory(objectsDir);

        var uniqueObjects = assetIndex.Objects.Values
            .GroupBy(o => o.Hash)
            .Select(g => g.First())
            .ToList();

        int total = uniqueObjects.Count;
        int done = 0;

        using var semaphore = new SemaphoreSlim(10);
        var tasks = uniqueObjects.Select(async obj =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var prefix = obj.Hash[..2];
                var targetDir = Path.Combine(objectsDir, prefix);
                Directory.CreateDirectory(targetDir);
                var targetPath = Path.Combine(targetDir, obj.Hash);

                if (!File.Exists(targetPath) || new FileInfo(targetPath).Length != obj.Size)
                {
                    var url = $"{ResourcesBaseUrl}/{prefix}/{obj.Hash}";
                    for (int attempt = 0; attempt < 3; attempt++)
                    {
                        try
                        {
                            await DownloadFileAsync(url, targetPath, ct);
                            break;
                        }
                        catch when (attempt < 2)
                        {
                            await Task.Delay(150 * (attempt + 1), ct);
                        }
                    }
                }
            }
            finally
            {
                semaphore.Release();
                int nowDone = Interlocked.Increment(ref done);
                double pct = 55 + (nowDone / (double)Math.Max(total, 1)) * 40; // 55%..95%
                if (nowDone % 25 == 0 || nowDone == total)
                    progress.Report(new ProgressInfo($"Ресурсы: {nowDone}/{total}", pct));
            }
        });

        await Task.WhenAll(tasks);
    }

    private async Task DownloadFileAsync(string url, string targetPath, CancellationToken ct, int timeoutSeconds = 60)
    {
        var tmpPath = targetPath + ".tmp";
        try
        {
            if (File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { }
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

            using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, timeoutCts.Token))
            {
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(tmpPath);
                await response.Content.CopyToAsync(fs, timeoutCts.Token);
            }

            if (File.Exists(targetPath))
            {
                try { File.Delete(targetPath); } catch { }
            }

            try
            {
                File.Move(tmpPath, targetPath);
            }
            catch
            {
                File.Copy(tmpPath, targetPath, overwrite: true);
                try { File.Delete(tmpPath); } catch { }
            }
        }
        finally
        {
            if (File.Exists(tmpPath))
            {
                try { File.Delete(tmpPath); } catch { }
            }
        }
    }

    public static string MavenNameToPath(string mavenName)
    {
        // group:artifact:version[:classifier][@extension] -> group/artifact/version/artifact-version[-classifier].extension
        string ext = "jar";
        var atIdx = mavenName.LastIndexOf('@');
        if (atIdx >= 0)
        {
            ext = mavenName[(atIdx + 1)..];
            mavenName = mavenName[..atIdx];
        }

        var parts = mavenName.Split(':');
        var group = parts[0].Replace('.', '/');
        var artifact = parts[1];
        var version = parts[2];
        var classifier = parts.Length > 3 ? "-" + parts[3] : "";
        return $"{group}/{artifact}/{version}/{artifact}-{version}{classifier}.{ext}";
    }

    // ---------------------------------------------------------------
    // Запуск игры
    // ---------------------------------------------------------------

    public Process Launch(VersionDetails details, LaunchOptions options)
    {
        var javaExe = !string.IsNullOrWhiteSpace(options.JavaPath)
            ? options.JavaPath!
            : FindJavaExecutable();

        var clientJar = GetClientJarPath(details);
        var classpath = _classpathCache.TryGetValue(details.Id, out var cp) ? new List<string>(cp) : new List<string>();
        classpath.Add(clientJar);
        var classpathStr = string.Join(Path.PathSeparator, classpath);

        var versionNativesDir = _versionNativesDirCache.TryGetValue(details.Id, out var nd)
            ? nd
            : Path.Combine(_nativesDir, details.Id);

        var assetsIndexName = details.AssetIndex?.Id ?? details.AssetsIndexId;

        var playerName = options.Nickname;
        var playerUuid = !string.IsNullOrWhiteSpace(options.Uuid)
            ? options.Uuid.Replace("-", "")
            : OfflineUuid(options.Nickname).ToString("N");
        var accessToken = !string.IsNullOrWhiteSpace(options.AccessToken)
            ? options.AccessToken
            : "0";
        var userType = !string.IsNullOrWhiteSpace(options.UserType)
            ? options.UserType
            : (!string.IsNullOrWhiteSpace(options.AccessToken) && options.AccessToken != "0" ? "msa" : "legacy");
        var xuid = options.Xuid ?? "-";
        var gameDir = options.GameDirectoryOverride ?? _rootDir;

        var placeholders = new Dictionary<string, string>
        {
            ["auth_player_name"] = playerName,
            ["version_name"] = details.Id,
            ["game_directory"] = gameDir,
            ["assets_root"] = _assetsDir,
            ["assets_index_name"] = assetsIndexName,
            ["auth_uuid"] = playerUuid,
            ["auth_access_token"] = accessToken,
            ["user_type"] = userType,
            ["version_type"] = details.Type,
            ["natives_directory"] = versionNativesDir,
            ["launcher_name"] = "GoidaLauncher",
            ["launcher_version"] = "1.0",
            ["classpath"] = classpathStr,
            ["resolution_width"] = options.WindowWidth,
            ["resolution_height"] = options.WindowHeight,
            ["clientid"] = "-",
            ["auth_xuid"] = xuid,
            ["user_properties"] = "{}",
        };

        var jvmArgs = new List<string>
        {
            $"-Xms{options.MinRamMb}M",
            $"-Xmx{options.MaxRamMb}M",
            $"-Djava.library.path={versionNativesDir}",
            "-Dfile.encoding=UTF-8",
        };

        if (options.OptimizeGame)
        {
            // Продвинутые флаги сборщика мусора и тюнинга производительности Minecraft (Aikar's Flags + G1GC)
            jvmArgs.AddRange(new[]
            {
                "-XX:+UseG1GC",
                "-XX:+ParallelRefProcEnabled",
                "-XX:MaxGCPauseMillis=200",
                "-XX:+UnlockExperimentalVMOptions",
                "-XX:+DisableExplicitGC",
                "-XX:+AlwaysPreTouch",
                "-XX:G1NewSizePercent=30",
                "-XX:G1MaxNewSizePercent=40",
                "-XX:G1ReservePercent=20",
                "-XX:G1HeapWastePercent=5",
                "-XX:G1MixedGCCountTarget=4",
                "-XX:InitiatingHeapOccupancyPercent=15",
                "-XX:G1MixedGCLiveThresholdPercent=90",
                "-XX:G1RSetUpdatingPauseTimePercent=5",
                "-XX:SurvivorRatio=32",
                "-XX:+PerfDisableSharedMem",
                "-XX:MaxTenuringThreshold=1",
                "-XX:+UseStringDeduplication"
            });
        }
        else
        {
            jvmArgs.Add("-XX:+UseG1GC");
        }

        if (!string.IsNullOrWhiteSpace(options.ExtraJvmArgs))
            jvmArgs.AddRange(options.ExtraJvmArgs.Split(' ', StringSplitOptions.RemoveEmptyEntries));

        var gameArgs = new List<string>();

        if (details.Arguments is not null)
        {
            jvmArgs.AddRange(ExtractArgList(details.Arguments.Jvm, placeholders));
            jvmArgs.Add("-cp");
            jvmArgs.Add(Quote(classpathStr));

            gameArgs.AddRange(ExtractArgList(details.Arguments.Game, placeholders));
        }
        else
        {
            jvmArgs.Add("-cp");
            jvmArgs.Add(Quote(classpathStr));

            var legacy = details.MinecraftArguments ?? "";
            foreach (var token in legacy.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                gameArgs.Add(SubstitutePlaceholders(token, placeholders));
        }

        var allArgs = new List<string>();
        allArgs.AddRange(jvmArgs);
        allArgs.Add(details.MainClass);
        allArgs.AddRange(gameArgs);

        var psi = new ProcessStartInfo
        {
            FileName = javaExe,
            WorkingDirectory = gameDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        if (options.ForceDiscreteGpu)
        {
            // Принудительное переключение на дискретную графику NVIDIA / AMD и включение многопоточного OpenGL
            psi.EnvironmentVariables["SHIM_MCCOMPAT"] = "0xD";
            psi.EnvironmentVariables["__GL_THREADED_OPTIMIZATIONS"] = "1";
            psi.EnvironmentVariables["mesa_glthread"] = "true";
            psi.EnvironmentVariables["__NV_PRIME_RENDER_OFFLOAD"] = "1";
            psi.EnvironmentVariables["__GLX_VENDOR_LIBRARY_NAME"] = "nvidia";
        }

        foreach (var arg in allArgs)
            psi.ArgumentList.Add(arg);

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.Start();

        if (options.HighPriority)
        {
            try
            {
                process.PriorityClass = ProcessPriorityClass.High;
            }
            catch
            {
                try { process.PriorityClass = ProcessPriorityClass.AboveNormal; } catch { }
            }
        }

        return process;
    }

    private static List<string> ExtractArgList(List<JsonElement> raw, Dictionary<string, string> placeholders)
    {
        var result = new List<string>();

        foreach (var element in raw)
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                result.Add(SubstitutePlaceholders(element.GetString() ?? "", placeholders));
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                // Условный аргумент вида { "rules": [...], "value": "..." или [...] }
                List<Rule>? rules = null;
                if (element.TryGetProperty("rules", out var rulesEl))
                    rules = JsonSerializer.Deserialize<List<Rule>>(rulesEl.GetRawText());

                if (!RuleEvaluator.IsAllowed(rules))
                    continue;

                if (element.TryGetProperty("value", out var valueEl))
                {
                    if (valueEl.ValueKind == JsonValueKind.String)
                    {
                        result.Add(SubstitutePlaceholders(valueEl.GetString() ?? "", placeholders));
                    }
                    else if (valueEl.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var v in valueEl.EnumerateArray())
                            result.Add(SubstitutePlaceholders(v.GetString() ?? "", placeholders));
                    }
                }
            }
        }

        return result;
    }

    private static string SubstitutePlaceholders(string input, Dictionary<string, string> placeholders)
    {
        if (!input.Contains("${")) return input;

        foreach (var (key, value) in placeholders)
            input = input.Replace("${" + key + "}", value);

        return input;
    }

    private static string Quote(string value) => value;

    private static Guid OfflineUuid(string nickname)
    {
        // Детерминированный UUID v3-подобный на основе ника (как делают офлайн-лаунчеры),
        // чтобы один и тот же ник всегда давал один и тот же UUID.
        using var md5 = System.Security.Cryptography.MD5.Create();
        var input = System.Text.Encoding.UTF8.GetBytes("OfflinePlayer:" + nickname);
        var hash = md5.ComputeHash(input);
        hash[6] = (byte)((hash[6] & 0x0F) | 0x30); // версия 3
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // вариант RFC 4122
        return new Guid(hash);
    }

    private static string FindJavaExecutable()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidateJava = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(candidateJava)) return candidateJava;
            var candidateJavaw = Path.Combine(javaHome, "bin", "javaw.exe");
            if (File.Exists(candidateJavaw)) return candidateJavaw;
        }

        // Пытаемся найти java в PATH
        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var candidateJava = Path.Combine(dir, "java.exe");
                if (File.Exists(candidateJava)) return candidateJava;
                var candidateJavaw = Path.Combine(dir, "javaw.exe");
                if (File.Exists(candidateJavaw)) return candidateJavaw;
            }
            catch { /* игнорируем некорректные записи PATH */ }
        }

        // Резервный вариант — рассчитываем на то, что java есть в PATH
        return "java.exe";
    }
}
