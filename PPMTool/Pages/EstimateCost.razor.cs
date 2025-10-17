using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using static PPMTool.Pages.Components.TaskConfigurationComponent;

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

        private Dictionary<string, TaskConfigurationComponent> resources;

        private TaskConfigurationComponent summaryComponent = null;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            resources = new Dictionary<string, TaskConfigurationComponent>
            {
                { "Leadership", new TaskConfigurationComponent() { ConfigModelUpdated = UpdateSummaryComponent } },
                { "RSE 1", new TaskConfigurationComponent() { ConfigModelUpdated = UpdateSummaryComponent } },
                { "RSE 2", new TaskConfigurationComponent() { ConfigModelUpdated = UpdateSummaryComponent } }
            };

            Loading = false;
        }

        /// <summary>
        /// Update the summary componet from all the resources
        /// </summary>
        /// <param name="name"></param>
        /// <param name="taskConfig"></param>
        private void UpdateSummaryComponent(string name, TaskConfigModel taskConfig)
        {
            if (summaryComponent == null)
            {
                Debug.WriteLine("** Summary task not ready!");
                return;
            }

            // Only include the leadership one if using the leadership model
            var resourcesToInclude = resources;
            if (costModel != CostModel.TechAndLeadership)
            {
                resourcesToInclude.Remove("Leadership");
            }

            // Compute the values for the summary
            summaryComponent.Model.StartDate = resourcesToInclude.Min(x => x.Value.Model.StartDate);
            summaryComponent.Model.EndDate = resourcesToInclude.Max(x => x.Value.Model.EndDate);
            summaryComponent.Model.DurationDays = resourcesToInclude.Max(x => x.Value.Model.DurationDays);
            summaryComponent.Model.DurationBillableDays = resourcesToInclude.Sum(x => x.Value.Model.DurationBillableDays);
            summaryComponent.Model.PlannedWorkHours = resourcesToInclude.Sum(x => x.Value.Model.PlannedWorkHours);
            summaryComponent.Model.PlannedCost = resourcesToInclude.Sum(x => x.Value.Model.PlannedCost);
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
