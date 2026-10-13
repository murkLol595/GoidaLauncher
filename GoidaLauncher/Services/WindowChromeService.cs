using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace GoidaLauncher.Services;

/// <summary>
/// Красит системную рамку окна (заголовок, кнопки свернуть/закрыть) в тёмный режим
/// через DWM API — без этого Windows рисует белую рамку поверх тёмной темы приложения.
/// </summary>
public static class WindowChromeService
{
    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_NEW = 20; // Windows 10 1903+/11
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_OLD = 19; // ранние сборки Windows 10

    /// <summary>Вызывать после того, как окно физически создано (например, в обработчике Loaded).</summary>
    public static void ApplyDarkTitleBar(Window window)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int useDark = 1;
            var result = DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_NEW, ref useDark, sizeof(int));
            if (result != 0)
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_OLD, ref useDark, sizeof(int));
        }
        catch
        {
            // Старые версии Windows (до 1809) не поддерживают тёмную рамку — просто остаётся светлой
        }
    }
}
