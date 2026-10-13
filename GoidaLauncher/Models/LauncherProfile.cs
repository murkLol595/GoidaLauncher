using System.Text.Json.Serialization;

namespace GoidaLauncher.Models;

public enum AccountType
{
    Offline,
    LicenseToken
}

public sealed class LauncherAccount
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Username { get; set; } = "Player";
    public string Uuid { get; set; } = "";
    public string? AccessToken { get; set; }
    public AccountType Type { get; set; } = AccountType.Offline;
    public string? SkinUrl { get; set; }
    public string? ActiveSkinFile { get; set; }
    public DateTimeOffset AddedAt { get; set; } = DateTimeOffset.Now;

    [JsonIgnore]
    public string DisplayName => Type == AccountType.LicenseToken
        ? $"🔑 {Username} (Лицензия)"
        : $"👤 {Username}";

    public override string ToString() => DisplayName;
}

/// <summary>
/// Настройки лаунчера, сохраняемые между запусками.
/// </summary>
public sealed class LauncherSettings
{
    public string Nickname { get; set; } = "Player";
    public string? ActiveAccountId { get; set; }
    public List<LauncherAccount> Accounts { get; set; } = new();

    public string? LastVersionId { get; set; }
    public int MinRamMb { get; set; } = 1024;
    public int MaxRamMb { get; set; } = 2048;
    public string GameDirectory { get; set; } = "";
    public string? JavaPath { get; set; }
    public bool ShowSnapshots { get; set; } = false;
    public string WindowWidth { get; set; } = "854";
    public string WindowHeight { get; set; } = "480";

    /// <summary>"Vanilla", "Fabric", "Quilt" или "Forge".</summary>
    public string ModLoader { get; set; } = "Vanilla";

    /// <summary>Акцентный цвет интерфейса, HEX (#RRGGBB).</summary>
    public string AccentColorHex { get; set; } = "#7C3AED";

    /// <summary>Тема оформления: "Goida" (по умолчанию), "Cyberpunk", "Emerald", "Sunset", "Crimson", "Midnight".</summary>
    public string ThemePreset { get; set; } = "Goida";

    /// <summary>Включена ли анимация живого фона (плавающие неоновые сферы).</summary>
    public bool AnimatedBackgroundEnabled { get; set; } = true;

    /// <summary>Включён ли эффект матового стекла (Glassmorphism).</summary>
    public bool GlassmorphismEnabled { get; set; } = true;

    /// <summary>Скругление углов (в пикселях: 4, 8, 14).</summary>
    public int CornerRadiusValue { get; set; } = 14;

    /// <summary>Стиль логотипа в шапке: "GOIDA".</summary>
    public string LogoTitle { get; set; } = "GOIDA";

    /// <summary>Неоновое свечение кнопки запуска.</summary>
    public bool GlowEffectEnabled { get; set; } = true;

    /// <summary>Имя файла текущего активного скина (в resourcepacks/), либо null — скин по умолчанию.</summary>
    public string? ActiveSkinPackFile { get; set; }

    /// <summary>Последние введённые никнеймы (самый свежий первым), для выпадающего списка.</summary>
    public List<string> RecentNicknames { get; set; } = new();

    /// <summary>Включён ли Discord Rich Presence.</summary>
    public bool DiscordRpcEnabled { get; set; } = true;

    /// <summary>Application ID из discord.com/developers/applications (нужен свой, см. README).</summary>
    public string? DiscordApplicationId { get; set; }

    /// <summary>Дополнительные аргументы JVM (через пробел), добавляются при каждом запуске игры.</summary>
    public string? CustomJvmArgs { get; set; }
}
