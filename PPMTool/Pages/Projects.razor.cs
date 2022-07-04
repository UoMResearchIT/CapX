using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class Projects : ComponentBase
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private NavigationManager NavigationManager { get; set; }

        private IEnumerable<Project> projects;
        RadzenDataGrid<Project> projectGrid;
        private List<ChartItem> chartSource = new List<ChartItem>();
        private ApexChartOptions<ChartItem> options;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private IEnumerable<FundingStatus> fundingOptions = (FundingStatus[])Enum.GetValues(typeof(FundingStatus));

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            using var context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).ToArray();
            if (proj.Count() > 0)
            {
                projects = proj;

                // Organise the project data so it is plottable
                foreach (var p in proj)
                {
                    chartSource.AddRange(ChartHelper.AggregateByWeek(p.SubTasks, x =>
                    {
                        // Value summed is the average contruibution of the task for that week
                        var durationWeeks = x.DurationHours / (7 * 7);
                        return x.PlannedWorkHours / durationWeeks;
                    }, p.Name));
                }
            }

            options = new ApexChartOptions<ChartItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarGroupRows = false
                    }
                }
            };
        }

        private void ProjectClicked(int id)
        {
            NavigationManager.NavigateTo($"/projectdetails/{id}");
        }

        async Task EditRow(Project project)
        {
            await projectGrid.EditRow(project);
        }

        void OnUpdateRow(Project project)
        {
            var context = new PPMToolContext();
            ProjectService.Update(context, project);
        }

        async Task SaveRow(Project project)
        {
            await projectGrid.UpdateRow(project);
        }

        void CancelEdit(Project project)
        {
            projectGrid.CancelEditRow(project);
            var context = new PPMToolContext();
            ProjectService.RevertChanges(context, project);
            projects = ProjectService.GetAll(context).ToArray();
        }
    }
}
