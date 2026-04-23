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
    [Authorize(Roles = "Manager,Superuser,Reader")]
    public partial class ManagementCapacity : BaseCapacityPage
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            // Update the cached people to just contain managers
            cachedPeople = GetManagers(cachedPeople);
            await ReloadDropDownSourcesAsync();

            await PeopleSelectionChangedAsync(chosenPeople);

            // Load the chart source based on the current configuration
            await ConfigureChartSourceAsync();

            LogInformation($"Viewing management capacity page");
        }

        protected override string GetSessionStorageTag() => "management-capacity";

        /// <summary>
        /// Only called in person mode per person to generate chart items. Assumed assignments only contain projects that are owned by this person.
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
                // Hatched function
                assignments => assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed()),
                // Value 2 function
                (assignments, value1, currentDay) =>
                {
                    return person.GetProjectManagementCapacityOnDate(currentDay);
                },
                // Gap filling function
                (assignments, gapStart, gapEnd) =>
                {
                    return ChartHelper.FillGapsBetweenChartItemsFromWorkloadModels(
                        person,
                        PersonService.GetWorkloadModelChanges(Context, person.PersonId),
                        gapStart,
                        gapEnd,
                        wlm => 0,
                        wlm => wlm?.ProjectManagementFTE ?? 0,
                        (value1, value2, isHatched) => ChartItem.GetColourStringFTE(value1, value2)
                    );
                },
                assignmentsWithinBlock => GenerateTooltipMessages(assignmentsWithinBlock, person, string.Empty)
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
                // Value 1 for each block -- Value is the effort of the chosen person
                (assignments, currentDay) => assignments.RoundedSum(assignment => assignment.SubTask.GetAssignmentValueForPerson(person)),
                // Colour function
                (value1, value2, isHatched) =>
                {
                    // Shading function based on value 1 and value 2
                    return isTotalRow ?
                        ChartItem.GetColourStringFTE(value1, value2) :
                        (
                            value1 > GlobalDefaults.ProjectManagementDefaultFTE ?
                                "#FF9800" :
                                "#609"
                        );
                },
                seriesName,
                startDate,
                endDate,
                // Hatched function
                hatchedFunction: assignments => assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed()),
                // Value 2 for each block
                value2Function: (assignments, value1, currentDay) =>
                {
                    return person.GetProjectManagementCapacityOnDate(currentDay);
                },
                tooltipMessageFormatter: assignmentsWithinBlock => GenerateTooltipMessages(assignmentsWithinBlock, person, string.Empty),
                ignoreZeroValue1Entries: !isTotalRow
            );
        }

        /// <summary>
        /// Override to add the increased leadership tooltip to the base ones
        /// </summary>
        /// <param name="assignmentsWithinBlock"></param>
        /// <param name="personOfInterest"></param>
        /// <param name="messages"></param>
        /// <returns></returns>
        protected override string GenerateTooltipMessages(IEnumerable<Assignment> assignmentsWithinBlock, Person personOfInterest, string messages)
        {
            // Add project badges
            foreach (var status in assignmentsWithinBlock.Select(x => x.ProjectStatus).Distinct())
            {
                messages += $"<div class=\"rz-badge {GetCSSBadgeStyle(status.GetBadgeStyle())}\">{status.ToNiceString()}</div>&nbsp";
            }

            // Add the base messages
            messages = base.GenerateTooltipMessages(assignmentsWithinBlock, personOfInterest, messages);

            // Check for leadership load greater than the standard
            if (assignmentsWithinBlock.Any(x => x.SubTask.GetAssignmentValueForPerson(personOfInterest) > GlobalDefaults.ProjectManagementDefaultFTE))
            {
                var amount = assignmentsWithinBlock.RoundedSum(x => x.SubTask.GetAssignmentValueForPerson(personOfInterest) > GlobalDefaults.ProjectManagementDefaultFTE ?
                    x.SubTask.GetAssignmentValueForPerson(personOfInterest) :
                    0
                );
                messages += $"<h3 class=\"me-1 text-warning\"> &#x26A0; [INCREASED LEADERSHIP ({amount} FTE)]</h3>";
            }
            return messages;
        }

        /// <summary>
        /// Convert the Radzen badge style to CSS style class
        /// </summary>
        /// <param name="badgeStyle"></param>
        /// <returns></returns>
        private string GetCSSBadgeStyle(BadgeStyle badgeStyle)
        {
            switch (badgeStyle)
            {
                case BadgeStyle.Success:
                    return "rz-badge-success";

                case BadgeStyle.Info:
                    return "rz-badge-info";
            }
            return "rz-badge-light";
        }

        /// <inheritdoc />
        protected override void PopulateGroupedAssignmentsForPeople(
            IEnumerable<Project> projects,
            IEnumerable<Person> people,
            bool isPersonMode,
            TaskSubset taskSet = TaskSubset.TechOnly)
        {
            base.PopulateGroupedAssignmentsForPeople(projects, people, isPersonMode, TaskSubset.LeadershipOnly);
        }
    }
}
