using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ProjectDetails : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private Project project;

        [Parameter]
        public int? ProjectID { get; set; }

        private List<SubTask> Data { get; set; }
        private List<ChartItem> chartSource = new List<ChartItem>();
        private ApexChartOptions<SubTask> options;
        private ApexChartOptions<ChartItem> options2;
        ApexChart<ChartItem> chart;
        private PPMToolContext context;
        private int count;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectID != null)
            {
                context = new PPMToolContext();
                project = ProjectService.GetById(context, ProjectID);
                Data = project.SubTasks.OrderBy(x => x.StartDate).ToList();
                count = Data.Count;

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

                options2 = new ApexChartOptions<ChartItem>
                {
                    PlotOptions = new PlotOptions
                    {
                        Bar = new PlotOptionsBar
                        {
                            Horizontal = true,
                            RangeBarGroupRows = false
                        }
                    },
                    Annotations = new Annotations()
                };

                // Only show a burn-up chart if the project is actually happening
                if (project.ProjectStatus != Enums.ProjectStatus.Finished && project.ProjectStatus != Enums.ProjectStatus.Cancelled)
                {
                    // Create the chart items
                    var temp = ChartHelper.AggregateByWeek(
                        project.Name,
                        project.SubTasks,
                        task =>
                        {
                            // Value summed is the average contribution of the task for that week
                            // Duration includes weekends by default so only approximate
                            var durationWeeks = task.DurationDays / 7f <  1 ? 1 : task.DurationDays / 7f;
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

                    // Create an annotation to indicate progress
                    var seriesStart = chartSource.Min(x => x.StartDate);
                    var seriesEnd = chartSource.Max(x => x.EndDate);
                    DateTime annotationX = DateTime.Now.Date;
                    double annotationY = project.SubTasks.Sum(x => x.ActualWorkHours);

                    // If the task has started yet or has already finished then x coordinate is the limits of the series
                    if (DateTime.Now.Date < seriesStart) annotationX = seriesStart;
                    else if (DateTime.Now.Date > seriesEnd) annotationX = seriesEnd;

                    // Add annotation
                    if (options2.Annotations.Points == null) options2.Annotations.Points = new List<AnnotationsPoint>();
                    else options2.Annotations.Points.Clear();
                    options2.Annotations.Points.Add(new AnnotationsPoint
                    {
                        X = annotationX,
                        Y = annotationY,
                        Marker = new AnnotationMarker
                        {
                            Size = 6,
                            FillColor = "#fff",
                            StrokeColor = "#2698FF",
                            Radius = 2
                        },
                        Label = new Label
                        {
                            BorderColor = "#FF4560",
                            OffsetY = 0,
                            Style = new Style
                            {
                                Color = "#fff",
                                Background = "#FF4560"
                            },
                            Text = $"Actual Work: {project.SubTasks.Sum(x => x.ActualWorkHours)} hours"
                        }
                    });
                    InvokeAsync(async () => await chart?.UpdateOptionsAsync(true, true, true));
                }
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
            Data = query.Skip(args.Skip.Value).Take(args.Top.Value).ToList();
        }
    }
}
