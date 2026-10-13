using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

/// <summary>
/// Устанавливает Forge так же, как это делает официальный инсталлятор: скачивает installer.jar,
/// разбирает install_profile.json, докачивает нужные библиотеки и запускает цепочку "processors" —
/// отдельных Java-программ, которые патчат/собирают клиентский jar Forge из ванильного.
/// Это самая сложная и наименее "стандартная" часть лаунчера (в отличие от Fabric, у Forge нет
/// простого JSON-профиля) — реализовано по официально задокументированному формату.
/// </summary>
public sealed class ForgeService
{
    private const string PromotionsUrl = "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json";
    private const string ForgeMavenBase = "https://maven.minecraftforge.net/";
    private const string VanillaLibrariesBase = "https://libraries.minecraft.net/";

    private readonly HttpClient _http;
    private readonly string _librariesDir;
    private readonly string _tempRootDir;

    public ForgeService(string gameRootDirectory, string librariesDirectory)
    {
        _librariesDir = librariesDirectory;
        _tempRootDir = Path.Combine(gameRootDirectory, "forge-temp");
        Directory.CreateDirectory(_tempRootDir);

        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
        _http.Timeout = TimeSpan.FromMinutes(10);
    }

    public async Task<string?> GetRecommendedForgeVersionAsync(string mcVersion, CancellationToken ct = default)
    {
        try
        {
            var json = await _http.GetStringAsync(PromotionsUrl, ct);
            var promos = JsonSerializer.Deserialize<ForgePromotions>(json)?.Promos ?? new();

            if (promos.TryGetValue($"{mcVersion}-recommended", out var recommended))
                return recommended;
            if (promos.TryGetValue($"{mcVersion}-latest", out var latest))
                return latest;

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<VersionDetails> InstallAndMergeAsync(
        VersionDetails vanilla,
        string mcVersion,
        string forgeVersion,
        string vanillaClientJarPath,
        IProgress<string> log,
        CancellationToken ct = default,
        string? customInstallerUrl = null,
        string? customLoaderName = null)
    {
        var loaderName = customLoaderName ?? "Forge";
        var workDir = Path.Combine(_tempRootDir, $"{loaderName.ToLowerInvariant()}-{mcVersion}-{forgeVersion}");
        Directory.CreateDirectory(workDir);

        log.Report($"Скачиваю установщик {loaderName} {forgeVersion}...");
        var installerPath = Path.Combine(workDir, "installer.jar");
        var installerUrl = customInstallerUrl
            ?? $"{ForgeMavenBase}net/minecraftforge/forge/{mcVersion}-{forgeVersion}/forge-{mcVersion}-{forgeVersion}-installer.jar";
        await DownloadFileAsync(installerUrl, installerPath, ct);

        using var installerZip = ZipFile.OpenRead(installerPath);

        log.Report("Разбираю install_profile.json...");
        var installProfile = ReadJsonEntry<ForgeInstallProfile>(installerZip, "install_profile.json")
            ?? throw new InvalidDataException("В установщике Forge не найден install_profile.json — формат установщика мог измениться.");

        var forgeVersionJsonEntryName = installProfile.VersionJsonPath.TrimStart('/');
        var forgeVersionDetails = ReadJsonEntry<VersionDetails>(installerZip, forgeVersionJsonEntryName)
            ?? throw new InvalidDataException($"В установщике Forge не найден {forgeVersionJsonEntryName}.");

        var libraryLookup = installProfile.Libraries
            .Where(l => !string.IsNullOrEmpty(l.Name))
            .GroupBy(l => l.Name)
            .ToDictionary(g => g.Key, g => g.First());

        var tokens = new Dictionary<string, string>
        {
            ["SIDE"] = "client",
            ["MINECRAFT_JAR"] = vanillaClientJarPath,
            ["MINECRAFT_VERSION"] = mcVersion,
            ["ROOT"] = workDir,
            ["INSTALLER"] = installerPath,
            ["LIBRARY_DIR"] = _librariesDir,
        };

        log.Report("Готовлю данные для процессоров установки...");
        foreach (var (key, entry) in installProfile.Data)
        {
            var raw = entry.Client;
            if (string.IsNullOrEmpty(raw)) continue;

            if (raw.StartsWith('[') && raw.EndsWith(']'))
            {
                var coord = raw[1..^1];
                var path = await EnsureLibraryAsync(coord, libraryLookup, installerZip, ct);
                tokens[key] = path;
            }
            else if (raw.StartsWith('/'))
            {
                var extractedPath = Path.Combine(workDir, "data", raw.TrimStart('/'));
                Directory.CreateDirectory(Path.GetDirectoryName(extractedPath)!);
                var zipEntry = installerZip.GetEntry(raw.TrimStart('/'))
                    ?? throw new InvalidDataException($"В установщике Forge не найден встроенный файл данных {raw}.");
                zipEntry.ExtractToFile(extractedPath, overwrite: true);
                tokens[key] = extractedPath;
            }
            else
            {
                tokens[key] = raw;
            }
        }

        log.Report($"Запускаю {installProfile.Processors.Count} шагов установки Forge (это может занять минуту)...");
        int step = 0;
        foreach (var processor in installProfile.Processors)
        {
            step++;
            if (processor.Sides is not null && processor.Sides.Count > 0 && !processor.Sides.Contains("client"))
                continue;

            log.Report($"[{step}/{installProfile.Processors.Count}] {processor.Jar}");

            var jarPath = await EnsureLibraryAsync(processor.Jar, libraryLookup, installerZip, ct);
            var mainClass = ReadMainClassFromJar(jarPath)
                ?? throw new InvalidDataException($"Не удалось определить главный класс в {processor.Jar}.");

            var classpathParts = new List<string> { jarPath };
            foreach (var cpEntry in processor.Classpath)
                classpathParts.Add(await EnsureLibraryAsync(cpEntry, libraryLookup, installerZip, ct));

            var args = new List<string>();
            foreach (var a in processor.Args)
                args.Add(await SubstituteTokenAsync(a, tokens, libraryLookup, installerZip, ct));

            var psi = new ProcessStartInfo
            {
                FileName = FindJavaExecutable(),
                WorkingDirectory = workDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-cp");
            psi.ArgumentList.Add(string.Join(Path.PathSeparator, classpathParts));
            psi.ArgumentList.Add(mainClass);
            foreach (var a in args) psi.ArgumentList.Add(a);

            using var process = Process.Start(psi)!;
            var stdout = await process.StandardOutput.ReadToEndAsync(ct);
            var stderr = await process.StandardError.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var tail = string.Join('\n', (stdout + "\n" + stderr).Split('\n').TakeLast(20));
                throw new InvalidOperationException(
                    $"Шаг установки Forge завершился с ошибкой (код {process.ExitCode}), процессор {processor.Jar}.\n" +
                    $"Последние строки лога:\n{tail}");
            }
        }

        log.Report("Установка Forge завершена, собираю итоговый профиль запуска...");

        var merged = new VersionDetails
        {
            Id = string.IsNullOrWhiteSpace(forgeVersionDetails.Id) ? $"{loaderName.ToLowerInvariant()}-{mcVersion}-{forgeVersion}" : forgeVersionDetails.Id,
            MainClass = forgeVersionDetails.MainClass,
            Type = vanilla.Type,
            AssetsIndexId = vanilla.AssetsIndexId,
            AssetIndex = vanilla.AssetIndex,
            Downloads = vanilla.Downloads,
            JavaVersion = vanilla.JavaVersion,
        };

        var mergedLibs = new List<Library>();
        var seenNames = new HashSet<string>();
        foreach (var lib in vanilla.Libraries.Concat(installProfile.Libraries).Concat(forgeVersionDetails.Libraries))
        {
            if (!string.IsNullOrEmpty(lib.Name) && !seenNames.Add(lib.Name))
                continue;
            mergedLibs.Add(lib);
        }
        merged.Libraries = mergedLibs;

        if (vanilla.Arguments is not null || forgeVersionDetails.Arguments is not null)
        {
            merged.Arguments = new ArgumentsSpec
            {
                Game = (vanilla.Arguments?.Game ?? new List<JsonElement>())
                    .Concat(forgeVersionDetails.Arguments?.Game ?? new List<JsonElement>())
                    .ToList(),
                Jvm = (vanilla.Arguments?.Jvm ?? new List<JsonElement>())
                    .Concat(forgeVersionDetails.Arguments?.Jvm ?? new List<JsonElement>())
                    .ToList(),
            };
        }
        else
        {
            merged.MinecraftArguments = vanilla.MinecraftArguments ?? forgeVersionDetails.MinecraftArguments;
        }

        return merged;
    }

    // ---------------------------------------------------------------
    // Вспомогательные методы
    // ---------------------------------------------------------------

    private async Task<string> SubstituteTokenAsync(
        string arg,
        Dictionary<string, string> tokens,
        Dictionary<string, Library> libraryLookup,
        ZipArchive installerZip,
        CancellationToken ct)
    {
        if (arg.StartsWith('[') && arg.EndsWith(']'))
        {
            var coord = arg[1..^1];
            return await EnsureLibraryAsync(coord, libraryLookup, installerZip, ct);
        }

        var result = arg;
        foreach (var (key, value) in tokens)
            result = result.Replace("{" + key + "}", value);

        return result;
    }

    private async Task<string> EnsureLibraryAsync(
        string mavenCoord,
        Dictionary<string, Library> libraryLookup,
        ZipArchive installerZip,
        CancellationToken ct)
    {
        var relPath = MinecraftService.MavenNameToPath(mavenCoord);
        var targetPath = Path.Combine(_librariesDir, relPath.Replace('/', Path.DirectorySeparatorChar));

        if (File.Exists(targetPath))
            return targetPath;

        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);

        if (libraryLookup.TryGetValue(mavenCoord, out var lib))
        {
            if (lib.Downloads?.Artifact is { } artifact && !string.IsNullOrEmpty(artifact.Url))
            {
                await DownloadFileAsync(artifact.Url, targetPath, ct);
                return targetPath;
            }

            if (!string.IsNullOrEmpty(lib.MavenRepositoryUrl))
            {
                var url = lib.MavenRepositoryUrl.TrimEnd('/') + "/" + relPath;
                if (await TryDownloadFileAsync(url, targetPath, ct)) return targetPath;
            }
        }

        var embeddedEntry = installerZip.GetEntry("maven/" + relPath);
        if (embeddedEntry is not null)
        {
            embeddedEntry.ExtractToFile(targetPath, overwrite: true);
            return targetPath;
        }

        if (await TryDownloadFileAsync(ForgeMavenBase + relPath, targetPath, ct)) return targetPath;
        if (await TryDownloadFileAsync(VanillaLibrariesBase + relPath, targetPath, ct)) return targetPath;
        if (await TryDownloadFileAsync("https://repo1.maven.org/maven2/" + relPath, targetPath, ct)) return targetPath;

        throw new InvalidDataException(
            $"Не удалось найти библиотеку {mavenCoord} ни в установщике, ни на известных maven-репозиториях.");
    }

    private static string? ReadMainClassFromJar(string jarPath)
    {
        using var archive = ZipFile.OpenRead(jarPath);
        var manifestEntry = archive.GetEntry("META-INF/MANIFEST.MF");
        if (manifestEntry is null) return null;

        using var reader = new StreamReader(manifestEntry.Open());
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("Main-Class:", StringComparison.OrdinalIgnoreCase))
                return line["Main-Class:".Length..].Trim();
        }

