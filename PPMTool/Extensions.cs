// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using PPMTool.Data.Enums;
using PPMTool.Services;
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
        /// <param name="settingsService"></param>
        /// <param name="cssVarService"></param>
        public static async Task SetDarkLightAsync(
            this ThemeService themeService,
            bool darkMode,
            SettingsService settingsService,
            CssVariableService cssVarService
        )
        {
            // Check if we need to do anything
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

                // Small delay to give the theme a chance to apply
                await Task.Yield();
            }

            // Get the appropriate colour from the settings
            var colour = darkMode
                ? settingsService.GetSetting(SettingType.AppPrimaryColourDark)
                : settingsService.GetSetting(SettingType.AppPrimaryColourLight);

            // Set the colour in the DOM
            await cssVarService.SetPrimaryColor(colour);
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
