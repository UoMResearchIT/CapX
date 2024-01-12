using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Dynamic.Core;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ProjectDetails : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Parameter]
        public int? ProjectID { get; set; }

        private List<SubTask> data;
        private Project project;
        private List<ChartItem> chartSource = new List<ChartItem>();
        private ApexChartOptions<SubTask> options;
        private ApexChartOptions<ChartItem> options2;
        private PPMToolContext context;
        private int count;
        private string plannedCostColour;
        private string actualCostColour;
        private string fundsReceivedColour;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectID != null)
            {
                context = new PPMToolContext();
                project = ProjectService.GetById(context, ProjectID);
                data = project.SubTasks.OrderBy(x => x.StartDate).ToList();
                plannedCostColour = project.PlannedCost > project.Budget ? "red" : "green";
                actualCostColour = project.ActualCost > project.PlannedCost ? "red" : "green";
                fundsReceivedColour = project.FundsReceived < project.Budget ? "red" : "green";
                count = data.Count;

                options = new ApexChartOptions<SubTask>
                {
                    PlotOptions = new PlotOptions
                    {
                        Bar = new PlotOptionsBar
                        {
                            Horizontal = true
                        }
                    }
                };

                // Only show a burn-up chart if the project is actually happening
                if (!project.ProjectStatus.IsProjectFinishedOrCancelled())
                {
                    // Create the chart items
                    var temp = ChartHelper.AggregateSubTasksByWeek(
                        project.GetFullName(),
                        project.SubTasks,
                        task =>
                        {
                            // Value summed is the average contribution of the task for that week
                            // Duration includes weekends by default so only approximate
                            var durationWeeks = task.DurationDays / 7f < 1 ? 1 : task.DurationDays / 7f;
                            return task.PlannedWorkHours / durationWeeks;
                        }
                    ).ToList();

                    // Generate series by aggregating the values
                    double cumulative = 0;
                    foreach (var week in temp)
                    {
                        cumulative += week.Value1;
                        chartSource.Add(new ChartItem(null, week.Label, week.StartDate, week.EndDate, Math.Round(cumulative), 0, false));
                    }

                    // Early exit if chartSource has no data
                    if (chartSource.Count < 1) return;

                    // Create a new data point to indicate progress
                    var seriesStart = chartSource.Min(x => x.StartDate);
                    var seriesEnd = chartSource.Max(x => x.EndDate);
                    var actualsX = DateTime.Now.Date;
                    var actualsY = project.SubTasks.RoundedSum(x => x.ActualWorkHours);

                    // If the task has started yet or has already finished then x coordinate is the limits of the series
                    if (DateTime.Now.Date < seriesStart) actualsX = seriesStart;
                    else if (DateTime.Now.Date > seriesEnd) actualsX = seriesEnd;

                    // Set options
                    options2 = new ApexChartOptions<ChartItem>
                    {
                        Annotations = new Annotations
                        {
                            Yaxis = new List<AnnotationsYAxis>
                            {
                                new AnnotationsYAxis()
                                {
                                    Y = actualsY,
                                    BorderWidth = 2,
                                    StrokeDashArray = 5,
                                    BorderColor = "red"
                                }
                            },
                            Xaxis = new List<AnnotationsXAxis>
                            {
                                new AnnotationsXAxis()
                                {
                                    X = actualsX.ToUnixTimeMilliseconds(),
                                    BorderWidth = 2,
                                    StrokeDashArray = 5,
                                    BorderColor = "red"
                                }
                            }
                        }
                    };
                    InvokeAsync(StateHasChanged);
                }
            }
        }

        private void TaskSelected(SelectedData<SubTask> dataPoint)
        {
            if (!EditAuthorised) return;

            // Only so the navigation when in project view mode
            if (dataPoint.IsSelected)
            {
                var task = dataPoint.DataPoint.Items.FirstOrDefault();
                if (task == null) return;
                Debug.WriteLine($"** Selected {task.Name}. Navigating to task edit page...");
                Navigation.NavigateTo($"/addtask/{ProjectID}/{task.SubTaskId}");
            }
        }

        void EditTask(SubTask task)
        {
            Navigation.NavigateTo($"/addtask/{project.ProjectId}/{task.SubTaskId}");
        }

        void AddTask()
        {
            Navigation.NavigateTo($"/addtask/{project.ProjectId}/-1");
        }

        void EditProject()
        {
            Navigation.NavigateTo($"addproject/{project.ProjectId}");
        }

        // Necessary to ensure that we can filter the resources on the fly
        private void LoadData(LoadDataArgs args)
        {
            var query = project.SubTasks.ToList().AsQueryable();

            if (!string.IsNullOrEmpty(args.Filter))
            {
                // Filter via the Where method
                query = query.Where(args.Filter);
            }

            // Now apply the resources filter
            if (args.Filters != null && args.Filters.Count() > 0)
            {
                var filter = args.Filters.FirstOrDefault(x => x.Property == "Resources");
                var filterValue = filter?.FilterValue as string;
                if (filter != null && filterValue != null)
                {
                    query = query.Where(x => x.AssignedResources.Any(x => x.Person.ShortName.Contains(filterValue)));
                }
            }

            if (!string.IsNullOrEmpty(args.OrderBy))
            {
                // Sort via the OrderBy method
                query = query.OrderBy(args.OrderBy);
            }

            // Important!!! Make sure the Count property of RadzenDataGrid is set.
            count = query.Count();

            // Perform paging via Skip and Take.
            data = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
        }

        public class ActualPoint
        {
            public DateTime X { get; set; }
            public double Y { get; set; }

            public ActualPoint(DateTime x, double y)
            {
                X = x;
                Y = y;
            }
        }
    }
}
