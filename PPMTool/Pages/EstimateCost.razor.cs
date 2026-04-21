using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Enums;
using PPMTool.Services;
using static PPMTool.Data.Extensions;
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
                    CostModelChanged();
                }
            }
        }

        private Dictionary<string, TaskConfigModel> models;
        private TaskConfigModel summaryModel;
        private bool isUsable = true;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            try
            {
                FinancialReferenceService.GetFinancialReferenceForDate(Context, DateTime.Today);
                summaryModel = new TaskConfigModel(FinancialReferenceService, Context, false);
                models = new Dictionary<string, TaskConfigModel>
                {
                    { "Leadership", new TaskConfigModel(FinancialReferenceService, Context, true) },
                    { "RSE 1", new TaskConfigModel(FinancialReferenceService, Context, false) }
                };
                CostModel = CostModel.TechAndLeadershipWithIndirects;
            }
            catch (FinancialRefException e)
            {
                isUsable = false;
            }
            Loading = false;
        }

        /// <summary>
        /// Update the summary component from all the resources
        /// </summary>
        /// <param name="name"></param>
        private void UpdateSummaryComponent(string name)
        {
            Debug.WriteLine($"** EstimateCost: Updating the summary model triggered by {name}");

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
            summaryModel.SetCostModel(CostModel, false);
            summaryModel.StartDate = resourcesToInclude.Min(x => x.StartDate);
            summaryModel.EndDate = resourcesToInclude.Max(x => x.EndDate);
            summaryModel.DurationDays = resourcesToInclude.Max(x => x.DurationDays);
            summaryModel.DurationBillableDays = resourcesToInclude.Sum(x => x.DurationBillableDays);
            summaryModel.PlannedWorkHours = resourcesToInclude.Sum(x => x.PlannedWorkHours);
            summaryModel.PlannedCost = resourcesToInclude.Sum(x => x.PlannedCost);
            summaryModel.PlannedIndirectCost = resourcesToInclude.Sum(x => x.PlannedIndirectCost);
            StateHasChanged();
        }

        /// <summary>
        /// Whether a resource should be included in the calculation based on the cost model in use
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private bool ShouldIncludeResource(KeyValuePair<string, TaskConfigModel> model)
        {
            return CostModel.HasLeadership() || (!CostModel.HasLeadership() && model.Key != "Leadership");
        }

        /// <summary>
        /// Updates the cost model for all resource components when the selected cost model changes.
        /// </summary>
        private void CostModelChanged()
        {
            foreach (var resource in models)
            {
                Debug.WriteLine($"** EstimateCost: Setting cost model to {CostModel} on {resource.Key}");
                resource.Value.SetCostModel(CostModel);
            }
            UpdateSummaryComponent("Self: Cost Model");
        }

        /// <summary>
        /// Add a resource to the list
        /// </summary>
        private void AddResource()
        {
            var numRes = models.Where(x => x.Key != "Leadership").Count() + 1;
            var res = new TaskConfigModel(FinancialReferenceService, Context, false);
            models.Add($"RSE {numRes}", res);
            res.SetCostModel(CostModel);
            UpdateSummaryComponent("Self: Add Resource");
        }

        /// <summary>
        /// Delete a resource
        /// </summary>
        /// <param name="key"></param>
        private void DeleteResource(string key)
        {
            models.Remove(key);
            UpdateSummaryComponent(key);
            StateHasChanged();
        }
    }
}
