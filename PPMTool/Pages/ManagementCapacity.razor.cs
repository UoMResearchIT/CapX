using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Reader")]
    public partial class ManagementCapacity : BaseCapacityPage
    {
        protected override void OnInitialized()
        {
            base.OnInitialized();

            cachedPeople = GetManagers(cachedPeople);
            ReloadDropDownSourcesAndChartSource();

            LogInformation($"Viewing management capacity page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            PeopleSelectionChanged(chosenPeople);
        }

        protected override string GetSessionStorageTag() => "management-capacity";

        /// <summary>
        /// Method to construct leadership assignment objects for the given projects and people
        /// </summary>
        /// <param name="projects"></param>
        /// <param name="people"></param>
        /// <param name="isPersonMode"></param>
        protected override void PopulateGroupedAssignmentsForPeople(IEnumerable<Project> projects, IEnumerable<Person> people, bool isPersonMode)
        {
            groupedAssignments = new Dictionary<object, IEnumerable<BaseAssignment>>();

            if (isPersonMode)
            {
                // Person -> Leadership assignments (for all projects)
                foreach (var person in people)
                {
                    var ownedProjects = projects.Where(x => x.ProjectManager.PersonId == person.PersonId);
                    var assignments = new List<LeadershipAssignment>();
                    foreach (var project in ownedProjects)
                    {
                        // Find leadership tasks and convert to leadership assignment
                        var dateRanges = project.GetLeadershipTaskRanges();
                        foreach (var dateRange in dateRanges)
                        {
                            assignments.Add(new LeadershipAssignment(dateRange, project.LeadershipFTE, project.ProjectStatus));
                        }
                    }

                    groupedAssignments.Add(person, assignments);
                }
            }
            else
            {
                // Project -> Leadership assignments (for a given person)
                var person = people.First();
                var ownedProjects = projects.Where(x => x.ProjectManager.PersonId == person.PersonId);
                foreach (var project in ownedProjects)
                {
                    // Find leadership tasks and convert to leadership assignment
                    var assignments = new List<LeadershipAssignment>();
                    var dateRanges = project.GetLeadershipTaskRanges();
                    foreach (var dateRange in dateRanges)
                    {
                        assignments.Add(new LeadershipAssignment(dateRange, project.LeadershipFTE, project.ProjectStatus));
                    }
                    if (assignments.Count > 0) groupedAssignments.Add(project, assignments);
                }
            }
        }

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
            IEnumerable<BaseAssignment> assignments,
            DateTime startDate,
            DateTime endDate)
        {
            return ChartHelper.ConvertAssignmentsToChartItemsForPerson(
                person,
                assignments,
                assignments =>
                {
                    return assignments.RoundedSum(assignment => (assignment as LeadershipAssignment)?.LeadershipFTE ?? 0);
                },
                (value1, value2) =>
                {
                    return ChartItem.GetColourStringFTE(value1, value2);
                },
                person.Name,
                startDate,
                endDate,
                assignments =>
                {
                    return assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed());
                },
                (assignments, value1, currentDay) =>
                {
                    return person.GetProjectManagementCapacityOnDate(currentDay);
                },
                (assignments, gapStart, gapEnd) =>
                {
                    return FillGapsBetweenChartItemsFromWorkloadModels(person, gapStart, gapEnd, wlm => wlm?.ProjectManagementFTE ?? 0);
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
                    return assignments.RoundedSum(assignment => (assignment as LeadershipAssignment)?.LeadershipFTE ?? 0);
                },
                // Colour function
                (value1, value2) =>
                {
                    // Shading function based on value 1 and value 2
                    return ChartItem.GetColourStringFTE(value1, isTotalRow ? value2 : 1, !isTotalRow);
                },
                seriesName,
                startDate,
                endDate,
                // Hatched function
                assignments =>
                {
                    return assignments.Any(assignment => assignment.ProjectStatus.IsUnconfirmed());
                },
                // Value 2 for each block
                (assignments, value1, currentDay) =>
                {
                    return person.GetProjectManagementCapacityOnDate(currentDay);
                }
            );
        }
    }
}
