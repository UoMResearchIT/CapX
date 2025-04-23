using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Helpers;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Reader,Finance")]
    public partial class FinanceSummary : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private InvoiceService InvoiceService { get; set; }

        [Inject]
        private PaymentService PaymentService { get; set; }

        [Inject]
        private FundingSourceService FundingSourceService { get; set; }

        private IList<FinanceSummaryItem> items;
        private RadzenDataGrid<FinanceSummaryItem> dataGrid;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            Loading = true;
            EnqueueLoadData(GetLoadTask);

            LogInformation("Viewing finance summary");
        }

        /// <summary>
        /// Navigate to finance items page
        /// </summary>
        private void GoToFinanceItems()
        {
            Navigation.NavigateTo("managefinancialitems");
        }

        /// <summary>
        /// Generates the load data task
        /// </summary>
        /// <returns></returns>
        private Task GetLoadTask()
        {
            return Task.Run(() =>
            {
                Debug.WriteLine($"** Loading finance data...");
                items = new List<FinanceSummaryItem>();
                var projects = ProjectService.GetAll(Context);
                var sources = FundingSourceService.GetAll(Context);
                foreach (var project in projects)
                {
                    var transactions = FinanceHelper.ComputeTransactionBreakdown(
                        Context,
                        project.SubTasks.SelectMany(x => x.AssignedResources),
                        sources.Where(x => x.Project.ProjectId == project.ProjectId),
                        InvoiceService.GetFundsRequested(Context, project.ProjectId),
                        PaymentService.GetFundsReceived(Context, project.ProjectId)
                    );

                    items.Add(
                        new FinanceSummaryItem(
                            project,
                            transactions
                        )
                    );
                }
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Debug.WriteLine($"** ...done! Item Count = {items.Count} | Status = {t.Status}");
                    Loading = false;
                    StateHasChanged();
                    dataGrid?.Reload();
                });
            });
        }
    }
}
