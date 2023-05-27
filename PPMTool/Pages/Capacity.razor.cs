using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

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

        [Inject]
        private IJSRuntime JS { get; set; }

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

        private bool includeLeavers = false;
        public bool IncludeLeavers
        {
            get => includeLeavers;
            set
            {
                if (includeLeavers != value)
                {
                    includeLeavers = value;

                    // Update the chart source
                    InvokeAsync(async () => await ConfigureSourceAsync());
                }
            }
        }

        private DateTime queryStartDate = DateTime.Now.Date;
        public DateTime QueryStartDate
        {
            get => queryStartDate;
            set
            {
                queryStartDate = value;

                // Update the end date to be a week ahead of the start date by default if it is behind
                if (queryEndDate < queryStartDate) queryEndDate = queryStartDate.AddDays(7);
            }
        }

        private IDictionary<string, IEnumerable<SubTask>> groupedSubTasks;
        private ApexChart<ChartItem> chart;
        private List<ChartItem> chartSource;
        private ApexChartOptions<ChartItem> options;
        private List<string> nameOptions;
        private string chartTitle;
        private PPMToolContext context;
        private DateTime queryEndDate = DateTime.Now.Date.AddDays(7);
        private IEnumerable<CapacityQueryItem> queryResults;
        private string queryErrorMessage;
        private bool queryActive;
        private int requiredFTE = 50;
        private List<CapacityQueryItem> fullMatch;
        private List<CapacityQueryItem> partialMatchPercent;
        private List<CapacityQueryItem> partialMatchDuration;
        private List<CapacityQueryItem> partialMatchBoth;

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
                },
                Xaxis = new XAxis { },
                Fill = new Fill
                {
                    Pattern = new FillPattern
                    {
                        Style = FillPatternStyle.SlantedLines
                    }
                }
            };

            // Get dropdown options
            nameOptions = PersonService.GetAll(context).OrderBy(x => x.Name).Select(p => p.Name).ToList();
            nameOptions.Sort();

            // Get data for chart
            await ConfigureSourceAsync();
            StateHasChanged();
        }

        /// <summary>
        /// Method to handle when a series element on the chart is selected
        /// </summary>
        /// <param name="dataPoint"></param>
        private void DataPointsSelected(SelectedData<ChartItem> dataPoint)
        {
            // Only so the navigation when in project view mode
            if (dataPoint.IsSelected && ChosenPerson != "All" && ChosenPerson != null)
            {
                var projectName = dataPoint.DataPoint.Items.FirstOrDefault()?.Label;
                Debug.WriteLine($"** Selected {projectName}. Navigating to details page...");

                // Use the title of the task to find its projectID then navigate to the details page
                var project = ProjectService.GetAll(context).FirstOrDefault(x => x.GetFullName() == projectName);
                if (project != null)
                {
                    Navigation.NavigateTo($"/projectdetails/{project.ProjectId}");
                }
            }
        }

        /// <summary>
        /// Resets the page to its initial state
        /// </summary>
        private async Task ClearQueryAsync()
        {
            Debug.WriteLine("** Clearing Query...");
            queryResults = null;
            queryErrorMessage = null;
            queryActive = false;

            // Reset the person filter but don't trigger the refresh.
            // Do it manually so we can force synchronisation.
            chosenPerson = "All";
            await ConfigureSourceAsync();
        }

        /// <summary>
        /// Runs the capacity query and updates the query result property
        /// </summary>
        private async void RunQueryAsync()
        {
            Debug.WriteLine("** Running query...");

            // Add error
            if (QueryStartDate >= queryEndDate)
            {
                queryErrorMessage = "End date must be after the start date!";
                return;
            }

            // Reset query results
            await ClearQueryAsync();
            queryActive = true;
            queryErrorMessage = null;
            var results = new List<CapacityQueryItem>();

            // Update the chart source as this is used
            await ConfigureSourceAsync();
            StateHasChanged();

            // Get all people
            var people = PersonService.GetAll(context).OrderBy(x => x.Name);

            // Get all the subtasks within the query window
            var tasks = GetSubTasksWithinQueryWindow(SubTaskService.GetAll(context));

            // Part 1: Get all the resources who are not assigned to any subtasks and add them to the query results
            var unassigned = people.Where(p => !tasks.Any(t => t.AssignedResources.Any(r => r.Person == p)));
            foreach (var person in unassigned)
            {
                // Get any availability changes in force at the beginning of the query or during it
                var changes = person.AvailabilityChanges.Where(x => x.ChangeDate < queryEndDate).ToList();

                // Add to the changes any leaving date as a zero availability
                if (person.EndDate != null)
                {
                    changes.Add(new AvailabilityChange()
                    {
                        Person = person,
                        ChangeDate = person.EndDate ?? DateTime.Now,
                        AvailabilityFTE = 0
                    });

                    // Remove all availability changes after the leaving date as these are unnecessary
                    changes = changes.Where(x => x.ChangeDate <= person.EndDate).ToList();
                }

                // Sort by date
                changes = changes.OrderBy(x => x.ChangeDate).ToList();

                // If no changes then use post FTE in a single result
                if (changes.Count == 0)
                {
                    results.Add(new CapacityQueryItem(person, QueryStartDate, queryEndDate, (int)(person.FTE * 100 / .84)));
                }
                else
                {
                    // We need to establish the availability at the beginning of the query window which will be post FTE by default
                    double initialFTE = person.FTE;

                    // Find the change immediately before the query window or on day one if there is one
                    var changeBefore = changes.Where(x => x.ChangeDate <= QueryStartDate).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
                    if (changeBefore != null) initialFTE = changeBefore.AvailabilityFTE;
                    var changesAfter = changes.Where(x => x.ChangeDate > QueryStartDate).OrderBy(x => x.ChangeDate).ToList();

                    // First period uses the initial FTE up to the first change after the window begins or the end of the window if there isn't any changes after
                    if (initialFTE > 0)
                    {
                        results.Add(new CapacityQueryItem(person, QueryStartDate, changesAfter.FirstOrDefault()?.ChangeDate ?? queryEndDate, (int)(initialFTE * 100 / .84)));
                    }

                    // Subsequent ones use the latest change information
                    for (int i = 0; i < changesAfter.Count; ++i)
                    {
                        // If the last change then use query end date for result
                        if (i == changesAfter.Count - 1)
                        {
                            // Filter out availability of less than a day or 0%
                            if (queryEndDate != changesAfter[i].ChangeDate && changesAfter[i].AvailabilityFTE != 0)
                            {
                                results.Add(new CapacityQueryItem(person, changesAfter[i].ChangeDate, queryEndDate, (int)(changesAfter[i].AvailabilityFTE * 100 / .84)));
                            }
                        }
                        else
                        {
                            // Filter out availability of less than a day or 0%
                            if (changesAfter[i + 1].ChangeDate != changesAfter[i].ChangeDate && changesAfter[i].AvailabilityFTE != 0)
                            {
                                results.Add(new CapacityQueryItem(person, changesAfter[i].ChangeDate, changesAfter[i + 1].ChangeDate, (int)(changesAfter[i].AvailabilityFTE * 100 / .84)));
                            }
                        }
                    }
                }
            }

            // Part 2: Invert the chart data and add to results array
            foreach (var item in chartSource)
            {
                // Get person from item label
                var person = people.FirstOrDefault(p => p.Name == item.Label);
                if (person == null)
                {
                    Debug.WriteLine($"** Couldn't find person {item.Label}");
                    continue;
                }

                // Availability of individual is value 2 in the chart item (converted here to a percentage rather than an FTE)
                var availabilityPercentage = (int)(item.Value2 * 100 / .84);

                // Invert value (value 1 here is the assignment value as a percentage)
                var unassignedPercentage = availabilityPercentage - (int)item.Value1;

                // Only add if the block (item) has a non-zero length and the person isn't already over-allocated which would give a negative inverse
                if (item.StartDate != item.EndDate && unassignedPercentage > 0)
                {
                    // Add to range
                    results.Add(new CapacityQueryItem(person, item.StartDate, item.EndDate, unassignedPercentage));
                }
            }

            // Check against the desired availabilty and sort into match, partial match %, partial match duration, partial match % and time
            fullMatch = results.Where(x => x.AvailabilityPercent == requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate).ToList();
            partialMatchPercent = results.Where(x => x.AvailabilityPercent == requiredFTE && (x.EndDate != queryEndDate || x.StartDate != queryStartDate)).ToList();
            partialMatchDuration = results.Where(x => x.AvailabilityPercent != requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate).ToList();
            partialMatchBoth = results.Where(x => x.AvailabilityPercent != requiredFTE && x.EndDate != queryEndDate && x.StartDate != queryStartDate).ToList();

            // Assign results
            queryResults = results.OrderByDescending(x => x.AvailabilityPercent);

            // Update the UI
            StateHasChanged();
        }

        /// <summary>
        /// Gets all the sub tasks within the query date window
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        private IEnumerable<SubTask> GetSubTasksWithinQueryWindow(IEnumerable<SubTask> source)
        {
            // Filter all the tasks based on the window of the query
            var tasks = source.Where(x =>
            {
                return
                // Tasks that start in the window
                (x.StartDate >= QueryStartDate && x.StartDate < queryEndDate) ||

                // Tasks that end in the window
                (x.EndDate > QueryStartDate && x.EndDate < queryEndDate) ||

                // Tasks that span over the window
                (x.StartDate < QueryStartDate && x.EndDate >= queryEndDate);
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

        /// <summary>
        /// Pulls project info from the DB and packages the data into a plottable format
        /// Can specific a start and end date to restrict the data window
        /// </summary>
        private async Task ConfigureSourceAsync()
        {
            Debug.WriteLine("** Configuring Chart Source...");

            // Reset source
            chartSource = new List<ChartItem>();

            if (chart != null) await RefreshChartAsync();

            // Get people from the database
            var peo = PersonService.GetAll(context).OrderBy(x => x.Name);

            // Remove people who have left if necessary
            if (!IncludeLeavers)
            {
                peo = peo.Where(p => p.EndDate == null || p.EndDate >= DateTime.Today).OrderBy(x => x.Name);
            }

            if (peo.Count() > 0)
            {
                // Get projects from the database ignoring finished or cancelled projects
                var projects = ProjectService.GetAll(context).Where(x => !x.ProjectStatus.IsProjectFinishedOrCancelled());
                if (!IncludeUnFunded)
                {
                    projects = projects.Where(p => p.ProjectStatus != ProjectStatus.Unfunded);
                }

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
                        groupedSubTasks.Add(p.Name, assignments);
                    }

                    // Build chart source from the grouped data
                    foreach (var group in groupedSubTasks)
                    {
                        var items = ChartHelper.AggregateByWeekIntoBlocks(group.Value,
                            x =>
                            {
                                var person = x.AssignedResources.First(x => x.Person.Name == group.Key);
                                return Math.Round(person.Percentage / .84);
                            },
                            (x, y) =>
                            {
                                return ChartItem.GetColourStringFTE(x, y * 100 / 84);
                            },
                            group.Key,
                            queryActive ? QueryStartDate : null,
                            queryActive ? queryEndDate : null,
                            x =>
                            {
                                return x.AssignedResources.First(x => x.Person.Name == group.Key).IsProvisional;
                            },
                            (x, w) =>
                            {
                                var person = peo.FirstOrDefault(y => y.Name == group.Key);
                                return person?.GetAvailabilityOnDate(w) ?? 0.84;
                            }
                        ).ToList();

                        // If this person has no assignments then create a dummy chart item based on their start date
                        // to ensure they show up in the capacity sheet
                        if (items.Count() < 1)
                        {
                            var person = peo.First(x => x.Name == group.Key);
                            items.Add(new ChartItem(null, group.Key, person.StartDate, person.StartDate, 0, 0, false));
                        }

                        // Add the range
                        chartSource.AddRange(items);
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
                        if (assignments.Count > 0) groupedSubTasks.Add(project.GetFullName(), assignments);
                    }

                    // Build chart source from the grouped data
                    foreach (var group in groupedSubTasks)
                    {
                        chartSource.AddRange(ChartHelper.AggregateByWeekIntoBlocks(group.Value,
                            x =>
                            {
                                var person = x.AssignedResources.First(x => x.Person.Name == ChosenPerson);
                                return Math.Round(person.Percentage / .84);
                            },
                            (x, y) =>
                            {
                                return ChartItem.GetColourStringFTE(x, y * 100 / 84);
                            },
                            group.Key,
                            queryActive ? QueryStartDate : null,
                            queryActive ? queryEndDate : null,
                            x =>
                            {
                                return x.AssignedResources.First(x => x.Person.Name == ChosenPerson).IsProvisional;
                            },
                            (x, w) =>
                            {
                                var person = peo.FirstOrDefault(y => y.Name == ChosenPerson);
                                return person?.GetAvailabilityOnDate(w) ?? 0.84;
                            }
                        ));
                    }
                }

                chartTitle = $"Load for {ChosenPerson ?? "All"}";
                Debug.WriteLine($"** Finished configuring {chartTitle}. Include unfunded = {includeUnFunded}! Include leavers = {includeLeavers}!");

                // Format X Axis range
                options.Xaxis.Min = !queryActive ? DateTime.Now.Date.AddDays(-14).ToUnixTimeMilliseconds() : QueryStartDate.ToUnixTimeMilliseconds();
                options.Xaxis.Max = !queryActive ? null : queryEndDate.ToUnixTimeMilliseconds();

                // First time this is called, there is no reference to the chart
                if (chart != null)
                {
                    Debug.WriteLine($"** Re-renderering chart! {options.Xaxis.Min} to {options.Xaxis.Max}");
                    await RefreshChartAsync();
                }

                Debug.WriteLine($"** ChartSource has {chartSource?.Count()} entries!");
            }
        }

        /// <summary>
        /// Method to force the chart to update
        /// </summary>
        /// <returns></returns>
        private async Task RefreshChartAsync()
        {
            // Update the options
            await chart.UpdateOptionsAsync(true, false, false);

            // HACK: Not sure why we have to call this twice but we do!
            await chart.UpdateSeriesAsync(false);
            await chart.UpdateSeriesAsync(false);

            // Force blazor redraw
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Method to export the capacity information in a format suitable for ITS GaDMO reporting
        /// </summary>
        private async void ExportCapacityData()
        {
            // Get all the people
            var people = PersonService.GetAll(context).OrderBy(x => x.Name);

            // Create blank list of data
            var allData = new List<ExportHelper.TaskData>();

            // Set the report length
            const int numMonths = 6;

            // Get data for each person
            var helper = new ExportHelper();
            foreach (var p in people)
            {
                // Assume 6 months for now
                var data = helper.GetExportDataForPerson(
                    p,
                    SubTaskService.GetAll(context).Where(x => x.AssignedResources.Any(x => x.Person == p)),
                    ProjectService.GetAll(context),
                    numMonths
                );
                allData.AddRange(data);
            }

            try
            {

                // Write to CSV file
                var filename = $"Capacity_{DateTime.Now.Ticks}.csv";
                var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX");
                Directory.CreateDirectory(folder);
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapX", filename);
                using (var writer = new StreamWriter(path))
                {

                    // Get all public properties
                    var props = typeof(ExportHelper.TaskData).GetProperties();
                    var propNames = props.Select(x => x.Name);

                    // Create header row
                    var headers = propNames.ToList();
                    var startDate = DateTime.Now.Date;
                    var d = startDate;
                    while (d < startDate.AddMonths(numMonths))
                    {
                        // Convert month number to name for the column heading
                        headers.Add($"{d.ToString("MMM", CultureInfo.InvariantCulture)} %");

                        // Increment month
                        d = d.AddMonths(1);
                    }

                    // Write header row
                    writer.WriteLine(string.Join(",", headers));

                    // Write rows one at a time
                    foreach (var record in allData)
                    {
                        // Write properties
                        var valuesAsStrings = new List<string>();
                        foreach (var name in propNames)
                        {
                            string value = record.GetType().GetProperty(name).GetValue(record)?.ToString() ?? string.Empty;
                            valuesAsStrings.Add(value.Replace(",", ";"));
                        }

                        // Write expanded values for months
                        d = startDate;
                        while (d < startDate.AddMonths(numMonths))
                        {
                            // Add the monthly value
                            valuesAsStrings.Add(record.GetMonthlyValue(d.Month)?.ToString() ?? string.Empty);

                            // Increment month
                            d = d.AddMonths(1);
                        }

                        // Write the row
                        writer.WriteLine(string.Join(",", valuesAsStrings));
                    }
                }
                Debug.WriteLine($"** Exported {allData.Count} rows to {path}");


                // Get file stream
                using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                // Invoke JS on the client to download the file
                await JS.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Could not download file: {ex}");
            }
        }
    }
}
