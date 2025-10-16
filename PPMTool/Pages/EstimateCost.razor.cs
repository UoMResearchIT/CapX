using Microsoft.AspNetCore.Authorization;
using PPMTool.Enums;
using PPMTool.Pages.Components;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class EstimateCost : BasePage
    {
        private CostModel costModel;
        private CostModel CostModel
        {
            get => costModel;
            set
            {
                if (costModel != value)
                {
                    costModel = value;
                    UpdateComponentCostModels();
                }
            }
        }

        private Dictionary<string, TaskConfigurationComponent> resources = new Dictionary<string, TaskConfigurationComponent>
        {
            { "Leadership", new TaskConfigurationComponent() },
            { "RSE 1", new TaskConfigurationComponent() },
            { "RSE 2", new TaskConfigurationComponent() }
        };

        private TaskConfigurationComponent summaryTask;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = false;
        }

        /// <summary>
        /// Updates the cost model for all resource components when the selected cost model changes.
        /// </summary>
        private void UpdateComponentCostModels()
        {
            foreach (var resource in resources.Values)
            {
                resource.CostModel = CostModel;
            }
        }
    }
}
