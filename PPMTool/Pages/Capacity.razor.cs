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
using static PPMTool.Data.ExportHelper;

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
        private IJSRuntime JSRuntime { get; set; }

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
                    ConfigureChartSource();
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
                    ConfigureChartSource();

                }
            }
        }

        private DateTime queryStartDate = DateTime.Today;
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

        private IEnumerable<Project> projects;
        private IDictionary<object, IEnumerable<Assignment>> groupedAssignments;
        private IList<List<ChartItem>> confirmedChartItems;
        private IList<List<ChartItem>> provisionalChartItems;
        private IList<string> chartTitles;
        private IList<ApexChartOptions<ChartItem>> chartOptions;
        private List<Person> people;
        private List<Person> managers;
        private List<Person> filteredPeople;
        private List<Person> filteredManagers;
        private DateTime queryEndDate = DateTime.Today.AddDays(7);
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
            Loading = true;

            // Get all projects not finished or cancelled
            projects = ProjectService.GetAll(context).Where(x => !x.ProjectStatus.IsFinishedOrCancelled());

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
                    ConfigureChartSource();
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


            // Filter out leavers if necessary
            if (!includeLeavers)
            {
                people = people
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Today)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            LoadFilteredPeople(new LoadDataArgs());

            // Add managers
            managers = people.Where(x => roles.Any(y => y.Person == x)).ToList();
            LoadFilteredManagers(new LoadDataArgs());
        }

        /// <summary>
        /// Use the master list of people to filter the data source for the dropdown based on user typing
        /// </summary>
        /// <param name="args"></param>
        void LoadFilteredPeople(LoadDataArgs args)
        {
            var temp = people.AsQueryable();
            if (!string.IsNullOrEmpty(args.Filter))
            {
                temp = temp.Where(p => p.Name.ToLower().Contains(args.Filter.ToLower()));
            }
            filteredPeople = temp.ToList();
            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Use the master list of managers to filter the data source for the dropdown based on user typing
        /// </summary>
        /// <param name="args"></param>
        void LoadFilteredManagers(LoadDataArgs args)
        {
            var temp = managers.AsQueryable();
            if (!string.IsNullOrEmpty(args.Filter))
            {
                temp = temp.Where(p => p.Name.ToLower().Contains(args.Filter.ToLower()));
            }
            filteredManagers = temp.ToList();
            InvokeAsync(StateHasChanged);
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
        private void PeopleSelectionChanged(object selectedOptions)
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
            ConfigureChartSource();

            LogInformation($"Selected people: {(items == null ? "" : string.Join("|", items))}");
        }

        /// <summary>
        /// Manager selected from the dropdown
        /// </summary>
        /// <param name="selectedOptions"></param>
        private void ManagerSelectionChanged(object selectedOptions)
        {
            var item = selectedOptions as Person;
            Debug.WriteLine($"** Selected Manager: {item?.Name}");

            // Save the new state
            SaveManagerState();

            // Regenerate the chart data
            ConfigureChartSource();

            LogInformation($"Selected manager: {item?.Name}");
        }

        /// <summary>
        /// Resets the page to its initial state
        /// </summary>
        private void ClearQuery(bool regenerateChart = true)
        {
            Debug.WriteLine("** Clearing Query...");
            queryResultsAvailable = false;
            queryErrorMessage = null;
            queryActive = false;
            ChosenPeople = new List<string>();
            if (regenerateChart) ConfigureChartSource();

            LogInformation($"Query cleared");
        }

        /// <summary>
        /// Runs the capacity query and updates the query result property
        /// </summary>
        private void RunQuery()
        {
            Debug.WriteLine("** Running query...");

            // Add error
            if (QueryStartDate >= queryEndDate)
            {
                queryErrorMessage = "End date must be after the start date!";
                return;
            }

            // Reset query results but don't regenerate the chart as we are going to do it again in a minute
            ClearQuery(false);

            // Start query state
            queryActive = true;
            LogInformation($"Query running.");

            // Update the chart source as this is used to drive the query results
            ConfigureChartSource(true);
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
        /// </summary>
        private void ConfigureChartSource(bool presentQueryResults = false)
        {
            Debug.WriteLine("** Configuring Chart Source...");
            Loading = true;
            StateHasChanged();

            Task.Run(() =>
            {
                Debug.WriteLine("** Running task...");

                // Initialise the dictionaries
                confirmedChartItems = new List<List<ChartItem>>();
                provisionalChartItems = new List<List<ChartItem>>();
                groupedAssignments = new Dictionary<object, IEnumerable<Assignment>>();
                chartTitles = new List<string>();
                chartOptions = new List<ApexChartOptions<ChartItem>>();
                IEnumerable<Project> validProjects = projects;

                // Need some people for this to work
                if (people.Count() == 0)
                {
                    LogError("People database is empty!");
                    Debug.WriteLine("** No people registered in the database!");
                    Loading = false;
                    return;
                }

                // Filter projects ignoring finished or cancelled projects
                if (!IncludeUnFunded)
                {
                    validProjects = validProjects.Where(p => !p.ProjectStatus.IsUnfunded());
                }

                // Filter the project source if a manager selected
                if (ChosenManager != null)
                {
                    validProjects = validProjects.Where(x => x.ProjectManager == ChosenManager);
                }

                // Get the window from the start and end dates of the projects included in the source
                // Avoids including malformed projects without a proper start date
                validProjects = validProjects.Where(x => x.StartDate.Year > 2000);
                if (validProjects.Count() == 0)
                {
                    Debug.WriteLine("** No projects found that match the chosen options!");
                    Loading = false;
                    return;
                }
                var startDate = validProjects.Min(x => x.StartDate);
                var endDate = validProjects.Max(x => x.EndDate);

                // Determine state based on drop down selections
                UpdateSelectionState();

                // -------------- PERSON MODE -------------- //

                // Flatten subtasks and group by person if "All" chosen
                if (!managerChosen && !peopleChosen)
                {
                    Debug.WriteLine("** Chart in PERSON MODE.");

                    // Create temporary list of chart items
                    var chartSourceTemp = new List<ChartItem>();

                    // Build chart items for each person
                    foreach (var person in people)
                    {
                        // Create a list of subtasks to which this person is assigned
                        var assignments = new List<Assignment>();
                        foreach (var project in validProjects)
                        {
                            foreach (var subTask in project.SubTasks)
                            {
                                if (subTask.AssignedResources.Any(z => z.Person == person))
                                {
                                    assignments.Add(new Assignment(subTask, project.ProjectStatus));
                                }
                            }
                        }

                        // Add dictionary entry with person as key
                        groupedAssignments.Add(person, assignments);
                    }

                    // Build chart source from the grouped data
                    foreach (var group in groupedAssignments)
                    {
                        // Add the range for this person
                        chartSourceTemp.AddRange(
                            GetPersonModeChartItemsFromAssignments(group.Key as Person, group.Value, queryActive ? QueryStartDate : startDate, queryActive ? queryEndDate : endDate)
                        );
                    }

                    // Add data
                    confirmedChartItems.Add(chartSourceTemp.Where(x => !x.IsHatched).ToList());
                    provisionalChartItems.Add(chartSourceTemp.Where(x => x.IsHatched).ToList());

                    // Chart title
                    var chartTitle = $"Load for {(!managerChosen ? "All" : "None")} {(managerChosen ? " with manager " + ChosenManager.Name : "")}";
                    chartTitles.Add(chartTitle);

                    // Chart options
                    chartOptions.Add(BuildNewChartOptionsObject());
                }

                // -------------- PROJECT MODE -------------- //

                // Filter by people chosen, flatten and group by project if in project mode
                else if (peopleChosen)
                {
                    Debug.WriteLine("** Chart in PROJECT MODE.");

                    // For each person selected
                    foreach (var name in ChosenPeople)
                    {
                        // Create temporary list of chart items
                        var chartSourceTemp = new List<ChartItem>();

                        // Get person object
                        var person = people.First(x => x.Name == name);

                        // Reset the grouped subtasks list for the next person
                        groupedAssignments.Clear();

                        // Create a list of subtasks for each project this person is assigned to
                        foreach (var project in validProjects)
                        {
                            var assignments = new List<Assignment>();
                            foreach (var subTask in project.SubTasks)
                            {
                                // Only include subtasks with this person assigned as a resource
                                if (subTask.AssignedResources.Any(x => name == x.Person.Name))
                                {
                                    assignments.Add(new Assignment(subTask, project.ProjectStatus));
                                }
                            }

                            // Add dictionary entry with project name as key
                            if (assignments.Count > 0) groupedAssignments.Add(project, assignments);
                        }

                        // Build chart source from the grouped data
                        Debug.WriteLine($"** {person.Name} has {groupedAssignments.Count} projects");
                        foreach (var group in groupedAssignments)
                        {
                            // Compute chart items from the grouped assignments
                            var seriesName = (group.Key as Project).GetFullName();
                            chartSourceTemp.AddRange(
                                GetProjectModeChartItemsFromAssignments(seriesName, group, startDate, endDate, person)
                            );
                        }

                        // Total row needs to repeat the above logic but on the flattened set of subtasks
                        var allProjectAssignments = groupedAssignments.SelectMany(x => x.Value);
                        var rowName = "Total";
                        chartSourceTemp.AddRange(
                            GetProjectModeChartItemsFromAssignments(
                                rowName,
                                new KeyValuePair<object, IEnumerable<Assignment>>(rowName, allProjectAssignments),
                                startDate,
                                endDate,
                                person
                            )
                        );

                        // Horrible hack required to get the Y-axis sorting to work correctly with multiple series
                        // by adding zero width entries to ensure both series have the same number of Y categories
                        var confirmedChartItemsComplete = new List<ChartItem>();
                        var provisionalChartItemsComplete = new List<ChartItem>();

                        foreach (var c in chartSourceTemp)
                        {
                            if (!c.IsHatched)
                            {
                                confirmedChartItemsComplete.Add(c);
                            }
                            else
                            {
                                confirmedChartItemsComplete.Add(new ChartItem(c.Colour, c.Label, DateTime.Today, DateTime.Today, 0, 0, c.IsHatched));
                            }
                        }

                        foreach (var c in chartSourceTemp)
                        {
                            if (c.IsHatched)
                            {
                                provisionalChartItemsComplete.Add(c);
                            }
                            else
                            {
                                provisionalChartItemsComplete.Add(new ChartItem(c.Colour, c.Label, DateTime.Today, DateTime.Today, 0, 0, c.IsHatched));
                            }
                        }

                        // Add completed chart source to dictionary
                        confirmedChartItems.Add(confirmedChartItemsComplete);
                        provisionalChartItems.Add(provisionalChartItemsComplete);

                        // Title
                        var chartTitle = $"Load for {name} {(managerChosen ? " with manager " + ChosenManager.Name : "")}";
                        chartTitles.Add(chartTitle);

                        // Options
                        chartOptions.Add(BuildNewChartOptionsObject());
                    }
                }

                Debug.WriteLine($"** Done. Unfunded = {IncludeUnFunded} | Leavers = {IncludeLeavers}.");

                // Format X Axis range based on last end date of real assignments (i.e. not padding assignments)
                var allItems = confirmedChartItems.Concat(provisionalChartItems).SelectMany(x => x).Where(x => x.Value1 != 0);
                long? endDateForChartNoQuery = allItems.Count() > 0 ? allItems.Max(x => x.EndDate).ToUnixTimeMilliseconds() : null;
                foreach (var opt in chartOptions)
                {
                    opt.Xaxis.Min = !queryActive ? DateTime.Today.AddDays(-14).ToUnixTimeMilliseconds() : QueryStartDate.ToUnixTimeMilliseconds();
                    opt.Xaxis.Max = !queryActive ? endDateForChartNoQuery : queryEndDate.ToUnixTimeMilliseconds();
                }
                Debug.WriteLine($"** Reconfguring the chart on XAxis range {chartOptions.FirstOrDefault()?.Xaxis?.Min} to {chartOptions.FirstOrDefault()?.Xaxis?.Max}");

            }).ContinueWith(task =>
            {
                Debug.WriteLine($"** ...task complete. Status = {task.Status}");

                if (presentQueryResults)
                {
                    // Convert the chart results to capacity query results
                    var results = new List<CapacityQueryItem>();
                    var mergedItems = confirmedChartItems.Concat(provisionalChartItems).SelectMany(x => x).ToList();
                    foreach (var item in mergedItems)
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

                    LogInformation("Query results generated.");
                }

                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });

                Debug.WriteLine($"** There are {chartTitles.Count} chart(s)!");
            });
        }

        private void OnChartZoomed(ZoomedData<ChartItem> zoomedData)
        {
            if (zoomedData != null)
            {
                Debug.WriteLine($"** {zoomedData.Chart.ChartId} Zoomed {zoomedData.XAxis.Min} to {zoomedData.XAxis.Max}");

                // Go through all the chart options objects and for all not associated with the chart making this call
                // and whose values of the X limits differ from those give can then be updated.
                foreach (var opt in chartOptions)
                {
                    if (opt != zoomedData.Chart.Options)
                    {
                        if (opt.Xaxis.Min as decimal? != zoomedData.XAxis.Min || opt.Xaxis.Max as decimal? != zoomedData.XAxis.Max)
                        {
                            Debug.WriteLine($"** Updating zoom for {opt.Chart.Id}: {zoomedData.XAxis.Min} to {zoomedData.XAxis.Max}");
                            JSRuntime.InvokeVoidAsync("apexChartsUpdateAxis", opt.Chart.Id, zoomedData.XAxis.Min, zoomedData.XAxis.Max);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a standard chart object to be pass to all chart instances -- they cannot share the same object
        /// </summary>
        /// <returns></returns>
        private ApexChartOptions<ChartItem> BuildNewChartOptionsObject()
        {
            return new ApexChartOptions<ChartItem>
            {
                PlotOptions = new PlotOptions
                {
                    Bar = new PlotOptionsBar
                    {
                        Horizontal = true,
                        RangeBarOverlap = true,
                        RangeBarGroupRows = true
                    }
                },
                Legend = new Legend
                {
                    Show = false
                },
                Xaxis = new XAxis { },
                Fill = new Fill
                {
                    Opacity = 1,
                    Type = new FillTypeSelections(new FillType[] { FillType.Solid, FillType.Pattern }),
                    Pattern = new FillPattern
                    {
                        Style = new FillPatternStyleSelections(new FillPatternStyle[] { FillPatternStyle.SlantedLines }),
                    }
                }
            };
        }

        /// <summary>
        /// Method only called in project mode to generate chart items
        /// </summary>
        /// <param name="seriesName"></param>
        /// <param name="groupedAssignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="chosenPerson"></param>
        /// <returns></returns>
        private IEnumerable<ChartItem> GetProjectModeChartItemsFromAssignments(
            string seriesName,
            KeyValuePair<object, IEnumerable<Assignment>> groupedAssignments,
            DateTime startDate,
            DateTime endDate,
            Person chosenPerson = null
        )
        {
            return ChartHelper.ConvertAssignmentsToChartItems(
                groupedAssignments.Value,
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
                (x, y) =>
                {
                    // Shading function based on value 1 and value 2
                    return ChartItem.GetColourStringFTE(x, y);
                },
                seriesName,
                queryActive ? QueryStartDate : startDate,
                queryActive ? queryEndDate : endDate,
                x =>
                {
                    // Get the set of resources to check the condition against
                    var resources = chosenPerson == null ?
                        x.SubTask.AssignedResources.Where(x => ChosenPeople.Contains(x.Person.Name)) :
                        x.SubTask.AssignedResources.Where(x => x.Person == chosenPerson);

                    // If any resources are marked as provisional or the project owning the task
                    // is not funded, active or in maintenance
                    return
                        x.ProjectStatus.IsUnconfirmed() ||
                        resources.Any(x => x.IsProvisional);
                },
                // Value 2 for each block is based on the sum of the availability of all chosen people
                (x, w) =>
                {
                    var peo = chosenPerson == null ?
                        people.Where(y => ChosenPeople.Contains(y.Name)) :
                        people.Where(y => y == chosenPerson);
                    return peo.RoundedSum(y => y.GetAvailabilityOnDate(w));
                },
                // Accepts list of assignments for the block to determine tooltip messages for the block
                assignmentsWithinBlock =>
                {
                    var messages = string.Empty;

                    // When not a total row, the group key will be a project.
                    var projectForRow = groupedAssignments.Key as Project;
                    if (projectForRow != null)
                    {
                        // Always return the project manager on the tooltip for project rows
                        messages += $"PM: {projectForRow.ProjectManager?.Name ?? "Not Set"}";

                        // Check whether this project has unmet demand on the tasks to which this person is assigned
                        var assignedWithinBlockWithChosenPerson = assignmentsWithinBlock.Where(x => x.SubTask.AssignedResources.Any(x => x.Person == chosenPerson));
                        if (assignedWithinBlockWithChosenPerson.Any(x => x.SubTask.HasUnmetDemand()))
                        {
                            var unmetDemand = assignedWithinBlockWithChosenPerson.RoundedSum(x => x.SubTask.UnmetDemand);
                            messages += $"<h3 class=\"me-1 text-danger\"> &#x26A0; [UNMET DEMAND ({unmetDemand} FTE)]</h3>";
                        }
                    }

                    // Generate further, universal messages
                    messages = GenerateTooltipMessages(assignmentsWithinBlock, chosenPerson, messages);

                    return messages;
                }
            );
        }

        /// <summary>
        /// Generates tooltip messages for the chart items (blocks) based on a series of conditions.
        /// </summary>
        /// <param name="assignmentsWithinBlock">List of assignments that have contributed to the block</param>
        /// <param name="personOfInterest">Person used to decide whether condition relevancy</param>
        /// <param name="messages">Messages to add to</param>
        /// <returns></returns>
        private string GenerateTooltipMessages(IEnumerable<Assignment> assignmentsWithinBlock, Person personOfInterest, string messages)
        {
            // Add the project unconfirmed warning to the tooltip if project is unconfirmed
            if (assignmentsWithinBlock.Any(x => x.ProjectStatus.IsUnconfirmed()))
            {
                messages += "<h3 class=\"me-1 text-warning\"> &#x26A0; [PROJECT UNCONFIRMED]</h3>";
            }

            // Add the provisional resource warning to the tooltip if chosen person is provisional on the project
            if (assignmentsWithinBlock.Any(x => x.SubTask.AssignedResources.Any(x => x.Person == personOfInterest && x.IsProvisional)))
            {
                messages += "<h3 class=\"me-1 text-warning\"> &#x26A0; [PROVISIONAL ASSIGNMENT]</h3>";
            }

            return messages;
        }

        /// <summary>
        /// Only called in person mode per person to generate chart items
        /// </summary>
        /// <param name="person"></param>
        /// <param name="assignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private IEnumerable<ChartItem> GetPersonModeChartItemsFromAssignments(
            Person person,
            IEnumerable<Assignment> assignments,
            DateTime startDate,
            DateTime endDate)
        {
            return ChartHelper.ConvertAssignmentsToChartItemsForPerson(
                person,
                assignments,
                x =>
                {
                    var resource = x.AssignedResources.First(x => x.Person.Name == person.Name);
                    return resource.AssignmentFTE;
                },
                (x, y) =>
                {
                    return ChartItem.GetColourStringFTE(x, y);
                },
                person.Name,
                queryActive ? QueryStartDate : startDate,
                queryActive ? queryEndDate : endDate,
                x =>
                {
                    // If any resources are marked as provisional or the project owning the task
                    // is not funded, active or in maintenance
                    return
                        x.ProjectStatus.IsUnconfirmed() ||
                        x.SubTask.AssignedResources.First(x => x.Person == person).IsProvisional;
                },
                (x, w) =>
                {
                    return person.GetAvailabilityOnDate(w);
                },
                tooltipMessageFormatter: assignmentsInBlock => GenerateTooltipMessages(assignmentsInBlock, person, string.Empty)
            );
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
            var allData = new List<TaskData>();

            // Set the report length
            const int numMonths = 6;

            // Get data for each person
            foreach (var p in people)
            {
                // Assume 6 months for now
                var data = ExportHelper.GetExportDataForPerson(
                    p,
                    SubTaskService.GetAll(context).Where(x => x.AssignedResources.Any(x => x.Person == p)),
                    ProjectService.GetAll(context),
                    numMonths
                );
                allData.AddRange(data);
            }

            // Remove duplicates of unmet demand entries
            var tempList = new List<TaskData>();
            foreach (var data in allData)
            {
                // If not unmet demand entry then copy over
                if (data.EmployeeName != "Unmet Demand")
                {
                    tempList.Add(data);
                    continue;
                }
                else
                {
                    // If unmet demand entry but already in list then skip
                    if (tempList.Any(x => x.ProjectAndTaskName == data.ProjectAndTaskName && x.EmployeeName == "Unmet Demand"))
                    {
                        continue;
                    }
                    // Must be a new unmet demand entry
                    else
                    {
                        tempList.Add(data);
                    }
                }
            }
            allData = tempList;
            allData.Sort((x, y) => x.EmployeeName.CompareTo(y.EmployeeName));

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
                    var props = typeof(TaskData).GetProperties();
                    var propNames = props.Select(x => x.Name);

                    // Create header row
                    var headers = propNames.ToList();
                    var startDate = DateTime.Today;
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
                await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
            }
            catch (Exception ex)
            {
                LogError($"Could not download file: {ex}");
            }
        }
    }
}
