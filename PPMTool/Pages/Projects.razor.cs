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

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> projects;
        RadzenDataGrid<Project> projectGrid;
        private List<List<ChartItem>> chartSource = new List<List<ChartItem>>();
        private ApexChartOptions<ChartItem> options;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private IEnumerable<ProjectStatus> fundingOptions = (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));
        private PPMToolContext context;

        private bool ShowActiveOnly { get; set; } = true;

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
            LoadProjectData();
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).OrderBy(x => x.Name).ToList();

            if (ShowActiveOnly) proj = proj.Where(x => x.ProjectStatus == ProjectStatus.Active || x.ProjectStatus == ProjectStatus.Maintenance).ToList();

            if (proj.Count > 0)
            {
                // Build the burn-up charts week by week
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
