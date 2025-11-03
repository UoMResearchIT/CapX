using Microsoft.AspNetCore.Authorization;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageFeatures : BasePage
    {
        private List<Feature> features;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            features = await FeatureService.GetAllFeaturesAsync(Context);
            Loading = false;

            LogInformation($"Viewing feature list");
        }

        /// <summary>
        /// Updates the state of the feature when toggled.
        /// </summary>
        /// <param name="feature"></param>
        private void OnFeatureToggled(Feature feature)
        {
            FeatureService.UpdateFeatureState(Context, feature);
            LogInformation($"Toggled feature '{feature.Name}' to {(feature.Enabled ? "enabled" : "disabled")}");
            Layout?.Render();
        }
    }
}