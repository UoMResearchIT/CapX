// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
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
    public partial class BaselineCapacity : BaseCapacityPage
    {
        private bool pageUnavailable = false;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            // Update the cached people to just contain people with BAU assignments
            cachedPeople = GetPeopleWithAssignmentsWithDuty(cachedPeople, [Duty.BAU]);
            await ReloadDropDownSourcesAsync();

            // Certain roles can use the dropdowns and save manager settings so need to reload
            if (!CanCustomise())
            {
                // If the person is not the list then mark as unavailable
                if (!cachedPeople.Select(x => x.Name).Contains(ActiveUser.GetName()))
                {
                    pageUnavailable = true;
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
            }

            // Load the chart source based on the current configuration
            await ConfigureChartSourceAsync();

            LogInformation($"Viewing baseline capacity page");
        }

        protected override string GetSessionStorageTag() => "baseline";

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
                    return person.GetBAUAvailability(currentDay);
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
                        wlm => wlm?.BusinessAsUsualFTE ?? 0,
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
                    return peo.RoundedSum(y => y.GetBAUAvailability(currentDay));
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
            base.PopulateGroupedAssignmentsForPeople(projects, people, isPersonMode, [Duty.BAU]);
        }
    }
}
