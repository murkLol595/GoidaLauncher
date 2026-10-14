using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;
using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public sealed class ThemeDefinition
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required Color PrimaryAccent { get; init; }
    public required Color SecondaryAccent { get; init; }
    public required Color BgDarkStart { get; init; }
    public required Color BgDarkEnd { get; init; }
    public required Color PanelGlassColor { get; init; }
    public required Color PanelSolidColor { get; init; }
    public required Color BorderColor { get; init; }
    public required Color Blob1 { get; init; }
    public required Color Blob2 { get; init; }
    public required Color Blob3 { get; init; }
    public required Color Blob4 { get; init; }
    public required Color Blob5 { get; init; }
}

public static class ThemeService
{
    public static readonly IReadOnlyList<ThemeDefinition> Presets = new List<ThemeDefinition>
    {
        new()
        {
            Id = "Goida",
            Name = "🌟 Goida Neon",
            Description = "Фирменный стиль Goida: неоновый фиолетовый, индиго и сапфир",
            PrimaryAccent = Color.FromRgb(0x7C, 0x3A, 0xED),      // #7C3AED
            SecondaryAccent = Color.FromRgb(0x25, 0x63, 0xEB),    // #2563EB
            BgDarkStart = Color.FromRgb(0x0A, 0x0D, 0x18),
            BgDarkEnd = Color.FromRgb(0x10, 0x14, 0x26),
            PanelGlassColor = Color.FromArgb(0xBA, 0x14, 0x1A, 0x2D), // Полупрозрачный акрил
            PanelSolidColor = Color.FromRgb(0x16, 0x1B, 0x30),
            BorderColor = Color.FromArgb(0x60, 0x3E, 0x4E, 0x7E),
            Blob1 = Color.FromArgb(0x70, 0x3B, 0x82, 0xF6),
            Blob2 = Color.FromArgb(0x75, 0x7C, 0x3A, 0xED),
            Blob3 = Color.FromArgb(0xB0, 0x07, 0x09, 0x14),
            Blob4 = Color.FromArgb(0x45, 0x63, 0x66, 0xF1),
            Blob5 = Color.FromArgb(0x55, 0x06, 0xB6, 0xD4)
        },
        new()
        {
            Id = "Cyberpunk",
            Name = "⚡ Cyberpunk Cyan",
            Description = "Киберпанк: неоновая бирюза, аквамарин и графит",
            PrimaryAccent = Color.FromRgb(0x06, 0xB6, 0xD4),      // #06B6D4
            SecondaryAccent = Color.FromRgb(0x3B, 0x82, 0xF6),    // #3B82F6
            BgDarkStart = Color.FromRgb(0x05, 0x0D, 0x13),
            BgDarkEnd = Color.FromRgb(0x0B, 0x18, 0x22),
            PanelGlassColor = Color.FromArgb(0xB8, 0x0E, 0x1C, 0x27),
            PanelSolidColor = Color.FromRgb(0x11, 0x21, 0x2D),
            BorderColor = Color.FromArgb(0x50, 0x1E, 0x4D, 0x66),
            Blob1 = Color.FromArgb(0x65, 0x06, 0xB6, 0xD4),
            Blob2 = Color.FromArgb(0x60, 0x3B, 0x82, 0xF6),
            Blob3 = Color.FromArgb(0xB0, 0x04, 0x0A, 0x0E),
            Blob4 = Color.FromArgb(0x45, 0x0E, 0xA5, 0xE9),
            Blob5 = Color.FromArgb(0x50, 0x10, 0xB9, 0x81)
        },
        new()
        {
            Id = "Emerald",
            Name = "🌿 Emerald Classic",
            Description = "Классический зелёный стиль: изумруд и сочный нефрит",
            PrimaryAccent = Color.FromRgb(0x10, 0xB9, 0x81),      // #10B981
            SecondaryAccent = Color.FromRgb(0x05, 0x96, 0x69),    // #059669
            BgDarkStart = Color.FromRgb(0x07, 0x12, 0x0B),
            BgDarkEnd = Color.FromRgb(0x0E, 0x20, 0x14),
            PanelGlassColor = Color.FromArgb(0xB8, 0x10, 0x22, 0x17),
            PanelSolidColor = Color.FromRgb(0x14, 0x28, 0x1C),
            BorderColor = Color.FromArgb(0x50, 0x22, 0x58, 0x3A),
            Blob1 = Color.FromArgb(0x65, 0x10, 0xB9, 0x81),
            Blob2 = Color.FromArgb(0x60, 0x05, 0x96, 0x69),
            Blob3 = Color.FromArgb(0xB0, 0x04, 0x0C, 0x07),
            Blob4 = Color.FromArgb(0x45, 0x34, 0xD3, 0x99),
            Blob5 = Color.FromArgb(0x50, 0x14, 0xB8, 0xA6)
        },
        new()
        {
            Id = "Sunset",
            Name = "🌅 Sunset Pink",
            Description = "Закатный градиент: фуксия, горячий розовый и пурпур",
            PrimaryAccent = Color.FromRgb(0xEC, 0x48, 0x99),      // #EC4899
            SecondaryAccent = Color.FromRgb(0x8B, 0x5C, 0xF6),    // #8B5CF6
            BgDarkStart = Color.FromRgb(0x14, 0x08, 0x14),
            BgDarkEnd = Color.FromRgb(0x22, 0x0E, 0x23),
            PanelGlassColor = Color.FromArgb(0xB8, 0x25, 0x0F, 0x26),
            PanelSolidColor = Color.FromRgb(0x2D, 0x12, 0x2E),
            BorderColor = Color.FromArgb(0x50, 0x60, 0x25, 0x61),
            Blob1 = Color.FromArgb(0x65, 0xEC, 0x48, 0x99),
            Blob2 = Color.FromArgb(0x60, 0x8B, 0x5C, 0xF6),
            Blob3 = Color.FromArgb(0xB0, 0x0D, 0x04, 0x0D),
            Blob4 = Color.FromArgb(0x45, 0xF4, 0x3F, 0x5E),
            Blob5 = Color.FromArgb(0x50, 0xA8, 0x55, 0xF7)
        },
        new()
        {
            Id = "Crimson",
            Name = "🔥 Crimson Flame",
            Description = "Огненный стиль: рубиново-красный и пламя",
            PrimaryAccent = Color.FromRgb(0xEF, 0x44, 0x44),      // #EF4444
            SecondaryAccent = Color.FromRgb(0xF9, 0x73, 0x16),    // #F97316
            BgDarkStart = Color.FromRgb(0x14, 0x07, 0x07),
            BgDarkEnd = Color.FromRgb(0x22, 0x0B, 0x0B),
            PanelGlassColor = Color.FromArgb(0xB8, 0x25, 0x0E, 0x0E),
            PanelSolidColor = Color.FromRgb(0x2E, 0x12, 0x12),
            BorderColor = Color.FromArgb(0x50, 0x63, 0x22, 0x22),
            Blob1 = Color.FromArgb(0x65, 0xEF, 0x44, 0x44),
            Blob2 = Color.FromArgb(0x60, 0xF9, 0x73, 0x16),
            Blob3 = Color.FromArgb(0xB0, 0x0D, 0x04, 0x04),
            Blob4 = Color.FromArgb(0x45, 0xDC, 0x26, 0x26),
            Blob5 = Color.FromArgb(0x50, 0xE1, 0x1D, 0x48)
        },
        new()
        {
            Id = "Midnight",
            Name = "🌙 Midnight Dark",
            Description = "Минималистичный глубокий тёмный с сапфиром",
            PrimaryAccent = Color.FromRgb(0x3B, 0x82, 0xF6),      // #3B82F6
            SecondaryAccent = Color.FromRgb(0x63, 0x66, 0xF1),    // #6366F1
            BgDarkStart = Color.FromRgb(0x04, 0x05, 0x09),
            BgDarkEnd = Color.FromRgb(0x09, 0x0B, 0x13),
            PanelGlassColor = Color.FromArgb(0xB8, 0x0E, 0x10, 0x17),
            PanelSolidColor = Color.FromRgb(0x13, 0x15, 0x1F),
            BorderColor = Color.FromArgb(0x50, 0x24, 0x2A, 0x3E),
            Blob1 = Color.FromArgb(0x55, 0x3B, 0x82, 0xF6),
            Blob2 = Color.FromArgb(0x45, 0x63, 0x66, 0xF1),
            Blob3 = Color.FromArgb(0xC0, 0x02, 0x03, 0x06),
            Blob4 = Color.FromArgb(0x30, 0x1D, 0x4E, 0xD8),
            Blob5 = Color.FromArgb(0x35, 0x06, 0xB6, 0xD4)
        }
    };

