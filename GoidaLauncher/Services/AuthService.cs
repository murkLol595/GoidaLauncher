using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;

namespace GoidaLauncher.Services;

public sealed class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Username { get; set; }
    public string? Uuid { get; set; }
    public string? AccessToken { get; set; }
    public string? SkinUrl { get; set; }
    public string? SkinVariant { get; set; }
}

public sealed class TokenValidationResult
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
}

public static class AuthService
{
    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
    };

    static AuthService()
    {
        Http.DefaultRequestHeaders.UserAgent.ParseAdd("GoidaLauncher/1.0");
    }

    /// <summary>
    /// Очищает и нормализует строку с токеном.
    /// Поддерживает чистый токен, заголовок "Bearer ...", а также случайную вставку JSON.
    /// </summary>
    public static string SanitizeToken(string rawInput)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
            return "";

        var token = rawInput.Trim().Trim('"', '\'');

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            token = token[7..].Trim();

        if (token.StartsWith('{') && token.EndsWith('}'))
        {
            try
            {
                using var doc = JsonDocument.Parse(token);
                if (doc.RootElement.TryGetProperty("accessToken", out var atProp) ||
                    doc.RootElement.TryGetProperty("access_token", out atProp))
                {
                    token = atProp.GetString()?.Trim() ?? token;
                }
            }
            catch
            {
                // Игнорируем ошибку парсинга, используем как есть
            }
        }

        return token.Trim();
    }

    /// <summary>
    /// Проверяет токен лицензии через официальный Mojang API и получает профиль игрока (никнейм, UUID, скин).
    /// </summary>
    public static async Task<AuthResult> ValidateAndGetProfileAsync(string rawInput, CancellationToken ct = default)
    {
        var token = SanitizeToken(rawInput);
        if (string.IsNullOrWhiteSpace(token))
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Токен не может быть пустым."
            };
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await Http.SendAsync(request, ct);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Токен недействителен или срок его действия истёк (401 Unauthorized)."
                };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "На этой учётной записи не найдена купленная лицензия Minecraft Java Edition (404 Not Found)."
                };
            }

            if (!response.IsSuccessStatusCode)
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = $"Ошибка Mojang API: {(int)response.StatusCode} {response.ReasonPhrase}"
                };
            }

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var uuid = root.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? "" : "";
            var username = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? "" : "";

            if (string.IsNullOrWhiteSpace(username))
            {
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Не удалось получить имя игрока из профиля Mojang."
                };
            }

            string? skinUrl = null;
            string? skinVariant = null;

            if (root.TryGetProperty("skins", out var skinsEl) && skinsEl.ValueKind == JsonValueKind.Array)
            {
                foreach (var s in skinsEl.EnumerateArray())
                {
                    var state = s.TryGetProperty("state", out var st) ? st.GetString() : null;
                    if (state == "ACTIVE" || skinUrl is null)
                    {
                        skinUrl = s.TryGetProperty("url", out var u) ? u.GetString() : null;
                        skinVariant = s.TryGetProperty("variant", out var v) ? v.GetString() : null;
                        if (state == "ACTIVE") break;
                    }
                }
            }

            return new AuthResult
            {
                Success = true,
                Username = username,
                Uuid = uuid,
                AccessToken = token,
                SkinUrl = skinUrl,
                SkinVariant = skinVariant
            };
        }
        catch (OperationCanceledException)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Превышено время ожидания ответа от Mojang API."
            };
        }
        catch (Exception ex)
        {
            return new AuthResult
            {
                Success = false,
                ErrorMessage = $"Сетевая ошибка при проверке токена: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Быстрая проверка токена перед запуском игры (с коротким таймаутом).
    /// </summary>
    public static async Task<TokenValidationResult> QuickValidateTokenAsync(string? token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TokenValidationResult { IsValid = false, Message = "Токен отсутствует." };

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.minecraftservices.com/minecraft/profile");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await Http.SendAsync(request, cts.Token);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return new TokenValidationResult
                {
                    IsValid = false,
                    Message = "Токен лицензии истёк или был отозван (401 Unauthorized)."
                };
            }

            if (response.IsSuccessStatusCode)
            {
                return new TokenValidationResult { IsValid = true };
            }

            return new TokenValidationResult
            {
                IsValid = true,
                Message = $"Mojang API вернул статус: {response.StatusCode}."
            };
        }
        catch
        {
            // Если сеть недоступна (офлайн) — не блокируем запуск игры
            return new TokenValidationResult
            {
                IsValid = true,
                Message = "Сервер Mojang недоступен, проверка пропущена."
            };
        }
    }

    /// <summary>
    /// Скачивает скин игрока по ссылке из профиля Mojang и сохраняет его в библиотеку скинов лаунчера.
    /// Возвращает имя созданного файла (для ActiveSkinPackFile).
    /// </summary>
    public static async Task<string?> DownloadSkinAsync(
        string skinUrl, string gameRootDir, string username, CancellationToken ct = default)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(skinUrl, ct);
            var skinsDir = SkinService.GetSkinsDirectory(gameRootDir);

            var safeNick = string.Concat(username.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
            if (string.IsNullOrWhiteSpace(safeNick)) safeNick = "skin";

            var fileName = $"{safeNick}.png";
            var destPath = Path.Combine(skinsDir, fileName);

            await File.WriteAllBytesAsync(destPath, bytes, ct);
            return fileName;
        }
        catch
        {
            return null;
        }
    }
}
