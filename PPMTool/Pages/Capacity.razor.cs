using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class Capacity : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        private IDictionary<string, IEnumerable<SubTask>> groupedSubTasks;
        private ApexChart<ChartItem> chart;
        private List<ChartItem> chartSource;
        private ApexChartOptions<ChartItem> options;
        private List<string> nameOptions;
        private string chartTitle;
        private string tooltipText;
        private PPMToolContext context;

        private string chosenPerson;
        private string ChosenPerson
        {
            get => chosenPerson;
            set
            {
                if (chosenPerson != value)
                {
                    chosenPerson = value;

                    // Update the chart source
                    InvokeAsync(async () => await ConfigureSourceAsync());
                }
            }
        }

        private bool includeUnFunded = true;
        public bool IncludeUnFunded
        {
            get => includeUnFunded;
            set
            {
                if (includeUnFunded != value)
                {
                    includeUnFunded = value;

                    // Update the chart source
                    InvokeAsync(async () => await ConfigureSourceAsync());
                }
            }
        }

        public DateTime QueryStartDate { get; set; } = DateTime.Now.Date;
        public DateTime QueryEndDate { get; set; } = DateTime.Now.Date.AddDays(7);
        public IEnumerable<Person> QueryResults { get; private set; }

        protected override async Task OnInitializedAsync()
        {
            context = new PPMToolContext();
            options = new ApexChartOptions<ChartItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarOverlap = true
                    }
                },
                Legend = new Legend
                {
                    Show = false
                }
            };

            // Get dropdown options
            nameOptions = PersonService.GetAll(context).Select(p => p.Name).ToList();
            nameOptions.Sort();

            // Get data for chart
            await ConfigureSourceAsync();
            StateHasChanged();
        }

        private void RunQuery()
        {
            // Reset
            QueryResults = null;

            // Get all people
            var people = PersonService.GetAll(context);

            // Get all subtasks which run within the window
            var tasks = SubTaskService.GetAll(context).Where(x =>
            {
                return
                // Tasks that start in the window
                (x.StartDate >= QueryStartDate && x.StartDate < QueryEndDate) ||

                // Tasks that end in the window
                (x.EndDate > QueryStartDate && x.EndDate < QueryEndDate) ||

                // Tasks that span over the window
                (x.StartDate < QueryStartDate && x.EndDate >= QueryEndDate);
            });

            // Filter based on project status
            if (!IncludeUnFunded)
            {
                // Get tasks which ought to be excluded from the query
                var exemptTasks = ProjectService
                    .GetAll(context)
                    .Where(p => p.FundingStatus == FundingStatus.AwaitingSubmission || p.FundingStatus == FundingStatus.AwaitingOutcome)
                    .SelectMany(x => x.SubTasks);

                // Exclude tasks found in the exempt list
                tasks = tasks.Where(x => !exemptTasks.Contains(x));
            }

            // Map tasks to assigned resources
            var resources = tasks.SelectMany(x => x.AssignedResources);

            // Remove people who are assigned resources on those tasks
            QueryResults = people.Where(x => !resources.Any(y => y.Person == x));

            // Update the UI
            StateHasChanged();
        }

        private void OnDataPointHover(HoverData<ChartItem> e)
        {
            // HACK: This try-catch shouldn't be necessary but since the chart I see and the data behind it seem to be out of sync then I have no choice here.
            try
            {
                var item = e.Series.ApexSeries.Items.ElementAt(e.DataPointIndex);
                tooltipText = $"FTE: {item.Value}% | {item.StartDate.ToShortDateString()} - {item.EndDate.ToShortDateString()}";
            }
            catch { }
        }

        private void OnDataPointHoverLeave(HoverData<ChartItem> e)
        {
            tooltipText = null;
        }

        /// <summary>
        /// Pulls project info from the DB and packages the data into a plottable format
        /// </summary>
        private async Task ConfigureSourceAsync()
        {
            // Reset source
            chartSource = new List<ChartItem>();

            // Get people from the database
            var peo = PersonService.GetAll(context);
            if (peo.Count() > 0)
            {
                // Get projects from the database
                var projects = ProjectService.GetAll(context).Where(x => x.FundingStatus != FundingStatus.Finished);
                if (!IncludeUnFunded) projects = projects.Where(p => p.FundingStatus != FundingStatus.AwaitingSubmission && p.FundingStatus != FundingStatus.AwaitingOutcome);

                // Reinitialise dictionary
                groupedSubTasks = new Dictionary<string, IEnumerable<SubTask>>();

                // Flatten subtasks and group by person
                if (ChosenPerson == "All" || ChosenPerson == null)
                {
                    foreach (var p in peo)
                    {
                        // Create a list of subtasks to which this person is assigned
                        var assignments = new List<SubTask>();
                        foreach (var project in projects)
                        {
                            foreach (var subTask in project.SubTasks)
                            {
                                if (subTask.AssignedResources.Any(z => z.Person == p))
                                {
                                    assignments.Add(subTask);
                                }
                            }
                        }

                        // Add dictionary entry with person name as key
                        if (assignments.Count > 0) groupedSubTasks.Add(p.Name, assignments);
                    }

                    // Build chart source from the grouped data
                    foreach (var group in groupedSubTasks)
                    {
                        chartSource.AddRange(ChartHelper.AggregateByWeek(group.Value, x => x.AssignedResources.First(x => x.Person.Name == group.Key).Percentage, group.Key));
                    }
                }

                // Filter by person and flatten and group by project
                else
                {
                    // Create a list of subtasks for each project this person is assigned to
                    foreach (var project in projects)
                    {
                        var assignments = new List<SubTask>();
                        foreach (var subTask in project.SubTasks)
                        {
                            if (subTask.AssignedResources.Any(z => z.Person.Name == ChosenPerson))
                            {
                                assignments.Add(subTask);
                            }
                        }

                        // Add dictionary entry with project name as key
                        if (assignments.Count > 0) groupedSubTasks.Add(project.Name, assignments);
                    }

                    // Build chart source from the grouped data
                    foreach (var group in groupedSubTasks)
                    {
                        chartSource.AddRange(ChartHelper.AggregateByWeek(group.Value, x => x.AssignedResources.First(x => x.Person.Name == ChosenPerson).Percentage, group.Key));
                    }
                }
                
                chartTitle = $"Load for {ChosenPerson ?? "All"}";
                Debug.WriteLine($"** Finished configuring {chartTitle}. Include unfunded = {includeUnFunded}!");

                // First time this is called, there is no reference to the chart
                if (chart != null)
                {
                    Debug.WriteLine($"** Re-renderering chart!");
                    await chart?.UpdateSeriesAsync();
                    //await chart?.RenderAsync();
                }
                Debug.WriteLine($"** ChartSource has {chartSource?.Count()} entries!");
            }
        }
    }
}
