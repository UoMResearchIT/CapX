using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageSettings : DataGridPage<Setting>
    {
        [Inject]
        private CssVariableService CssVariableService { get; set; }

        private string logo;

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
        /// Handles updates to a setting entity and refreshes the application theme if a colour setting is changed.
        /// </summary>
        /// <remarks>If the updated setting represents a colour value (identified by a value starting with
        /// '#'), the application theme is refreshed to reflect the change. This ensures that theme-related settings are
        /// applied immediately after an update.</remarks>
        /// <param name="entity">The setting entity that has been updated. Must not be null.</param>
        protected override async void OnUpdateRow(Setting entity)
        {
            base.OnUpdateRow(entity);

            // If successful update then do further processing
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
            StateHasChanged();
        }

        /// <summary>
        /// Saves the specified setting entity to the data store, updating the value if the setting type is an
        /// organisation logo explicitly.
        /// </summary>
        /// <remarks>If the setting type is <see cref="SettingType.OrganisationLogo"/>, the setting value
        /// is updated before saving. This method overrides the base implementation to handle special logic for
        /// organisation logo settings.</remarks>
        /// <param name="entity">The setting entity to save. Must not be null.</param>
        /// <returns>A task that represents the asynchronous save operation.</returns>
        protected override Task SaveRow(Setting entity)
        {
            // If it is the logo then we can update the field before saving to ensure the cached image is saved and the value isn't null
            if (entity.SettingType == SettingType.OrganisationLogo)
            {
                entity.SettingValue = logo ?? string.Empty;
            }

            return base.SaveRow(entity);
        }

        /// <summary>
        /// Handles an error that occurs during the logo upload process.
        /// </summary>
        /// <remarks>Call this method to log or respond to errors encountered when uploading a logo. The
        /// error details are provided in the event arguments.</remarks>
        /// <param name="args">The event data containing details about the upload error. Cannot be null.</param>
        private void OnLogoUploadError(UploadErrorEventArgs args)
        {
            LogError($"Logo failed to upload! {args.Message}");
        }
    }
}
