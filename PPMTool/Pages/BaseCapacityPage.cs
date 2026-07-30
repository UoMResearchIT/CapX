// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using ApexCharts;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Helpers;
using PPMTool.Models;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public abstract class BaseCapacityPage : BasePage
    {
        /// <summary>
        /// Represents a model for a particular gantt chart
        /// </summary>
        public class ChartModel
        {
            public IList<ChartItem> ConfirmedChartItems { get; set; }
            public IList<ChartItem> ProvisionalChartItems { get; set; }
            public string ChartTitle { get; set; }
            public ApexChartOptions<ChartItem> ChartOptions { get; set; }
        }

        [Inject]
        protected PersonService PersonService { get; set; }

        [Inject]
        protected ProjectService ProjectService { get; set; }

        [Inject]
        protected SubTaskService SubTaskService { get; set; }

        [Inject]
        protected IJSRuntime JSRuntime { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "filterid")]
        public int? FilterPersonId { get; set; }

        protected bool IsDeveloper() => ActiveUserRoleType == RoleType.Developer;

        /// <summary>
        /// Whether the user can change the dropdown selections and click the generate button to update the chart.
        /// </summary>
        /// <returns></returns>
        protected bool CanCustomise() => EditAuthorised || ActiveUserRoleType == RoleType.Reader;

        protected CancellationTokenSource configureChartTaskCancellationTokenSource = null;
        protected Task configureChartTask = null;
        protected IList<ChartModel> chartModels = new List<ChartModel>();
        protected IEnumerable<Project> cachedProjects;
        protected IEnumerable<Person> cachedPeople;
        protected IDictionary<object, IEnumerable<Assignment>> groupedAssignments;
        protected List<Person> people;
        protected List<Person> filteredPeople;
        protected IEnumerable<string> chosenPeople = new List<string>();
        protected bool includeUnFunded = true;
        protected bool includeLeavers = false;
        protected bool includeFinished = false;

        /// <summary>
        /// Change callback for unfunded switch
        /// </summary>
        protected async Task UnFundedSwitchChangedAsync(bool value)
        {
            includeUnFunded = value;
            await SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-unfunded", value);
        }

        /// <summary>
        /// Change callback for leavers switch
        /// </summary>
        protected async Task LeaversSwitchChangedAsync(bool value)
        {
            includeLeavers = value;
            await SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-leavers", value);
            await ReloadDropDownSourcesAsync();
        }

        /// <summary>
        /// Change callback for include finished switch
        /// </summary>
        protected async Task FinishedSwitchChangedAsync(bool value)
        {
            includeFinished = value;
            await SessionStorage.SetItemAsync<bool?>($"{GetSessionStorageTag()}-include-finished", value);
        }

        /// <summary>
        /// Save the chosen people to session storage
        /// </summary>
        protected async Task SavePeopleStateAsync() => await SessionStorage.SetItemAsync($"{GetSessionStorageTag()}-chosen-people", chosenPeople);

        /// <summary>
        /// Fire and forget when selection of the multi-select people down changes
        /// </summary>
        /// <param name="selectedOptions"></param>
        protected async Task PeopleSelectionChangedAsync(object selectedOptions)
        {
            var items = selectedOptions as IEnumerable<string>;
            Debug.WriteLine($"** Selected People: {(items != null ? string.Join('|', items) : "")}");

            // Save the new state
            await SavePeopleStateAsync();
        }

        /// <summary>
        /// Generate chart items for a given person in person mode
        /// </summary>
        /// <param name="person"></param>
        /// <param name="assignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        protected abstract IEnumerable<ChartItem> GetPersonModeChartItemsFromAssignments(
            Person person,
            IEnumerable<Assignment> assignments,
            DateTime startDate,
            DateTime endDate
        );

        /// <summary>
        /// Generate chart items for a given person in project mode
        /// </summary>
        /// <param name="seriesName"></param>
        /// <param name="groupedAssignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="person"></param>
        /// <param name="isTotalRow"></param>
        /// <returns></returns>
        protected abstract IEnumerable<ChartItem> GetProjectModeChartItemsFromAssignments(
            string seriesName,
            KeyValuePair<object, IEnumerable<Assignment>> groupedAssignments,
            DateTime startDate,
            DateTime endDate,
            Person person,
            bool isTotalRow = false
        );

        /// <summary>
        /// Method to handle when a series element on the chart is selected
        /// </summary>
        /// <param name="dataPoint"></param>
        protected virtual async Task DataPointsSelectedAsync(SelectedData<ChartItem> dataPoint)
        {
            // When in project mode, navigate
            if (dataPoint.IsSelected && PeopleChosen())
            {
                var projectName = dataPoint.DataPoint.Items.FirstOrDefault()?.Label;
                Debug.WriteLine($"** Selected {projectName}. Navigating to details page...");

                // Use the title of the task to find its projectID then navigate to the details page
                var project = ProjectService.GetAll(Context).FirstOrDefault(x => ProjectService.GetFullName(x) == projectName);
                if (project != null)
                {
                    Navigation.NavigateTo($"projects/projectdetails/{project.ProjectId}");
                }
            }

            // When in people ("All") mode then add person to selection and update the chart
            else if (dataPoint.IsSelected && !PeopleChosen())
            {
                var personName = dataPoint.DataPoint.Items.FirstOrDefault()?.Label;
                Debug.WriteLine($"** Selected {personName}. Updating selection...");
                var match = people.FirstOrDefault(x => x.Name == personName);
                if (match != null)
                {
                    var temp = PeopleChosen() ? new List<string>(chosenPeople) : new List<string>();
                    temp.Add(personName);
                    chosenPeople = temp;
                    await PeopleSelectionChangedAsync(chosenPeople);
                }
            }
            await ConfigureChartSourceAsync();
        }

        /// <summary>
        /// Generates tooltip messages for the chart items (blocks) based on a series of conditions.
        /// </summary>
        /// <param name="assignmentsWithinBlock">List of assignments that have contributed to the block</param>
        /// <param name="personOfInterest">Person used to decide whether condition relevancy</param>
        /// <param name="messages">Messages to add to</param>
        /// <returns></returns>
        protected virtual string GenerateTooltipMessages(IEnumerable<Assignment> assignmentsWithinBlock, Person personOfInterest, string messages)
        {
            // Add the project unconfirmed warning to the tooltip if project is unconfirmed
            if (assignmentsWithinBlock.Any(x => x.ProjectStatus.IsUnconfirmed()))
            {
                messages += "<h3 class=\"me-1 text-warning\"> &#x26A0; [PROJECT UNCONFIRMED]</h3>";
            }

            // Add the provisional resource warning to the tooltip if chosen person is provisional on the project
            if (assignmentsWithinBlock.Any(x => x?.SubTask.AssignedResources.Any(x => x.Person.PersonId == personOfInterest.PersonId && x.IsProvisional) ?? false))
            {
                messages += "<h3 class=\"me-1 text-warning\"> &#x26A0; [PROVISIONAL ASSIGNMENT]</h3>";
            }

            return messages;
        }

        /// <summary>
        /// Method to construct a dictionary of assignments grouped by person or project which can be mapped into blocks on the chart
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="people"></param>
        /// <param name="isPersonMode"></param>
        /// <param name="dutySet">What subset of the tasks should be used to populate the dictionary based on the duty associated with the demand</param>
        protected virtual void PopulateGroupedAssignmentsForPeople(
            IEnumerable<Project> projects,
            IEnumerable<Person> people,
            bool isPersonMode,
            Duty[] dutySet = null)
        {
            // Reset existing dictionary
            groupedAssignments = new Dictionary<object, IEnumerable<Assignment>>();

            // Return early if no data
            if (people.Count() == 0 || projects.Count() == 0)
            {
                return;
            }

            // Create the dictionary differently depending on the mode of the chart
            if (isPersonMode)
            {
                foreach (var person in people)
                {
                    // Create a list of subtasks to which this person is assigned
                    var assignments = new List<Assignment>();
                    foreach (var project in projects)
                    {
                        var subTasks = GetFilteredSubTasks(project, person, dutySet);

                        // Build assignments
                        foreach (var subTask in subTasks)
                        {
                            assignments.Add(new Assignment(subTask, project.ProjectStatus));
                        }
                    }

                    // Add dictionary entry with person as key
                    groupedAssignments.Add(person, assignments);
                }
            }
            else
            {
                // Get person
                var person = people.First();

                // Create a list of subtasks for each project this person is assigned to
                foreach (var project in projects)
                {
                    var assignments = new List<Assignment>();
                    var subTasks = GetFilteredSubTasks(project, person, dutySet);

                    foreach (var subTask in subTasks)
                    {
                        assignments.Add(new Assignment(subTask, project.ProjectStatus));
                    }

                    // Add dictionary entry with project name as key
                    if (assignments.Count > 0) groupedAssignments.Add(project, assignments);
                }
            }
        }

        /// <summary>
        /// Local helper to return a list of subtasks based on appropriate filtering
        /// </summary>
        /// <param name="project"></param>
        /// <param name="person"></param>
        /// <param name="dutySet"></param>
        /// <returns></returns>
        private IEnumerable<SubTask> GetFilteredSubTasks(Project project, Person person, Duty[] dutySet)
        {
            // Filter list
            var subTasks = project.SubTasks
                .Where(x => x.AssignedResources
                    .Any(z => z.Person.PersonId == person.PersonId)
                );

            // Filter again if there is a subset indicated
            if (dutySet is not null && dutySet.Any())
            {
                subTasks = subTasks.Where(x => dutySet.Contains(x.TaskDuty));
            }
            return subTasks;
        }

        /// <summary>
        /// Method to configure the sources for the capacity chart objects
        /// </summary>
        /// <param name="afterConfigureTask">Runs after the configuration task has completed</param>
        /// <param name="manualStartDate">Overrides the start window for things like axis limits</param>
        /// <param name="manualEndDate">Overrides the end window for things like axis limits</param>
        /// <param name="customChartTitleGenerator">Generates the title for the charts - takes the name of the person if in project mode</param>
        /// <param name="projectModeCondition">Optional OR condition for deciding whether in project mode</param>
        protected async Task ConfigureChartSourceAsync(
            Action afterConfigureTask = null,
            DateTime? manualStartDate = null,
            DateTime? manualEndDate = null,
            Func<string, string> customChartTitleGenerator = null,
            Func<bool> projectModeCondition = null)
        {
            Debug.WriteLine("** Configuring Chart Source...");
            Loading = true;
            StateHasChanged();
            await Task.Yield();

            // Run the data loading
            await Task.Run(() =>
            {
                try
                {
                    Debug.WriteLine("** Running new configure task...");

                    // Initialise the dictionaries
                    chartModels.Clear();
                    IEnumerable<Project> validProjects = cachedProjects;

                    // Need some people for this to work
                    if (people.Count() == 0)
                    {
                        LogError("People database is empty!");
                        Debug.WriteLine("** No people registered in the database!");
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
                        return Task.CompletedTask;
                    }
                    var startDate = validProjects.Min(x => x.StartDate);
                    var endDate = validProjects.Max(x => x.EndDate);

                    // -------------- PERSON MODE -------------- //

                    // Flatten subtasks and group by person if "All" chosen
                    if (!PeopleChosen())
                    {
                        Debug.WriteLine("** Chart in PERSON MODE.");

                        // Create temporary list of chart items
                        var chartSourceTemp = new List<ChartItem>();

                        // Build assignments dictionary for each person
                        PopulateGroupedAssignmentsForPeople(validProjects, people, true);

                        // Build chart source from the grouped data
                        foreach (var group in groupedAssignments)
                        {
                            // Add the range for this person
                            chartSourceTemp.AddRange(
                                GetPersonModeChartItemsFromAssignments(
                                    group.Key as Person,
                                    group.Value,
                                    manualStartDate ?? startDate,
                                    manualEndDate ?? endDate
                                )
                            );
                        }

                        // Add data
                        var numberRows = chartSourceTemp.DistinctBy(x => x.Label).Count();
                        chartModels.Add(new ChartModel
                        {
                            ChartTitle = customChartTitleGenerator?.Invoke(null) ?? "Load for All",
                            ChartOptions = BuildNewChartOptionsObject(numberRows),
                            ConfirmedChartItems = chartSourceTemp.Where(x => !x.IsHatched()).ToList(),
                            ProvisionalChartItems = chartSourceTemp.Where(x => x.IsHatched()).ToList()
                        });
                    }

                    // -------------- PROJECT MODE -------------- //

                    // Filter by people chosen, flatten and group by project if in project mode
                    else if (PeopleChosen() || (projectModeCondition?.Invoke() ?? false))
                    {
                        Debug.WriteLine("** Chart in PROJECT MODE.");

                        // For each person selected
                        foreach (var name in chosenPeople)
                        {
                            // Create temporary list of chart items
                            var chartSourceTemp = new List<ChartItem>();

                            // Get person object
                            var person = people.First(x => x.Name == name);

                            // Build assignments dictionary for each project
                            PopulateGroupedAssignmentsForPeople(validProjects, new List<Person> { person }, false);

                            // Build chart source from the grouped data
                            Debug.WriteLine($"** {person.Name} has {groupedAssignments.Count} projects");
                            foreach (var group in groupedAssignments)
                            {
                                // Compute chart items from the grouped assignments
                                var seriesName = ProjectService.GetFullName(group.Key as Project);
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
                            groupedAssignments = new Dictionary<object, IEnumerable<Assignment>>();
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
                                c => new ChartItem(c.Colour, c.Label, DateTime.Today, DateTime.Today, 0, 0, c.IsHatched(), isFake: true),
                                out var confirmedChartItemsComplete,
                                out var provisionalChartItemsComplete
                            );

                            // Add data
                            var numberRows = confirmedChartItemsComplete.Concat(provisionalChartItemsComplete).DistinctBy(x => x.Label).Count();
                            chartModels.Add(new ChartModel
                            {
                                ChartTitle = customChartTitleGenerator?.Invoke(name) ?? $"Load for {name}",
                                ChartOptions = BuildNewChartOptionsObject(numberRows),
                                ConfirmedChartItems = confirmedChartItemsComplete,
                                ProvisionalChartItems = provisionalChartItemsComplete
                            });
                        }
                    }

                    Debug.WriteLine($"** Done. Unfunded = {includeUnFunded} | Leavers = {includeLeavers} | Finished = {includeFinished}.");

                    // Format X Axis range based on last end date of real assignments (i.e. not padding assignments)
                    var allItems = chartModels.SelectMany(x => x.ConfirmedChartItems.Concat(x.ProvisionalChartItems)).Where(x => x.Value1 != 0);
                    long? endDateForAxisRange = allItems.Count() > 0 ? DateTime.Today.AddYears(1).ToUnixTimeMilliseconds() : null;
                    foreach (var opt in chartModels.Select(x => x.ChartOptions))
                    {
                        opt.Xaxis.Min = manualStartDate == null ? DateTime.Today.AddDays(-14).ToUnixTimeMilliseconds() : manualStartDate.Value.ToUnixTimeMilliseconds();
                        opt.Xaxis.Max = manualEndDate == null ? endDateForAxisRange : manualEndDate.Value.ToUnixTimeMilliseconds();
                    }
                    Debug.WriteLine($"** Reconfguring the chart on XAxis range {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Min} to {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Max}");

                    return Task.CompletedTask;
                }
                catch (Exception e)
                {
                    LogError(e.Message);
                    throw new Exception("Task Faulted: See inner exception.", e);
                }
            })
            .ContinueWith(task =>
            {
                Debug.WriteLine($"** ...task complete. Status = {task.Status}");

                afterConfigureTask?.Invoke();

                InvokeAsync(() =>
                {
                    if (task.IsFaulted)
                    {
                        ShowNotification(new CapXNotificationMessage
                        {
                            Severity = NotificationSeverity.Error,
                            Summary = "Chart Generation Failed",
                            Detail = "Chart generation has unexpectedly failed. Please report this as a bug!"
                        });
                    }

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
        /// Method to get a unique session storage tag for the page
        /// </summary>
        /// <returns></returns>
        protected abstract string GetSessionStorageTag();

        /// <summary>
        /// Get managers from a list of people
        /// </summary>
        /// <param name="people"></param>
        /// <returns></returns>
        protected IList<Person> GetManagers(IEnumerable<Person> people)
        {
            var users = UserService.GetAll(Context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            return people.Where(x => users.Any(y => y.Person.PersonId == x.PersonId)).ToList();
        }

        /// <summary>
        /// Filters the cached people to just those with assignments with the duties listed
        /// </summary>
        /// <param name="cachedPeople"></param>
        /// <param name="duties"></param>
        /// <returns></returns>
        protected IEnumerable<Person> GetPeopleWithAssignmentsWithDuty(IEnumerable<Person> cachedPeople, Duty[] duties)
        {
            // Ask the subtask service to filter based on assigned resources and duties
            return SubTaskService.GetPeopleWithAssignmentsWithDuty(Context, cachedPeople, duties);
        }

        /// <summary>
        /// Method to reload the dropdown sources on the page
        /// </summary>
        protected virtual Task ReloadDropDownSourcesAsync()
        {
            Debug.WriteLine("** Reloading dropdown sources...");

            // Get people and filter if PM selected
            people = cachedPeople.ToList();

            // Filter out leavers if necessary
            if (!includeLeavers)
            {
                people = people
                    .Where(x => x.EndDate == null || x.EndDate >= DateTime.Today)
                    .OrderBy(x => x.Name)
                    .ToList();
            }

            // Apply autocomplete box filters
            LoadFilteredPeople(new LoadDataArgs());

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

            return Task.CompletedTask;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            // Navigate away if feature not enabled
            if (!FeatureService.IsFeatureEnabled(FeatureType.ProjectsAndCapacity))
            {
                Navigation.NavigateTo("people");
                return;
            }

            // Get all projects not finished or cancelled
            cachedProjects = ProjectService.GetAll(Context).Where(x => !x.ProjectStatus.IsCancelled());

            // Cache all the people
            cachedPeople = await PersonService.GetAllShallowAsync(Context);

            // Load dropdown sources
            await ReloadDropDownSourcesAsync();

            chosenPeople = await SessionStorage.GetItemAsync<IEnumerable<string>>($"{GetSessionStorageTag()}-chosen-people");
            Debug.WriteLine($"** From session storage: {(chosenPeople != null ? string.Join('|', chosenPeople) : "")}");

            // If there is a query parameter then use it
            if (FilterPersonId != null)
            {
                var matchingPerson = cachedPeople.FirstOrDefault(x => x.PersonId == FilterPersonId);
                if (matchingPerson != null)
                {
                    chosenPeople = new List<string>
                    {
                        matchingPerson.Name
                    };
                }
            }

            // Check that the boolean flags are not null (i.e. that they exist in session storage) before overwriting defaults
            var temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-leavers");
            if (temp != null)
            {
                includeLeavers = temp ?? false;
            }
            temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-unfunded");
            if (temp != null)
            {
                includeUnFunded = temp ?? false;
            }
            temp = await SessionStorage.GetItemAsync<bool?>($"{GetSessionStorageTag()}-include-finished");
            if (temp != null)
            {
                includeFinished = temp ?? false;
            }

            // Reload dropdowns sources
            await ReloadDropDownSourcesAsync();
        }

        /// <summary>
        /// Determine whether any people are chosen
        /// </summary>
        /// <returns></returns>
        protected bool PeopleChosen()
        {
            return chosenPeople != null && chosenPeople.Count() > 0;
        }

        /// <summary>
        /// Use the master list of people to filter the data source for the dropdown based on user typing
        /// </summary>
        /// <param name="args"></param>
        protected void LoadFilteredPeople(LoadDataArgs args)
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
        /// Updates all the chart models that do not have the matching options object with the min and max provided
        /// </summary>
        /// <param name="options"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        protected void UpdateZoomAcrossCharts(ApexChartOptions<ChartItem> options, object min, object max)
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

            // Fire a background task to wait for the JS to run then trigger a redraw
            Task.Run(async () => await Task.Delay(300)).ContinueWith(async t => await InvokeAsync(StateHasChanged));
        }

        /// <summary>
        /// Method to automatically zoom the charts to the number of months in the future
        /// </summary>
        /// <param name="numberOfMonths"></param>
        protected void SetZoomToMonthsAhead(int numberOfMonths)
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

        /// <summary>
        /// Callback when an item is zoomed
        /// </summary>
        /// <param name="zoomedData"></param>
        protected void OnChartZoomed(ZoomedData<ChartItem> zoomedData)
        {
            if (zoomedData != null)
            {
                Debug.WriteLine($"** {zoomedData.Chart.ChartId} Zoomed {zoomedData.XAxis.Min} to {zoomedData.XAxis.Max}");
                UpdateZoomAcrossCharts(zoomedData.Chart.Options, zoomedData.XAxis.Min, zoomedData.XAxis.Max);
            }
        }

        /// <summary>
        /// Takes the cached projects on the page and filters them based on the state of the switches and dropdowns
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerable<Project> GetValidProjects()
        {
            var validProjects = cachedProjects;

            // Filter projects based on finished
            if (!includeFinished)
            {
                Debug.WriteLine("** Removing finished projects...");
                validProjects = validProjects.Where(p => p.ProjectStatus != ProjectStatus.Finished);
            }

            // Filter projects based on unfunded
            if (!includeUnFunded)
            {
                Debug.WriteLine("** Removing unfunded projects...");
                validProjects = validProjects.Where(p => !p.ProjectStatus.IsUnfunded());
            }

            return validProjects;
        }

        /// <summary>
        /// Creates a standard chart options object to be pass to all chart instances -- they cannot share the same object
        /// </summary>
        /// <param name="totalRowsOnChart"></param>
        /// <returns></returns>
        protected virtual ApexChartOptions<ChartItem> BuildNewChartOptionsObject(int totalRowsOnChart)
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
                    Height = $"{(totalRowsOnChart * 30) + 100}px",
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
    }
}
