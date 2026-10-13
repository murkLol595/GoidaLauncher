using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using GoidaLauncher.Services;

namespace GoidaLauncher;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. Принудительно включаем аппаратный рендеринг GPU
        RenderOptions.ProcessRenderMode = RenderMode.Default;

        // 2. Стабильные плавные 60 FPS для всех анимаций WPF
        Timeline.DesiredFrameRateProperty.OverrideMetadata(
            typeof(Timeline),
            new FrameworkPropertyMetadata(60));

        var settings = SettingsService.Load();
        ThemeService.ApplyTheme(settings);

        base.OnStartup(e);
    }

    /// <summary>
    /// Меняет акцентный цвет интерфейса на лету (без перезапуска).
    /// </summary>
    public static void ApplyAccentColor(string hex)
    {
        var settings = SettingsService.Load();
        settings.AccentColorHex = hex;
        ThemeService.ApplyTheme(settings);
    }
}
