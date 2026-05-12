using PPMTool.Data.Entities;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ManageSettings : DataGridPage<Setting>
    {
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
    }
}
