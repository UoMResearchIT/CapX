// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Enums;
using PPMTool.Helpers;
using PPMTool.Models;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader")]
    public partial class Capacity : BaseCapacityPage
    {
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

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            // Certain roles can use the dropdowns and save manager settings so need to reload
            if (CanCustomise())
            {
                // Load settings
                var managerName = await SessionStorage.GetItemAsync<string>($"{GetStorageTag()}-chosen-manager");
                ChosenManager = managers.FirstOrDefault(x => x.Name == managerName);

                // Reload the dropdown sources if a manager has been chosen
                if (ChosenManager != null)
                {
                    await ReloadDropDownSourcesAsync();
                }
            }
            else
            {
                // Choose the person automatically if not a manager
                chosenPeople = new List<string>
                {
                    ActiveUser.GetName()
                };

                // Will automatically load the chart source
                await PeopleSelectionChangedAsync(chosenPeople);
            }

            // Load the chart source based on the current configuration
            await ConfigureChartSourceAsync();

            LogInformation($"Viewing capacity page");
        }

        /// <summary>
        /// Override to provide a unique tag for session storage for this page.
        /// </summary>
        /// <returns></returns>
        protected override string GetStorageTag() => "capacity";

        /// <summary>
        /// Method to setup the dropdown sources
        /// </summary>
        protected override async Task ReloadDropDownSourcesAsync()
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
                await SavePeopleStateAsync();
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
        private void SaveManagerState() => SessionStorage.SetItemAsync($"{GetStorageTag()}-chosen-manager", chosenManager == null ? null : chosenManager.Name);

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
        private async Task ManagerSelectionChangedAsync(object selectedOptions)
        {
            var item = selectedOptions as Person;
            Debug.WriteLine($"** Selected Manager: {item?.Name}");

            // Save the new state
            SaveManagerState();

            // Reload the people to include just those working on projects that PM manages
            await ReloadDropDownSourcesAsync();

            // Log selection
            LogInformation($"Selected manager: {item?.Name}");
        }

        /// <summary>
        /// Quickly select those people in the available list that the active user manages
        /// </summary>
        private async Task FilterToMyStaffAsync()
        {
            chosenPeople = people.Where(x => x.LineManager?.PersonId == ActiveUser?.Person?.PersonId).Select(x => x.Name);
            await PeopleSelectionChangedAsync(chosenPeople);
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
        /// Wrapper for the chart configuration event that sets the optional parameters relevant for this page
        /// </summary>
        private async Task ConfigureChartSourceAsync()
        {
            await ConfigureChartSourceAsync(
                customChartTitleGenerator: (name) => $"Load for {(!string.IsNullOrEmpty(name) ? name : "All")} {(ManagerChosen() ? " with manager " + ChosenManager.Name : "")}",
                projectModeCondition: () => ManagerChosen()
            );
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
            IEnumerable<Assignment> assignments,
            DateTime startDate,
            DateTime endDate)
        {
            return ChartHelper.ConvertAssignmentsToChartItems(
                assignments,
                // Value 1 function
                (assignments, currentDay) => assignments.RoundedSum(assignment => assignment.SubTask.GetAssignmentValueForPerson(person)),
                // Colour function
                (value1, value2, isHatched) =>
                {
                    return ChartItem.GetColourStringFTE(value1, value2);
                },
                person.Name,
                startDate,
                endDate,
                person,
                // Hatched function -- If any resources are marked as provisional or the project owning the task is not funded, active or in maintenance i.e. unconfirmed.
                assignments => assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed() || assignment.SubTask.IsProvisionalResource(person)),
                // Value 2 function
                (assignments, value1, currentDay) =>
                {
                    return person.GetProjectWorkAvailabilityOnDate(currentDay);
                },
                // Gap filler function
                (assignments, gapStart, gapEnd) =>
                {
                    return ChartHelper.FillGapsBetweenChartItemsFromWorkloadModels(
                        person,
                        PersonService.GetWorkloadModelChanges(Context, person.PersonId),
                        gapStart,
                        gapEnd,
                        wlm => 0,
                        wlm => wlm?.ProjectWorkFTE ?? 0,
                        (double value1, double value2, bool isHatched) => ChartItem.GetColourStringFTE(value1, value2)
                    );
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
                (assignments, currentDay) => assignments.RoundedSum(assignment => assignment.SubTask.GetAssignmentValueForPerson(person)),
                // Colour function
                (value1, value2, isHatched) =>
                {
                    // Shading function based on value 1 and value 2
                    return ChartItem.GetColourStringFTE(value1, isTotalRow ? value2 : 1, isTotalRow ? ColourScale.Capacity : ColourScale.Load);
                },
                seriesName,
                startDate,
                endDate,
                // Providing the person truncates the blocks to the person's start and end dates which we want for the total row
                person: isTotalRow ? person : null,
                // Hatched function
                hatchedFunction: assignments => assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed() || assignment.SubTask.IsProvisionalResource(person)),
                // Value 2 for each block
                value2Function: (assignments, value1, currentDay) =>
                {
                    var peo = people.Where(y => y == person);

                    // The total availability of the person becomes value 2
                    return peo.RoundedSum(y => y.GetProjectWorkAvailabilityOnDate(currentDay));
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
                        var assignedWithinBlockWithChosenPerson = assignmentsWithinBlock.Where(x => x?.SubTask.AssignedResources.Any<Resource>(x => x.Person.PersonId == person.PersonId) ?? false);
                        if (assignedWithinBlockWithChosenPerson.Any(x => x?.SubTask.HasUnmetDemand() ?? false))
                        {
                            var unmetDemand = assignedWithinBlockWithChosenPerson.RoundedSum(x => x?.SubTask.UnmetDemand ?? 0);
                            messages += $"<h3 class=\"me-1 text-danger\"> &#x26A0; [UNMET DEMAND ({unmetDemand} FTE)]</h3>";
                        }
                    }

                    // Generate further, universal messages
                    messages = GenerateTooltipMessages(assignmentsWithinBlock, person, messages);

                    return messages;
                },
                ignoreZeroValue1Entries: !isTotalRow
            );
        }

        /// <inheritdoc />
        protected override void PopulateGroupedAssignmentsForPeople(
            IEnumerable<Project> projects,
            IEnumerable<Person> people,
            bool isPersonMode,
            Duty[] dutySet = null)
        {
            base.PopulateGroupedAssignmentsForPeople(projects, people, isPersonMode, [Duty.ProjectWork]);
        }
    }
}
