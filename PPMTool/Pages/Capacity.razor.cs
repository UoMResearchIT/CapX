using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Context;
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
        private RolesService RoleService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private IJSRuntime JS { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        private bool includeUnFunded = true;
        public bool IncludeUnFunded
        {
            get => includeUnFunded;
            set
            {
                if (includeUnFunded != value)
                {
                    includeUnFunded = value;
                    SessionStorage.SetItemAsync<bool?>("capacity-include-unfunded", includeUnFunded);

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
                    SessionStorage.SetItemAsync<bool?>("capacity-include-leavers", includeLeavers);

                    // Refresh the people source
                    ReloadDropDownSources();

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

        private Person chosenManager;
        public Person ChosenManager
        {
            get => chosenManager;
            set
            {
                if (chosenManager != value)
                {
                    chosenManager = value;
                    SaveManagerState();
                }
            }
        }

        private IEnumerable<string> chosenPeople = new List<string>();
        public IEnumerable<string> ChosenPeople
        {
            get => chosenPeople;
            set
            {
                if (chosenPeople != value)
                {
                    chosenPeople = value;
                    SavePeopleState();
                }
            }
        }

        private IDictionary<object, IEnumerable<SubTask>> groupedSubTasks;
        private ApexChart<ChartItem> chart;
        private List<ChartItem> chartSource;
        private ApexChartOptions<ChartItem> options;
        private List<Person> people;
        private List<Person> managers;
        private string chartTitle;
        private DateTime queryEndDate = DateTime.Now.Date.AddDays(7);
        private bool queryResultsAvailable;
        private string queryErrorMessage;
        private bool queryActive;
        private double requiredFTE = 0.5;
        private List<CapacityQueryItem> fullMatch;
        private List<CapacityQueryItem> partialMatchPercent;
        private List<CapacityQueryItem> partialMatchDuration;
        private List<CapacityQueryItem> partialMatchBoth;
        private bool managerChosen;
        private bool peopleChosen;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            loading = true;

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

            // Refresh the dropdown
            ReloadDropDownSources();

            LogInformation($"Viewing capacity page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                // Load settings
                var managerName = await SessionStorage.GetItemAsync<string>("capacity-chosen-manager");
                ChosenManager = managers.FirstOrDefault(x => x.Name == managerName);
                ChosenPeople = await SessionStorage.GetItemAsync<IEnumerable<string>>("capacity-chosen-people");
                UpdateSelectionState();

                // Check that the boolean flags are not null (i.e. that they exist in session storage) before overwriting defaults
                var temp = await SessionStorage.GetItemAsync<bool?>("capacity-include-leavers");
                if (temp != null) IncludeLeavers = temp ?? false;
                temp = await SessionStorage.GetItemAsync<bool?>("capacity-include-unfunded");
                if (temp != null) IncludeUnFunded = temp ?? false;

                // Choose the person automatically if not a manager
                if (!EditAuthorised)
                {
                    // Look up the username
                    var role = RoleService.GetByUsername(context, AuthenticationState.User.Identity.Name.Trim().ToLower());
                    ChosenPeople = new List<string>
                    {
                        role.Person.Name
                    };
                    PeopleSelectionChanged(ChosenPeople);
                }
                else
                {
                    // Get data for chart
                    await ConfigureSourceAsync();
                }
            }
        }

        private void SaveManagerState()
        {
            SessionStorage.SetItemAsync("capacity-chosen-manager", chosenManager == null ? null : chosenManager.Name);
        }

        private void SavePeopleState()
        {
            SessionStorage.SetItemAsync("capacity-chosen-people", chosenPeople);
        }

        private void UpdateSelectionState()
        {
            managerChosen = ChosenManager != null;
            peopleChosen = ChosenPeople != null && ChosenPeople.Count() > 0;
        }

        /// <summary>
        /// Method to setup the dropdown sources
        /// </summary>
        private void ReloadDropDownSources()
        {
            // Get dropdown options
            people = PersonService.GetAll(context).OrderBy(x => x.Name).ToList();
            var roles = RolesService.GetAll(context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            managers = people.Where(x => roles.Any(y => y.Person == x)).ToList();

            // Filter out leavers if necessary
            if (!includeLeavers)
            {
                people = people
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Now.Date)
                    .OrderBy(x => x.Name)
                    .ToList();
            }
        }

        /// <summary>
        /// Method to handle when a series element on the chart is selected
        /// </summary>
        /// <param name="dataPoint"></param>
        private void DataPointsSelected(SelectedData<ChartItem> dataPoint)
        {
            // Decide on state
            UpdateSelectionState();

            // When in project mode, navigate
            if (dataPoint.IsSelected && peopleChosen)
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

            // When in people ("All") mode then add person to selection and update the chart
            else if (dataPoint.IsSelected && !peopleChosen && !managerChosen)
            {
                var personName = dataPoint.DataPoint.Items.FirstOrDefault()?.Label;
                Debug.WriteLine($"** Selected {personName}. Updating selection...");
                var match = people.FirstOrDefault(x => x.Name == personName);
                if (match != null)
                {
                    var temp = peopleChosen ? new List<string>(ChosenPeople) : new List<string>();
                    temp.Add(personName);
                    ChosenPeople = temp;
                    PeopleSelectionChanged(ChosenPeople);
                }
            }
        }

        /// <summary>
        /// Fire and forget when selection of the multi-drop down changes
        /// </summary>
        /// <param name="selectedOptions"></param>
        private async void PeopleSelectionChanged(object selectedOptions)
        {
            var items = selectedOptions as IEnumerable<string>;
            Debug.WriteLine("** Selected People:");
            if (items != null)
            {
                foreach (var i in items)
                {
                    Debug.WriteLine($"** {i}");
                }
            }

            // Save the new state
            SavePeopleState();

            // Regenerate the chart data
            await ConfigureSourceAsync();

            LogInformation($"Selected people: {(items == null ? "" : string.Join("|", items))}");
        }

        /// <summary>
        /// Manager selected from the dropdown
        /// </summary>
        /// <param name="selectedOptions"></param>
        private async void ManagerSelectionChanged(object selectedOptions)
        {
            var item = selectedOptions as Person;
            Debug.WriteLine($"** Selected Manager: {item?.Name}");

            // Save the new state
            SaveManagerState();

            // Regenerate the chart data
            await ConfigureSourceAsync();

            LogInformation($"Selected manager: {item?.Name}");
        }

        /// <summary>
        /// Resets the page to its initial state
        /// </summary>
        private async Task ClearQueryAsync()
        {
            Debug.WriteLine("** Clearing Query...");
            queryResultsAvailable = false;
            queryErrorMessage = null;
            queryActive = false;
            ChosenPeople = new List<string>();
            await ConfigureSourceAsync();

            LogInformation($"Query cleared");
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

            LogInformation($"Query running.");

            // Update the chart source as this is used
            await ConfigureSourceAsync();

            // Convert the chart results to capcity query results
            foreach (var item in chartSource)
            {
                // Get person from item label
                var person = people.FirstOrDefault(p => p.Name == item.Label);
                if (person == null)
                {
                    Debug.WriteLine($"** Couldn't find person {item.Label}");
                    continue;
                }

                // Availability of individual is value 2 in the chart item
                var availabilityFTE = item.Value2;

                // Invert value (value 1 here is the assignment value) -- truncate to 2 DP
                var unassignedFTE = Math.Round(100 * (availabilityFTE - item.Value1)) / 100;

                // Only add if the block (item) has a non-zero length and the person isn't already over-allocated which would give a negative inverse
                if (item.StartDate != item.EndDate && unassignedFTE > 0)
                {
                    // Add to range
                    results.Add(new CapacityQueryItem(person, item.StartDate, item.EndDate, unassignedFTE));
                }
            }

            // Check against the desired availabilty and sort into match, partial match FTE, partial match duration, partial match FTE and time
            fullMatch = OrganiseResults(results
                .Where(x => x.AvailabilityPercent == requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate));
            partialMatchPercent = OrganiseResults(results
                .Where(x => x.AvailabilityPercent == requiredFTE && (x.EndDate != queryEndDate || x.StartDate != queryStartDate)));
            partialMatchDuration = OrganiseResults(results
                .Where(x => x.AvailabilityPercent != requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate));
            partialMatchBoth = OrganiseResults(results
                .Where(x => x.AvailabilityPercent != requiredFTE && (x.EndDate != queryEndDate || x.StartDate != queryStartDate)));

            // Results available
            queryResultsAvailable = results.Count() > 0;

            // Update the UI
            StateHasChanged();
        }

        private List<CapacityQueryItem> OrganiseResults(IEnumerable<CapacityQueryItem> results)
        {
            return results
                .OrderBy(x => x.Person.Name)
                .ThenByDescending(x => x.AvailabilityPercent)
                .ToList();
        }

        /// <summary>
        /// Pulls project info from the DB and packages the data into a plottable format
        /// Can specific a start and end date to restrict the data window
        /// </summary>
        private async Task ConfigureSourceAsync()
        {
            Debug.WriteLine("** Configuring Chart Source...");
            loading = true;

            // Create a temp source
            var chartSourceTemp = new List<ChartItem>();

            // Need some people for this to work
            if (people.Count() == 0)
            {
                LogError("People database is empty!");
                Debug.WriteLine("** No people registered in the database!");
                loading = false;
                chartSource = new List<ChartItem>();
                return;
            }

            // Get projects from the database ignoring finished or cancelled projects
            var projects = ProjectService.GetAll(context).Where(x => !x.ProjectStatus.IsProjectFinishedOrCancelled());
            if (!IncludeUnFunded)
            {
                projects = projects.Where(p => p.ProjectStatus != ProjectStatus.Unfunded);
            }

            // Filter the project source if a manager selected
            if (ChosenManager != null)
            {
                projects = projects.Where(x => x.ProjectManager == ChosenManager);
            }

            // Get the window from the start and end dates of the projects included in the source
            var safeProjects = projects.Where(x => x.StartDate.Year > 2000);
            if (safeProjects.Count() == 0)
            {
                Debug.WriteLine("** No projects found that match the chosen options!");
                loading = false;
                chartSource = new List<ChartItem>();
                return;
            }
            var startDate = safeProjects.Min(x => x.StartDate);
            var endDate = safeProjects.Max(x => x.EndDate);

            // Reinitialise dictionary
            groupedSubTasks = new Dictionary<object, IEnumerable<SubTask>>();

            // Determine state
            UpdateSelectionState();

            // -------------- PERSON MODE -------------- //

            // Flatten subtasks and group by person if "All" chosen
            if (!managerChosen && !peopleChosen)
            {
                Debug.WriteLine("** Chart in PERSON MODE.");
                foreach (var person in people)
                {
                    // Create a list of subtasks to which this person is assigned
                    var assignments = new List<SubTask>();
                    foreach (var project in projects)
                    {
                        foreach (var subTask in project.SubTasks)
                        {
                            if (subTask.AssignedResources.Any(z => z.Person == person))
                            {
                                assignments.Add(subTask);
                            }
                        }
                    }

                    // Add dictionary entry with person as key
                    groupedSubTasks.Add(person, assignments);
                }

                // Build chart source from the grouped data
                foreach (var group in groupedSubTasks)
                {
                    var items = ChartHelper.ConvertSubTasksToChartItemsForPerson(
                        group.Key as Person,
                        group.Value,
                        x =>
                        {
                            var resource = x.AssignedResources.First(x => x.Person.Name == (group.Key as Person).Name);
                            return resource.AssignmentFTE;
                        },
                        (x, y) =>
                        {
                            return ChartItem.GetColourStringFTE(x, y);
                        },
                        (group.Key as Person).Name,
                        queryActive ? QueryStartDate : startDate,
                        queryActive ? queryEndDate : endDate,
                        x =>
                        {
                            return x.AssignedResources.First(x => x.Person == group.Key).IsProvisional;
                        },
                        (x, w) =>
                        {
                            return (group.Key as Person)?.GetAvailabilityOnDate(w) ?? 1.0;
                        }
                    ).ToList();

                    // Add the range for this person
                    chartSourceTemp.AddRange(items);
                }
            }

            // -------------- PROJECT MODE -------------- //

            // Filter by people chosen, flatten and group by project if in project mode
            else if (peopleChosen)
            {
                Debug.WriteLine("** Chart in PROJECT MODE.");

                // For each person selected
                List<SubTask> subTasksAllPeople = new List<SubTask>();
                foreach (var name in ChosenPeople)
                {
                    // Get person object
                    var person = people.First(x => x.Name == name);

                    // Reset the grouped subtasks list for the next person
                    groupedSubTasks.Clear();

                    // Create a list of subtasks for each project this person is assigned to
                    foreach (var project in projects)
                    {
                        var assignments = new List<SubTask>();
                        foreach (var subTask in project.SubTasks)
                        {
                            // Only include subtasks with this person assigned aa resource
                            if (subTask.AssignedResources.Any(z => name == z.Person.Name))
                            {
                                assignments.Add(subTask);
                            }
                        }

                        // Add dictionary entry with project name as key
                        if (assignments.Count > 0) groupedSubTasks.Add(project, assignments);
                    }

                    // Build chart source from the grouped data
                    Debug.WriteLine($"** {person.Name} has {groupedSubTasks.Count} projects");
                    foreach (var group in groupedSubTasks)
                    {
                        // Give unique name to series when multiple people selected
                        var seriesName = (group.Key as Project).GetFullName();
                        chartSourceTemp.AddRange(
                            GetProjectModeChartItemsFromTasks(ChosenPeople.Count() > 1 ? $"{seriesName} ({person.ShortName})" : seriesName, group, startDate, endDate, person)
                        );
                    }

                    // Total row needs to repeat the above logic but on the flattened set of subtasks
                    var allProjectSubTasks = groupedSubTasks.SelectMany(x => x.Value);
                    var rowName = $"Total ({name})";
                    chartSourceTemp.AddRange(
                        GetProjectModeChartItemsFromTasks(
                            rowName,
                            new KeyValuePair<object, IEnumerable<SubTask>>(rowName, allProjectSubTasks),
                            startDate,
                            endDate,
                            person
                        )
                    );

                    // Add the subtasks to the aggregated list for later (if more than one person)
                    if (ChosenPeople.Count() > 1) subTasksAllPeople.AddRange(allProjectSubTasks);
                }

                if (ChosenPeople.Count() > 1)
                {
                    // Final total row is the same logic applied to the subtasks aggregated across everyone selected
                    var totalName = $"Total (All)";
                    chartSourceTemp.AddRange(
                        GetProjectModeChartItemsFromTasks(
                            totalName,
                            new KeyValuePair<object, IEnumerable<SubTask>>(totalName, subTasksAllPeople),
                            startDate,
                            endDate
                        )
                    );
                }
            }

            // Assign new source
            loading = false;
            chartSource = chartSourceTemp;

            chartTitle = $"Load for {(peopleChosen ? string.Join(",", ChosenPeople) : (!managerChosen ? "All" : "None"))} " +
                $"{(managerChosen ? " with manager " + ChosenManager.Name : "")}";
            Debug.WriteLine($"** ...Finished configuring {chartTitle}. Include unfunded = {includeUnFunded}! Include leavers = {includeLeavers}!");

            // Format X Axis range
            options.Xaxis.Min = !queryActive ? DateTime.Now.Date.AddDays(-14).ToUnixTimeMilliseconds() : QueryStartDate.ToUnixTimeMilliseconds();
            options.Xaxis.Max = !queryActive ? null : queryEndDate.ToUnixTimeMilliseconds();

            // First time this is called, there is no reference to the chart
            if (chart != null)
            {
                Debug.WriteLine($"** Re-renderering chart with options! {options.Xaxis.Min} to {options.Xaxis.Max}");
                await RefreshChartAsync();
            }
            else
            {
                await InvokeAsync(StateHasChanged);
            }

            Debug.WriteLine($"** ChartSource has {chartSource?.Count()} entries!");
        }

        private IEnumerable<ChartItem> GetProjectModeChartItemsFromTasks(
            string seriesName,
            KeyValuePair<object, IEnumerable<SubTask>> group,
            DateTime startDate,
            DateTime endDate,
            Person chosenPerson = null
        )
        {
            return ChartHelper.ConvertSubTasksToChartItems(
                group.Value,
                // Value 1 for each block
                x =>
                {
                    // If no person specified then it is the sum of the effort across all chosen people
                    // If a person specified then the value is just their effort
                    var resources = chosenPerson == null ?
                        x.AssignedResources.Where(x => ChosenPeople.Contains(x.Person.Name)) :
                        x.AssignedResources.Where(x => x.Person == chosenPerson);
                    return resources.RoundedSum(x => x.AssignmentFTE);

                },
                // Shading function based on value 1 and value 2
                (x, y) =>
                {
                    return ChartItem.GetColourStringFTE(x, y);
                },
                seriesName,
                queryActive ? QueryStartDate : startDate,
                queryActive ? queryEndDate : endDate,
                // Hatched value is whether any assignee is provisional
                x =>
                {
                    var resources = chosenPerson == null ?
                        x.AssignedResources.Where(x => ChosenPeople.Contains(x.Person.Name)) :
                        x.AssignedResources.Where(x => x.Person == chosenPerson);
                    return resources.Any(x => x.IsProvisional);
                },
                // Value 2 for each block is based on the sum of the availability of all chosen people
                (x, w) =>
                {
                    var peo = chosenPerson == null ?
                        people.Where(y => ChosenPeople.Contains(y.Name)) :
                        people.Where(y => y == chosenPerson);
                    return peo.RoundedSum(y => y.GetAvailabilityOnDate(w));
                });
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
            //await chart.UpdateSeriesAsync(false);

            // Force blazor redraw
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Method to export the capacity information in a format suitable for ITS GaDMO reporting
        /// </summary>
        private async void ExportCapacityData()
        {
            LogInformation($"Exporting capacity data");

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
                            valuesAsStrings.Add(record.GetMonthlyValue(d.Month, d.Year)?.ToString() ?? string.Empty);

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
                LogError($"Could not download file: {ex}");
            }
        }
    }
}