        return null;
    }

    private static T? ReadJsonEntry<T>(ZipArchive archive, string entryName)
    {
        var entry = archive.GetEntry(entryName);
        if (entry is null) return default;

        using var reader = new StreamReader(entry.Open());
        var text = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(text);
    }

    private async Task DownloadFileAsync(string url, string targetPath, CancellationToken ct)
    {
        var tmpPath = targetPath + ".tmp";
        using (var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct))
        {
            response.EnsureSuccessStatusCode();
            await using var fs = File.Create(tmpPath);
            await response.Content.CopyToAsync(fs, ct);
        }

        if (File.Exists(targetPath)) File.Delete(targetPath);
        File.Move(tmpPath, targetPath);
    }

    private async Task<bool> TryDownloadFileAsync(string url, string targetPath, CancellationToken ct)
    {
        try
        {
            await DownloadFileAsync(url, targetPath, ct);
            return true;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    private static string FindJavaExecutable()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (!string.IsNullOrWhiteSpace(javaHome))
        {
            var candidate = Path.Combine(javaHome, "bin", "java.exe");
            if (File.Exists(candidate)) return candidate;
        }

        var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathVar.Split(Path.PathSeparator))
        {
            try
            {
                var candidate = Path.Combine(dir, "java.exe");
                if (File.Exists(candidate)) return candidate;
            }
            catch { }
        }

        return "java.exe";
    }
}
