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
using System.Diagnostics;
using PPMTool.Pages.Components;

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> projects;
        RadzenDataGrid<Project> projectGrid;
        private List<List<ChartItem>> chartSource;
        private ApexChartOptions<ChartItem> options;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private IEnumerable<ProjectStatus> fundingOptions = (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));
        private List<ProjectSummaryWidget.ProjectSummaryData> summaryData;
        private PPMToolContext context;

        private bool ShowActiveOnly { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LoadProjectData();

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

        private void OnChange(bool? value)
        {
            Debug.WriteLine("Change detected. Reloading data...");
            LoadProjectData();
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).OrderBy(x => x.Name).ToList();

            // Build the summary widget data and sort by total
            summaryData = new List<ProjectSummaryWidget.ProjectSummaryData>();
            foreach (var p in portfolioOptions)
            {
                summaryData.Add(new ProjectSummaryWidget.ProjectSummaryData
                {
                    Portfolio = p,
                    Active = proj.Where(x => x.Portfolio == p && (x.ProjectStatus == ProjectStatus.Active || x.ProjectStatus == ProjectStatus.Paused || x.ProjectStatus == ProjectStatus.Maintenance)).Count(),
                    Incoming = proj.Where(x => x.Portfolio == p && (x.ProjectStatus == ProjectStatus.Unfunded || x.ProjectStatus == ProjectStatus.Funded)).Count(),
                    Complete = proj.Where(x => x.Portfolio == p && x.ProjectStatus == ProjectStatus.Finished).Count()
                });
            }
            summaryData = summaryData.OrderByDescending(x=>x.GetTotal()).ToList();
            

            // Remove the ones that are not active for the data grid if necessary
            if (ShowActiveOnly) proj = proj.Where(x => x.ProjectStatus != ProjectStatus.Finished).ToList();

            // If we have any left then build the rest
            if (proj.Count > 0)
            {
                // Build the burn-up charts week by week
                chartSource = new List<List<ChartItem>>();
                for (int i = 0; i < proj.Count; ++i)
                {
                    // Update the summary of the project and save back to DB
                    var p = proj[i];
                    p.UpdateProjectSummary();
                    ProjectService.Update(context, p);

                    // Create the chart items
                    var listOfChartItems = ChartHelper.AggregateByWeek(
                        p.Name,
                        p.SubTasks,
                        x =>
                        {
                            // Value summed is the average contribution of the task for that week
                            // Duration includes weekends by default so only approximate
                            var durationWeeks = x.DurationDays / 7f;
                            return x.PlannedWorkHours / durationWeeks;
                        },
                        x =>
                        {
                            // Same for actuals
                            var durationWeeks = x.DurationDays / 7f;
                            return x.ActualWorkHours / durationWeeks;
                        }
                    );

                    // Wrap in a list
                    var list = new List<ChartItem>();
                    list.AddRange(listOfChartItems);

                    // Add to chart source
                    chartSource.Add(list);
                }

                // Assign data for the data grid
                projects = proj;
            }

            Debug.WriteLine($"{proj.Count()} projects loaded.");
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
