using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexCharts;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader")]
    public partial class Capacity : BasePage
    {
        /// <summary>
        /// Represents a model for a particular chart
        /// </summary>
        public class ChartModel
        {
            public IList<ChartItem> ConfirmedChartItems { get; set; }
            public IList<ChartItem> ProvisionalChartItems { get; set; }
            public string ChartTitle { get; set; }
            public ApexChartOptions<ChartItem> ChartOptions { get; set; }
        }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "filterid")]
        public int? FilterPersonId { get; set; }


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

        private bool includeFinished = false;
        public bool IncludeFinished
        {
            get => includeFinished;
            set
            {
                if (includeFinished != value)
                {
                    includeFinished = value;
                    SessionStorage.SetItemAsync<bool?>("capacity-include-finished", includeFinished);

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

        private bool IsReader => AuthenticationState?.User.IsInRole(RoleType.Reader.ToString()) ?? false;

        private bool ReadOrEditAuthorised => EditAuthorised || IsReader;

        private IEnumerable<Project> cachedProjects;
        private IEnumerable<Person> cachedPeople;
        private IDictionary<object, IEnumerable<Assignment>> groupedAssignments;
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
        private CancellationTokenSource configureChartTaskCancellationTokenSource = null;
        private Task configureChartTask = null;
        private IList<ChartModel> chartModels = new List<ChartModel>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;

            // Get all projects not finished or cancelled
            cachedProjects = ProjectService.GetAll(Context).Where(x => !x.ProjectStatus.IsCancelled());

            // Cache all the people
            cachedPeople = PersonService.GetAll(Context).OrderBy(x => x.Name);

            // Refresh the dropdown
            ReloadDropDownSources();

            LogInformation($"Viewing capacity page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Load settings
            var managerName = await SessionStorage.GetItemAsync<string>("capacity-chosen-manager");
            ChosenManager = managers.FirstOrDefault(x => x.Name == managerName);
            ChosenPeople = await SessionStorage.GetItemAsync<IEnumerable<string>>("capacity-chosen-people");

            // If there is a query parameter then use it
            if (FilterPersonId != null)
            {
                var matchingPerson = cachedPeople.FirstOrDefault(x => x.PersonId == FilterPersonId);
                if (matchingPerson != null)
                {
                    ChosenPeople = new List<string>
                    {
                        matchingPerson.Name
                    };
                }
            }

            // Force the selection logic to run
            UpdateSelectionState();

            // Reload the dropdown sources if a manager has been chosen
            if (ChosenManager != null)
            {
                ReloadDropDownSources();
            }

            // Check that the boolean flags are not null (i.e. that they exist in session storage) before overwriting defaults
            var temp = await SessionStorage.GetItemAsync<bool?>("capacity-include-leavers");
            if (temp != null) IncludeLeavers = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>("capacity-include-unfunded");
            if (temp != null) IncludeUnFunded = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>("capacity-include-finished");
            if (temp != null) IncludeFinished = temp ?? false;

            if (EditAuthorised || IsReader)
            {
                ConfigureChartSource();
                return;
            }

            // Choose the person automatically if not a manager    
            // Look up the username
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            ChosenPeople = new List<string>
            {
                role.GetName()
            };
            PeopleSelectionChanged(ChosenPeople);
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
            Debug.WriteLine("** Reloading dropdown sources...");

            // Get people and filter if PM selected
            people = cachedPeople.ToList();
            if (chosenManager != null)
            {
                var validProjects = GetValidProjects();

                // Get flattened list of all resources for the valid projects
                people = validProjects.SelectMany(x => x.SubTasks.SelectMany(x => x.AssignedResources.Select(x => x.Person)))
                    .DistinctBy(x => x.PersonId)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            // Add managers
            var roles = RolesService.GetAll(Context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            managers = cachedPeople.Where(x => roles.Any(y => y.Person.PersonId == x.PersonId)).ToList();

            // Filter out leavers if necessary
            if (!includeLeavers)
            {
                people = people
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Today)
                    .OrderBy(x => x.Name)
                    .ToList();

                managers = managers
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Today)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            // Apply autocomplete box filters
            LoadFilteredPeople(new LoadDataArgs());
            LoadFilteredManagers(new LoadDataArgs());

            // Remove any people not in the dropdown source from the selected people list
            if (chosenPeople != null)
            {
                var temp = new List<string>();
                foreach (var p in chosenPeople)
                {
                    if (filteredPeople.Any(x => x.Name == p))
                    {
                        temp.Add(p);
                    }
                }
                chosenPeople = temp;
            }
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
                var project = ProjectService.GetAll(Context).FirstOrDefault(x => x.GetFullName() == projectName);
                if (project != null)
                {
                    Navigation.NavigateTo($"projects/projectdetails/{project.ProjectId}");
                }
            }

            // When in people ("All") mode then add person to selection and update the chart
            else if (dataPoint.IsSelected && !peopleChosen)
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

            // Reload the people to include just those working on projects that PM manages
            ReloadDropDownSources();

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

        /// <summary>
        /// Method to order the capacity query results
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
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

            // Before kicking off a new task here we need to cancel the previous one
            if (configureChartTaskCancellationTokenSource != null)
            {
                Debug.WriteLine("** Cancelling existing task...");
                configureChartTaskCancellationTokenSource.Cancel();
            }

            // Wait for the task to be finished
            while (!configureChartTask?.IsCompleted ?? false)
            {
                Debug.WriteLine("** Waiting for completion...");
                Task.Delay(1000);
            }

            // Create new cancellation token and task
            configureChartTaskCancellationTokenSource = new CancellationTokenSource();
            configureChartTask = Task.Run(new Func<Task>(() =>
            {
                Debug.WriteLine("** Running new configure task...");

                // Initialise the dictionaries
                chartModels.Clear();
                groupedAssignments = new Dictionary<object, IEnumerable<Assignment>>();
                IEnumerable<Project> validProjects = cachedProjects;

                // Need some people for this to work
                if (people.Count() == 0)
                {
                    LogError("People database is empty!");
                    Debug.WriteLine("** No people registered in the database!");
                    Loading = false;
                    return Task.CompletedTask;
                }

                // Update the valid projects
                validProjects = GetValidProjects();

                // Get the window from the start and end dates of the projects included in the source
                // Avoids including malformed projects without a proper start date
                validProjects = validProjects.Where(x => x.StartDate.Year > 2000);
                if (validProjects.Count() == 0)
                {
                    Debug.WriteLine("** No projects found that match the chosen options!");
                    Loading = false;
                    return Task.CompletedTask;
                }
                var startDate = validProjects.Min(x => x.StartDate);
                var endDate = validProjects.Max(x => x.EndDate);

                // Determine state based on drop down selections
                UpdateSelectionState();

                // -------------- PERSON MODE -------------- //

                // Flatten subtasks and group by person if "All" chosen
                if (!peopleChosen)
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
                            GetPersonModeChartItemsFromAssignments(
                                group.Key as Person,
                                group.Value,
                                queryActive ? QueryStartDate : startDate,
                                queryActive ? queryEndDate : endDate
                            )
                        );
                    }

                    // Add data
                    chartModels.Add(new ChartModel
                    {
                        ChartTitle = $"Load for All {(managerChosen ? " with manager " + ChosenManager.Name : "")}",
                        ChartOptions = BuildNewChartOptionsObject(),
                        ConfirmedChartItems = chartSourceTemp.Where(x => !x.IsHatched).ToList(),
                        ProvisionalChartItems = chartSourceTemp.Where(x => x.IsHatched).ToList()
                    });
                }

                // -------------- PROJECT MODE -------------- //

                // Filter by people chosen, flatten and group by project if in project mode
                else if (managerChosen || peopleChosen)
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
                                GetProjectModeChartItemsFromAssignments(
                                    seriesName,
                                    group,
                                    startDate,
                                    endDate,
                                    person
                                )
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
                                person,
                                isTotalRow: true
                            )
                        );

                        // Hack to complete the entries
                        ChartHelper.CompleteChartSeries(
                            chartSourceTemp,
                            c => new ChartItem(c.Colour, c.Label, DateTime.Today, DateTime.Today, 0, 0, c.IsHatched, isFake: true),
                            out var confirmedChartItemsComplete,
                            out var provisionalChartItemsComplete
                        );

                        // Add data
                        chartModels.Add(new ChartModel
                        {
                            ChartTitle = $"Load for {name} {(managerChosen ? " with manager " + ChosenManager.Name : "")}",
                            ChartOptions = BuildNewChartOptionsObject(),
                            ConfirmedChartItems = confirmedChartItemsComplete,
                            ProvisionalChartItems = provisionalChartItemsComplete
                        });
                    }
                }

                Debug.WriteLine($"** Done. Unfunded = {IncludeUnFunded} | Leavers = {IncludeLeavers} | Finished = {IncludeFinished}.");

                // Format X Axis range based on last end date of real assignments (i.e. not padding assignments)
                var allItems = chartModels.SelectMany(x => x.ConfirmedChartItems.Concat(x.ProvisionalChartItems)).Where(x => x.Value1 != 0);
                long? endDateForChartNoQuery = allItems.Count() > 0 ? allItems.Max(x => x.EndDate).ToUnixTimeMilliseconds() : null;
                foreach (var opt in chartModels.Select(x => x.ChartOptions))
                {
                    opt.Xaxis.Min = !queryActive ? DateTime.Today.AddDays(-14).ToUnixTimeMilliseconds() : QueryStartDate.ToUnixTimeMilliseconds();
                    opt.Xaxis.Max = !queryActive ? endDateForChartNoQuery : queryEndDate.ToUnixTimeMilliseconds();
                }
                Debug.WriteLine($"** Reconfguring the chart on XAxis range {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Min} to {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Max}");

                return Task.CompletedTask;

            }), configureChartTaskCancellationTokenSource.Token)
            .ContinueWith(task =>
            {
                Debug.WriteLine($"** ...task complete. Status = {task.Status}");

                if (presentQueryResults)
                {
                    // Convert the chart results to capacity query results
                    var results = new List<CapacityQueryItem>();
                    var mergedItems = chartModels.SelectMany(x => x.ConfirmedChartItems.Concat(x.ProvisionalChartItems));
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
                    // Reset the state
                    Debug.WriteLine($"** Continue with task complete!");
                    Loading = false;
                    configureChartTask = null;
                    StateHasChanged();
                });

                Debug.WriteLine($"** There are {chartModels.Count} chart(s)!");
            });
        }

        /// <summary>
        /// Takes the cached projects on the page and filters them based on the state of the switches and dropdowns
        /// </summary>
        /// <returns></returns>
        private IEnumerable<Project> GetValidProjects(bool filterBasedOnSelectedManager = true)
        {
            var validProjects = cachedProjects;

            // Filter projects based on finished
            if (!IncludeFinished)
            {
                Debug.WriteLine("** Removing finished projects...");
                validProjects = validProjects.Where(p => p.ProjectStatus != ProjectStatus.Finished);
            }

            // Filter projects based on unfunded
            if (!IncludeUnFunded)
            {
                Debug.WriteLine("** Removing unfunded projects...");
                validProjects = validProjects.Where(p => !p.ProjectStatus.IsUnfunded());
            }

            // Filter the project source if a manager selected
            if (ChosenManager != null && filterBasedOnSelectedManager)
            {
                Debug.WriteLine("** Removing projects not belonging to selected manager...");
                validProjects = validProjects.Where(x => x.ProjectManager.PersonId == ChosenManager.PersonId);
            }

            return validProjects;
        }

        /// <summary>
        /// Callback when an item is zoomed
        /// </summary>
        /// <param name="zoomedData"></param>
        private void OnChartZoomed(ZoomedData<ChartItem> zoomedData)
        {
            if (zoomedData != null)
            {
                Debug.WriteLine($"** {zoomedData.Chart.ChartId} Zoomed {zoomedData.XAxis.Min} to {zoomedData.XAxis.Max}");
                UpdateZoomAcrossCharts(zoomedData.Chart.Options, zoomedData.XAxis.Min, zoomedData.XAxis.Max);
            }
        }

        /// <summary>
        /// Updates all the chart models that do not have the matching options object with the min and max provided
        /// </summary>
        /// <param name="options"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        private void UpdateZoomAcrossCharts(ApexChartOptions<ChartItem> options, object min, object max)
        {
            // Go through all the chart options objects and for all not associated with the chart making this call
            // and whose values of the X limits differ from those give can then be updated.
            foreach (var opt in chartModels.Select(x => x.ChartOptions))
            {
                if (opt != options)
                {
                    Debug.WriteLine($"** Updating zoom for {opt.Chart.Id}: {min} to {max}");
                    JSRuntime.InvokeVoidAsync("apexChartsUpdateAxis", opt.Chart.Id, min, max);
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
                },
                Chart = new Chart
                {
                    Zoom = new Zoom
                    {
                        AllowMouseWheelZoom = false
                    }
                },
                Annotations = new Annotations
                {
                    Xaxis = new List<AnnotationsXAxis>
                    {
                        new AnnotationsXAxis()
                        {
                            X = DateTime.Today.ToUnixTimeMilliseconds(),
                            BorderWidth = 2,
                            StrokeDashArray = 5,
                            BorderColor = "#888",
                            Label = new Label
                            {
                                Text = "Today",
                                Position = LabelPosition.Left
                            }
                        }
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
        /// <param name="person"></param>
        /// <param name="isTotalRow"></param>
        /// <returns></returns>
        private IEnumerable<ChartItem> GetProjectModeChartItemsFromAssignments(
            string seriesName,
            KeyValuePair<object, IEnumerable<Assignment>> groupedAssignments,
            DateTime startDate,
            DateTime endDate,
            Person person,
            bool isTotalRow = false
        )
        {
            return ChartHelper.ConvertAssignmentsToChartItems(
                groupedAssignments.Value,
                // Value 1 for each block
                assignments =>
                {
                    return assignments.RoundedSum(assignment =>
                    {
                        // Value is the effort of the chosen person
                        var resource = assignment.SubTask.AssignedResources.First(x => x.Person.Name == person.Name);
                        return resource.AssignmentFTE;
                    });
                },
                // Colour function
                (value1, value2) =>
                {
                    // Shading function based on value 1 and value 2
                    return ChartItem.GetColourStringFTE(value1, isTotalRow ? value2 : 1, !isTotalRow);
                },
                seriesName,
                queryActive ? QueryStartDate : startDate,
                queryActive ? queryEndDate : endDate,
                // Hatched function
                assignments =>
                {
                    return assignments.Any(assignment =>
                    {
                        // Get the set of resources to check the condition against
                        var resource = assignment.SubTask.AssignedResources.First(x => x.Person == person);

                        // If resource is marked as provisional or the project owning the task
                        // is not funded, active or in maintenance
                        return assignment.ProjectStatus.IsUnconfirmed() || resource.IsProvisional;
                    });
                },
                // Value 2 for each block
                (assignments, value1, currentDay) =>
                {
                    var peo = people.Where(y => y == person);

                    // The total availability of the person becomes value 2
                    return peo.RoundedSum(y => y.GetAvailabilityOnDate(currentDay));
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
                        var assignedWithinBlockWithChosenPerson = assignmentsWithinBlock.Where(x => x.SubTask.AssignedResources.Any(x => x.Person == person));
                        if (assignedWithinBlockWithChosenPerson.Any(x => x.SubTask.HasUnmetDemand()))
                        {
                            var unmetDemand = assignedWithinBlockWithChosenPerson.RoundedSum(x => x.SubTask.UnmetDemand);
                            messages += $"<h3 class=\"me-1 text-danger\"> &#x26A0; [UNMET DEMAND ({unmetDemand} FTE)]</h3>";
                        }
                    }

                    // Generate further, universal messages
                    messages = GenerateTooltipMessages(assignmentsWithinBlock, person, messages);

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
                assignments =>
                {
                    return assignments.RoundedSum(assignment =>
                    {
                        var resource = assignment.SubTask.AssignedResources.First(x => x.Person.Name == person.Name);
                        return resource.AssignmentFTE;
                    });
                },
                (value1, value2) =>
                {
                    return ChartItem.GetColourStringFTE(value1, value2);
                },
                person.Name,
                queryActive ? QueryStartDate : startDate,
                queryActive ? queryEndDate : endDate,
                assignments =>
                {
                    return assignments.Any(assignment =>
                    {
                        // If any resources are marked as provisional or the project owning the task
                        // is not funded, active or in maintenance
                        return
                            assignment.ProjectStatus.IsUnconfirmed() ||
                            assignment.SubTask.AssignedResources.First(x => x.Person == person).IsProvisional;
                    });
                },
                (assignments, value1, currentDay) =>
                {
                    return person.GetAvailabilityOnDate(currentDay);
                },
                tooltipMessageFormatter: assignmentsInBlock => GenerateTooltipMessages(assignmentsInBlock, person, string.Empty)
            );
        }

        /// <summary>
        /// Quickly select those people in the available list that the active user manages
        /// </summary>
        private void FilterToMyStaff()
        {
            ChosenPeople = people.Where(x => x.LineManager?.PersonId == ActiveUser?.PersonId).Select(x => x.Name);
            PeopleSelectionChanged(ChosenPeople);
        }

        /// <summary>
        /// Does the active user manage staff that are in the available list
        /// </summary>
        /// <returns></returns>
        private bool HasStaffInList()
        {
            return people.Any(x => x.LineManager?.PersonId == ActiveUser?.PersonId);
        }

        /// <summary>
        /// Method to automatically zoom the charts to the number of months in the future
        /// </summary>
        /// <param name="numberOfMonths"></param>
        private void SetZoomToMonthsAhead(int numberOfMonths)
        {
            var zoomTo = DateTime.Today.AddMonths(numberOfMonths).ToUnixTimeMilliseconds();
            var opt = chartModels.FirstOrDefault()?.ChartOptions;
            if (opt != null)
            {
                Debug.WriteLine($"** Updating zoom for {opt.Chart.Id}: {opt.Xaxis.Min} to {zoomTo}");
                JSRuntime.InvokeVoidAsync("apexChartsUpdateAxis", opt.Chart.Id, opt.Xaxis.Min, zoomTo);
                UpdateZoomAcrossCharts(opt, opt.Xaxis.Min, zoomTo);
            }
        }
    }
}
