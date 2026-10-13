using GoidaLauncher.Models;

namespace GoidaLauncher.Services;

public static class RuleEvaluator
{
    /// <summary>
    /// Определяет, разрешено ли использование библиотеки/аргумента на текущей ОС (Windows),
    /// согласно списку правил из version.json Mojang.
    /// </summary>
    public static bool IsAllowed(List<Rule>? rules)
    {
        if (rules is null || rules.Count == 0)
            return true;

        bool allowed = false;

        foreach (var rule in rules)
        {
            bool matches = true;

            if (rule.Os is not null)
            {
                if (!string.IsNullOrEmpty(rule.Os.Name) &&
                    !string.Equals(rule.Os.Name, "windows", StringComparison.OrdinalIgnoreCase))
                {
                    matches = false;
                }

                if (matches && !string.IsNullOrEmpty(rule.Os.Arch))
                {
                    var currentArch = Environment.Is64BitOperatingSystem ? "x86_64" : "x86";
                    // Правила Mojang иногда используют "x86" для 32-бит, что нам достаточно для сопоставления
                    if (!rule.Os.Arch.Contains("64") && currentArch == "x86_64")
                    {
                        // 32-битное правило на 64-битной системе — не блокируем строго,
                        // Mojang использует это редко и в основном для старых natives
                    }
                }
            }

            if (rule.Features is not null && rule.Features.Count > 0)
            {
                // Мы не включаем спец-фичи (demo, custom_resolution, quick_play и т.п.)
                matches = false;
            }

            if (matches)
                allowed = string.Equals(rule.Action, "allow", StringComparison.OrdinalIgnoreCase);
        }

        return allowed;
    }
}
