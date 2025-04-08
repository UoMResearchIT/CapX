using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader")]
    public partial class Capacity : BaseCapacityPage
    {
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

        private IList<Person> managers;
        private IList<Person> filteredManagers;
        private DateTime queryEndDate = DateTime.Today.AddDays(7);
        private bool queryResultsAvailable;
        private string queryErrorMessage;
        private bool queryActive;
        private double requiredFTE = 0.5;
        private List<CapacityQueryItem> fullMatch;
        private List<CapacityQueryItem> partialMatchPercent;
        private List<CapacityQueryItem> partialMatchDuration;
        private List<CapacityQueryItem> partialMatchBoth;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LogInformation($"Viewing capacity page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            // Certain roles can use the dropdowns and save manager settings so need to reload
            if (EditAuthorised || ActiveUserRoleType == RoleType.Reader)
            {
                // Load settings
                var managerName = await SessionStorage.GetItemAsync<string>($"{GetSessionStorageTag()}-chosen-manager");
                ChosenManager = managers.FirstOrDefault(x => x.Name == managerName);

                // Reload the dropdown sources if a manager has been chosen
                if (ChosenManager != null)
                {
                    ReloadDropDownSources();
                }

                // Load the chart source based on the current configuration
                ConfigureChartSource();
            }
            else
            {
                // Choose the person automatically if not a manager
                chosenPeople = new List<string>
                {
                    ActiveUser.GetName()
                };

                // Will automatically load the chart source
                PeopleSelectionChanged(chosenPeople);
            }
        }

        protected override string GetSessionStorageTag() => "capacity";

        private bool IsDeveloper() => ActiveUserRoleType == RoleType.Developer;

        /// <summary>
        /// Method to setup the dropdown sources
        /// </summary>
        protected override void ReloadDropDownSources()
        {
            Debug.WriteLine("** Reloading dropdown sources...");
            people = cachedPeople.ToList();

            // Filter if PM selected
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
            managers = GetManagers(cachedPeople);

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
            if (chosenPeople != null && chosenPeople.Count() > 0)
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

                // Update the session storage
                SavePeopleState();
            }
        }

        /// <summary>
        /// Determine whether a manager has been chosen
        /// </summary>
        /// <returns></returns>
        private bool ManagerChosen() => ChosenManager != null;

        /// <summary>
        /// Save the chosen manager to session storage
        /// </summary>
        private void SaveManagerState() => SessionStorage.SetItemAsync($"{GetSessionStorageTag()}-chosen-manager", chosenManager == null ? null : chosenManager.Name);

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

            // Reconfigure the chart
            ConfigureChartSource();

            LogInformation($"Selected manager: {item?.Name}");
        }

        /// <summary>
        /// Quickly select those people in the available list that the active user manages
        /// </summary>
        private void FilterToMyStaff()
        {
            chosenPeople = people.Where(x => x.LineManager?.PersonId == ActiveUser?.Person?.PersonId).Select(x => x.Name);
            PeopleSelectionChanged(chosenPeople);
        }

        /// <summary>
        /// Does the active user manage staff that are in the available list
        /// </summary>
        /// <returns></returns>
        private bool HasStaffInList()
        {
            return people.Any(x => x.LineManager?.PersonId == ActiveUser?.Person?.PersonId);
        }

        /// <summary>
        /// Wrapper for the chart configuration event that sets the optional paramters
        /// </summary>
        private void ConfigureChartSource()
        {
            ConfigureChartSource(
                ConvertChartItemsToQueryResults,
                queryActive ? QueryStartDate : null,
                queryActive ? queryEndDate : null,
                customChartTitleGenerator: (name) => $"Load for {(!string.IsNullOrEmpty(name) ? name : "All")} {(ManagerChosen() ? " with manager " + ChosenManager.Name : "")}",
                () => ManagerChosen()
            );
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
            chosenPeople = new List<string>();
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
            ConfigureChartSource();
        }

        /// <summary>
        /// Method to take the result of the chart configuration and convert it to query results in the table
        /// </summary>
        private void ConvertChartItemsToQueryResults()
        {
            if (queryActive)
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
                fullMatch = OrganiseQueryResults(results
                    .Where(x => x.AvailabilityPercent == requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate));
                partialMatchPercent = OrganiseQueryResults(results
                    .Where(x => x.AvailabilityPercent == requiredFTE && (x.EndDate != queryEndDate || x.StartDate != queryStartDate)));
                partialMatchDuration = OrganiseQueryResults(results
                    .Where(x => x.AvailabilityPercent != requiredFTE && x.EndDate == queryEndDate && x.StartDate == queryStartDate));
                partialMatchBoth = OrganiseQueryResults(results
                    .Where(x => x.AvailabilityPercent != requiredFTE && (x.EndDate != queryEndDate || x.StartDate != queryStartDate)));

                // Results available
                queryResultsAvailable = results.Count() > 0;

                LogInformation("Query results generated.");
            }
        }

        /// <summary>
        /// Method to order the capacity query results
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        private List<CapacityQueryItem> OrganiseQueryResults(IEnumerable<CapacityQueryItem> results)
        {
            return results
                .OrderBy(x => x.Person.Name)
                .ThenByDescending(x => x.AvailabilityPercent)
                .ToList();
        }

        /// <summary>
        /// Takes the cached projects on the page and filters them based on the state of the switches and dropdowns
        /// </summary>
        /// <returns></returns>
        protected override IEnumerable<Project> GetValidProjects()
        {
            var validProjects = base.GetValidProjects();

            // Filter the project source if a manager selected
            if (ChosenManager != null)
            {
                Debug.WriteLine("** Removing projects not belonging to selected manager...");
                validProjects = validProjects.Where(x => x.ProjectManager.PersonId == ChosenManager.PersonId);
            }

            return validProjects;
        }

        /// <summary>
        /// Only called in person mode per person to generate chart items
        /// </summary>
        /// <param name="person"></param>
        /// <param name="assignments"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        protected override IEnumerable<ChartItem> GetPersonModeChartItemsFromAssignments(
            Person person,
            IEnumerable<BaseAssignment> assignments,
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
                        var resource = (assignment as Assignment)?.SubTask.AssignedResources.First(x => x.Person.Name == person.Name);
                        return resource?.AssignmentFTE ?? 0;
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
                            ((assignment as Assignment)?.SubTask.AssignedResources.First(x => x.Person == person).IsProvisional ?? true);
                    });
                },
                (assignments, value1, currentDay) =>
                {
                    return person.GetAvailabilityOnDate(currentDay);
                },
                (assignments, gapStart, gapEnd) =>
                {
                    return FillGapsBetweenChartItemsFromWorkloadModels(person, gapStart, gapEnd, wlm => wlm?.ProjectWorkFTE ?? person.FTE);
                },
                assignmentsInBlock => GenerateTooltipMessages(assignmentsInBlock, person, string.Empty)
            );
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
        protected override IEnumerable<ChartItem> GetProjectModeChartItemsFromAssignments(
            string seriesName,
            KeyValuePair<object, IEnumerable<BaseAssignment>> groupedAssignments,
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
                        var resource = (assignment as Assignment)?.SubTask.AssignedResources.First(x => x.Person.Name == person.Name);
                        return resource?.AssignmentFTE ?? 0;
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
                        var resource = (assignment as Assignment)?.SubTask.AssignedResources.First(x => x.Person == person);

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
                tooltipMessageFormatter: assignmentsWithinBlock =>
                {
                    var messages = string.Empty;

                    // When not a total row, the group key will be a project.
                    var projectForRow = groupedAssignments.Key as Project;
                    if (projectForRow != null)
                    {
                        // Always return the project manager on the tooltip for project rows
                        messages += $"PM: {projectForRow.ProjectManager?.Name ?? "Not Set"}";

                        // Check whether this project has unmet demand on the tasks to which this person is assigned
                        var assignedWithinBlockWithChosenPerson = assignmentsWithinBlock.Where(x => (x as Assignment)?.SubTask.AssignedResources.Any(x => x.Person == person) ?? false);
                        if (assignedWithinBlockWithChosenPerson.Any(x => (x as Assignment)?.SubTask.HasUnmetDemand() ?? false))
                        {
                            var unmetDemand = assignedWithinBlockWithChosenPerson.RoundedSum(x => (x as Assignment)?.SubTask.UnmetDemand ?? 0);
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
        /// Method to generate tooltip messages for the chart blocks
        /// </summary>
        /// <param name="assignmentsWithinBlock"></param>
        /// <param name="personOfInterest"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected override string GenerateTooltipMessages(IEnumerable<BaseAssignment> assignmentsWithinBlock, Person personOfInterest, string messages)
        {
            messages = base.GenerateTooltipMessages(assignmentsWithinBlock, personOfInterest, messages);

            // Add the provisional resource warning to the tooltip if chosen person is provisional on the project
            if (assignmentsWithinBlock.Any(x => (x as Assignment)?.SubTask.AssignedResources.Any(x => x.Person == personOfInterest && x.IsProvisional) ?? true))
            {
                messages += "<h3 class=\"me-1 text-warning\"> &#x26A0; [PROVISIONAL ASSIGNMENT]</h3>";
            }

            return messages;
        }
    }
}
