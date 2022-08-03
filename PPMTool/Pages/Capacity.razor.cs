using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.Build.Framework;
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
        public IEnumerable<CapacityQueryItem> QueryResults { get; private set; }

        public string QueryErrorMessage { get; private set; }

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

        private async void ClearQueryAsync()
        {
            QueryResults = null;
            QueryErrorMessage = null;
            await ConfigureSourceAsync();
            StateHasChanged();
        }

        /// <summary>
        /// Runs the capacity query and updates the query result property
        /// </summary>
        private async void RunQueryAsync()
        {
            // Add error
            if (QueryStartDate >= QueryEndDate)
            {
                QueryErrorMessage = "End date must be after the start date!";
                return;
            }

            // Reset query results
            QueryResults = null;
            var results = new List<CapacityQueryItem>();

            // Update the chart source
            await ConfigureSourceAsync(QueryStartDate, QueryEndDate);
            StateHasChanged();

            // Get all people
            var people = PersonService.GetAll(context);

            // Get all the subtasks within the query window
            var tasks = GetSubTasksWithinQueryWindow(SubTaskService.GetAll(context));

            // Get all the resources who are not assigned to any subtasks and add them to the query results
            var unassigned = people.Where(p => !tasks.Any(t => t.AssignedResources.Any(r => r.Person == p)));
            foreach (var person in unassigned)
            {
                results.Add(new CapacityQueryItem(person, QueryStartDate, QueryEndDate, 100));
            }

            // Invert the chart results and add to array
            foreach (var item in chartSource)
            {
                if ((int)item.Value1 < 100)
                {
                    // Get person from name
                    var person = people.FirstOrDefault(p => p.Name == item.Label);
                    if (person == null)
                    {
                        Debug.WriteLine($"** Couldn't find person {item.Label}");
                        continue;
                    }

                    // Add to range
                    results.Add(new CapacityQueryItem(person, item.StartDate, item.EndDate, 100 - (int)item.Value1));
                }
            }

            // Assign results
            QueryResults = results;

            // Update the UI
            StateHasChanged();
        }

        private IEnumerable<SubTask> GetSubTasksWithinQueryWindow(IEnumerable<SubTask> source)
        {
            // Filter all the tasks based on the window of the query
            var tasks = source.Where(x =>
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
                    .GetUnfundedProjects(context)
                    .SelectMany(x => x.SubTasks);

                // Exclude tasks found in the exempt list
                tasks = tasks.Where(x => !exemptTasks.Contains(x));
            }

            return tasks;
        }

        private void OnDataPointHover(HoverData<ChartItem> e)
        {
            // HACK: This try-catch shouldn't be necessary but since the chart I see and the data behind it seem to be out of sync then I have no choice here.
            try
            {
                var item = e.Series.ApexSeries.Items.ElementAt(e.DataPointIndex);
                tooltipText = $"FTE: {item.Value1}% | {item.StartDate.ToShortDateString()} - {item.EndDate.ToShortDateString()}";
            }
            catch { }
        }

        private void OnDataPointHoverLeave(HoverData<ChartItem> e)
        {
            tooltipText = null;
        }

        /// <summary>
        /// Pulls project info from the DB and packages the data into a plottable format
        /// Can specific a start and end date to restrict the data window
        /// </summary>
        private async Task ConfigureSourceAsync(DateTime? startDate = null, DateTime? endDate = null)
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
                        chartSource.AddRange(ChartHelper.AggregateByWeekIntoBlocks(group.Value, x => x.AssignedResources.First(x => x.Person.Name == group.Key).Percentage, group.Key, startDate, endDate));
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
                        chartSource.AddRange(ChartHelper.AggregateByWeekIntoBlocks(group.Value, x => x.AssignedResources.First(x => x.Person.Name == ChosenPerson).Percentage, group.Key, startDate, endDate));
                    }
                }
                
                chartTitle = $"Load for {ChosenPerson ?? "All"}";
                Debug.WriteLine($"** Finished configuring {chartTitle}. Include unfunded = {includeUnFunded}!");

                // First time this is called, there is no reference to the chart
                if (chart != null)
                {
                    Debug.WriteLine($"** Re-renderering chart!");

                    // HACK: Not sure why we have to call this twice but we do!
                    await chart?.UpdateSeriesAsync();
                    await chart?.UpdateSeriesAsync();
                    await InvokeAsync(StateHasChanged);
                }
                Debug.WriteLine($"** ChartSource has {chartSource?.Count()} entries!");
            }
        }
    }
}