    public static ThemeDefinition GetPreset(string? id) =>
        Presets.FirstOrDefault(p => string.Equals(p.Id, id, StringComparison.OrdinalIgnoreCase))
        ?? Presets[0]; // Goida by default

    public static void ApplyTheme(LauncherSettings settings, MainWindow? mainWindow = null)
    {
        var preset = GetPreset(settings.ThemePreset);

        Color primary = preset.PrimaryAccent;
        if (!string.IsNullOrWhiteSpace(settings.AccentColorHex))
        {
            try
            {
                primary = (Color)ColorConverter.ConvertFromString(settings.AccentColorHex)!;
            }
            catch
            {
                // Используем цвет из пресета
            }
        }

        var secondary = preset.SecondaryAccent;
        var darkColor = Darken(primary, 0.25);

        var res = Application.Current.Resources;

        // 1. Акцентные цвета
        var accentGreen = new SolidColorBrush(primary);
        accentGreen.Freeze();
        res["AccentGreen"] = accentGreen;

        var accentGreenDark = new SolidColorBrush(darkColor);
        accentGreenDark.Freeze();
        res["AccentGreenDark"] = accentGreenDark;

        res["AccentColorRaw"] = primary;

        var accentSecondary = new SolidColorBrush(secondary);
        accentSecondary.Freeze();
        res["AccentSecondary"] = accentSecondary;

        // 2. Фон приложения
        var bgGradient = new LinearGradientBrush(preset.BgDarkStart, preset.BgDarkEnd, new Point(0, 0), new Point(1, 1));
        bgGradient.Freeze();
        res["BgDark"] = bgGradient;

        // 3. Панели (Glassmorphism / акрил или Solid)
        var panelColor = settings.GlassmorphismEnabled ? preset.PanelGlassColor : preset.PanelSolidColor;
        var panelGradient = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(0, 1),
            GradientStops = new GradientStopCollection
            {
                new(panelColor, 0),
                new(Darken(panelColor, 0.12), 1)
            }
        };
        panelGradient.Freeze();
        res["BgPanel"] = panelGradient;

