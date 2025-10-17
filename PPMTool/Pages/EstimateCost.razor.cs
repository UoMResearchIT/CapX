using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using PPMTool.Services;
using static PPMTool.Pages.Components.TaskConfigurationComponent;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class EstimateCost : BasePage
    {
        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

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

        private Dictionary<string, TaskConfigModel> models;

        private TaskConfigurationComponent summaryComponent = null;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            models = new Dictionary<string, TaskConfigModel>
            {
                { "Leadership", new TaskConfigModel(FinancialReferenceService, Context) },
                { "RSE 1", new TaskConfigModel(FinancialReferenceService, Context) },
                { "RSE 2", new TaskConfigModel(FinancialReferenceService, Context) }
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
            Debug.WriteLine($"** EstimateCost: Updating the summary model triggered by {name}");

            if (summaryComponent == null)
            {
                Debug.WriteLine("** Summary task not ready!");
                return;
            }

            // Only include the leadership one if using the leadership model
            var resourcesToInclude = new List<TaskConfigModel>();
            foreach (var model in models)
            {
                if (ShouldIncludeResource(model))
                {
                    resourcesToInclude.Add(model.Value);
                }
            }

            // Compute the values for the summary
            summaryComponent.Model.StartDate = resourcesToInclude.Min(x => x.StartDate);
            summaryComponent.Model.EndDate = resourcesToInclude.Max(x => x.EndDate);
            summaryComponent.Model.DurationDays = resourcesToInclude.Max(x => x.DurationDays);
            summaryComponent.Model.DurationBillableDays = resourcesToInclude.Sum(x => x.DurationBillableDays);
            summaryComponent.Model.PlannedWorkHours = resourcesToInclude.Sum(x => x.PlannedWorkHours);
            summaryComponent.Model.PlannedCost = resourcesToInclude.Sum(x => x.PlannedCost);
            StateHasChanged();
        }

        /// <summary>
        /// Whether a resource should be included in the calculation based on the cost model in use
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool ShouldIncludeResource(KeyValuePair<string, TaskConfigModel> model)
        {
            return CostModel == CostModel.TechAndLeadership || (CostModel != CostModel.TechAndLeadership && model.Key != "Leadership");
        }

        /// <summary>
        /// Updates the cost model for all resource components when the selected cost model changes.
        /// </summary>
        private void UpdateComponentCostModels()
        {
            foreach (var resource in models)
            {
                Debug.WriteLine($"** EstimateCost: Setting cost model to {CostModel} on {resource.Key}");
                resource.Value.CostModel = CostModel;
            }
        }
    }
}
