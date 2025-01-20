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

            LogInformation($"Viewing management capacity page");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (!firstRender) return;

            PeopleSelectionChanged(ChosenPeople);
        }

        protected override string GetSessionStorageTag() => "management-capacity";

        protected override void PopulateGroupedAssignmentsForPeople(IEnumerable<Project> projects, IEnumerable<Person> people, bool isPersonMode)
        {
            groupedAssignments = new Dictionary<object, IEnumerable<Assignment>>();

            if (isPersonMode)
            {
                // Person -> Leadership assignments (for all projects)
                foreach (var person in people)
                {
                    var ownedProjects = projects.Where(x => x.ProjectManager.PersonId == person.PersonId);
                    var assignments = new List<Assignment>();
                    foreach (var project in ownedProjects)
                    {
                        assignments.Add(new Assignment)
                    }
                }
            }
            else
            {
                // Project -> Leadership assignments (for a given person)
            }
        }

        /// <summary>
        /// Only called in person mode per person to generate chart items. Assumed assignments only contain projects
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
                startDate,
                endDate,
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
                tooltipMessageFormatter: assignmentsInBlock => string.Empty
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
            // TODO: Check
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
                startDate,
                endDate,
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

                    return messages;
                }
            );
        }
    }
}