        var panelBorder = new SolidColorBrush(preset.BorderColor);
        panelBorder.Freeze();
        res["PanelBorderBrush"] = panelBorder;

        // 4. Неоновое свечение кнопки
        var glow = new DropShadowEffect
        {
            Color = primary,
            Direction = 0,
            ShadowDepth = 0,
            BlurRadius = settings.GlowEffectEnabled ? 26 : 0,
            Opacity = settings.GlowEffectEnabled ? 0.65 : 0
        };
        glow.Freeze();
        res["GlowShadow"] = glow;

        // 5. Градиент кнопки "ИГРАТЬ"
        var playBtnBrush = new LinearGradientBrush(primary, secondary, new Point(0, 0), new Point(1, 1));
        playBtnBrush.Freeze();
        res["PlayButtonBrush"] = playBtnBrush;

        // 6. Скругление углов
        res["PanelCornerRadius"] = new CornerRadius(settings.CornerRadiusValue);
        res["ButtonCornerRadius"] = new CornerRadius(Math.Max(6, settings.CornerRadiusValue - 2));

        // 7. Обновление визуалов главного окна
        mainWindow ??= Application.Current.MainWindow as MainWindow;
        mainWindow?.ApplyThemeVisuals(settings, preset);
    }

    private static Color Darken(Color color, double amount)
    {
        byte Scale(byte channel) => (byte)Math.Max(0, channel * (1 - amount));
        return Color.FromArgb(color.A, Scale(color.R), Scale(color.G), Scale(color.B));
    }
}
