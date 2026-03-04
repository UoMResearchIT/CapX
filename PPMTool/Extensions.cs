using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using Radzen;

namespace PPMTool
{
    public static class Extensions
    {
        /// <summary>
        /// Sets to material dark or light mode with WCAG colours
        /// </summary>
        /// <param name="themeService"></param>
        /// <param name="darkMode"></param>
        public static void SetDarkLight(this ThemeService themeService, bool darkMode)
        {
            // Conditional setting
            if (themeService.IsDarkTheme() != darkMode)
            {
                Debug.WriteLine($"** Setting theme. Dark mode = {darkMode}");
                themeService.SetTheme(new ThemeOptions
                {
                    Theme = darkMode ? "material-dark" : "material",
                    Wcag = true,
                    TriggerChange = true,
                    RightToLeft = false
                });
            }
        }

        /// <summary>
        /// Whether the current theme is a dark theme. Assumes that dark themes have "dark" in the name.
        /// </summary>
        /// <param name="themeService"></param>
        /// <returns></returns>
        public static bool IsDarkTheme(this ThemeService themeService)
        {
            return themeService.Theme.ToLowerInvariant().Contains("dark");
        }
    }
}
