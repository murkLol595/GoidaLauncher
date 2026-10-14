using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GoidaLauncher.Services;

public static class MemoryOptimizer
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private class MEMORYSTATUSEX
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX()
        {
            dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
        }
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

    [DllImport("psapi.dll")]
    private static extern int EmptyWorkingSet(IntPtr hwProc);

    /// <summary>
    /// Возвращает суммарный объём физической оперативной памяти системы в мегабайтах.
    /// </summary>
    public static int GetTotalPhysicalMemoryMb()
    {
        try
        {
            var mem = new MEMORYSTATUSEX();
            if (GlobalMemoryStatusEx(mem))
            {
                return (int)(mem.ullTotalPhys / (1024 * 1024));
            }
        }
        catch
        {
            // Fallback
        }

        return 8192; // 8 ГБ по умолчанию при ошибке определения
    }

    /// <summary>
    /// Рассчитывает оптимальное выделение ОЗУ для Minecraft под текущую систему.
    /// </summary>
    public static (int MinRamMb, int MaxRamMb) GetRecommendedRamPreset()
    {
        int totalMb = GetTotalPhysicalMemoryMb();

        if (totalMb >= 16000)
        {
            // 16 ГБ и выше: 6 ГБ — золотой стандарт для модов, тяжелых текстур и шейдеров без GC-статтеров
            return (1024, 6144);
        }
        if (totalMb >= 8000)
        {
            // 8-16 ГБ: 4 ГБ
            return (1024, 4096);
        }

        // Менее 8 ГБ: 2 ГБ
        return (512, 2048);
    }

    /// <summary>
    /// Принудительно освобождает неиспользуемую память лаунчера (GC + сброс рабочего набора страниц).
    /// </summary>
    public static void TrimProcessMemory()
    {
        try
        {
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);
            GC.WaitForPendingFinalizers();
            GC.Collect(2, GCCollectionMode.Aggressive, true, true);

            var processHandle = Process.GetCurrentProcess().Handle;
            EmptyWorkingSet(processHandle);
        }
        catch
        {
            // Игнорируем в случае ограничений прав
        }
    }
}
