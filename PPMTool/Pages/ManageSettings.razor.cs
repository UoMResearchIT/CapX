using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageSettings : DataGridPage<Setting>
    {
        [Inject]
        private CssVariableService CssVariableService { get; set; }

        protected override async Task OnInitializedAsync()
        {
            Loading = true;
            await Task.Yield();

            await base.OnInitializedAsync();

            // Load the settings from the database
            dataGridEntityService = SettingsService;
            dataGridEntities = SettingsService
                .GetAll(Context)
                .OrderBy(x => x.SettingType.ToString())
                .ToList();

            LogInformation("ManageSettings page initialised and settings loaded.");
            Loading = false;
        }

        /// <summary>
        /// Handles updates to a setting entity and refreshes the application theme if a color setting is changed.
        /// </summary>
        /// <remarks>If the updated setting represents a color value (identified by a value starting with
        /// '#'), the application theme is refreshed to reflect the change. This ensures that theme-related settings are
        /// applied immediately after an update.</remarks>
        /// <param name="entity">The setting entity that has been updated. Must not be null.</param>
        protected override async void OnUpdateRow(Setting entity)
        {
            base.OnUpdateRow(entity);

            // If successful update then refresh the theme
            if (ErrorMessage == null)
            {
                // If this is a colour variable then we need to refresh the theme
                // Take a gamble that all values starting with # are colours
                if (entity.SettingValue.StartsWith("#"))
                {
                    LogInformation("Colour setting updated. Refreshing theme...");
                    var darkMode = ThemeService.IsDarkTheme();
                    await ThemeService.SetDarkLightAsync(darkMode, SettingsService, CssVariableService);
                }
            }
        }
    }
}
