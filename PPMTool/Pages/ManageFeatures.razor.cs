using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageFeatures : BasePage
    {
        [Inject]
        private FeatureService FeatureService { get; set; }

        private List<Feature> features;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            features = FeatureService.GetAllFeatures(Context);

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
        }
    }
}