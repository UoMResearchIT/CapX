using System.Diagnostics;
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

        /// <summary>
        /// Extension method to get all messages from an exception and its inner exceptions.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public static string GetAllMessages(this Exception exp)
        {
            string message = string.Empty;
            Exception innerException = exp;

            do
            {
                message = message + (string.IsNullOrEmpty(innerException.Message) ? string.Empty : innerException.Message);
                innerException = innerException.InnerException;
            }
            while (innerException != null);

            return message;
        }
    }
}
