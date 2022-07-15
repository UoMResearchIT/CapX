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
using FluentDate;

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> projects;
        RadzenDataGrid<Project> projectGrid;
        private List<ChartItem> chartSource = new List<ChartItem>();
        private ApexChartOptions<ChartItem> options;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private IEnumerable<FundingStatus> fundingOptions = (FundingStatus[])Enum.GetValues(typeof(FundingStatus));
        private PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get people from the database
            context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).ToArray();
            if (proj.Count() > 0)
            {
                projects = proj;

                // Organise the project data so it is plottable
                foreach (var p in proj)
                {
                    // Update the summary of the project and save back to DB
                    p.UpdateProjectSummary();
                    ProjectService.Update(context, p);

                    // Add to chart source
                    chartSource.AddRange(ChartHelper.AggregateByWeek(p.SubTasks, x =>
                    {
                        // Value summed is the average contribution of the task for that week
                        // Duration includes weekends by default
                        var durationWeeks = x.DurationDays / 7f;
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
            Navigation.NavigateTo($"/projectdetails/{id}");
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }

        private void EditProject(Project project)
        {
            Navigation.NavigateTo($"/addproject/{project.ProjectId}");
        }
    }
}
