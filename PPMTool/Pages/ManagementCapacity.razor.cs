using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexCharts;
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

        /// <summary>
        /// Pulls project info from the DB and packages the data into a plottable format
        /// </summary>
        protected override void ConfigureChartSource()
        {
            // TODO: Check
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

                // -------------- PERSON MODE -------------- //

                // Flatten subtasks and group by person if "All" chosen
                if (!PeopleChosen())
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
                                startDate,
                                endDate
                            )
                        );
                    }

                    // Add data
                    chartModels.Add(new ChartModel
                    {
                        ChartTitle = $"Load for All",
                        ChartOptions = BuildNewChartOptionsObject(),
                        ConfirmedChartItems = chartSourceTemp.Where(x => !x.IsHatched).ToList(),
                        ProvisionalChartItems = chartSourceTemp.Where(x => x.IsHatched).ToList()
                    });
                }

                // -------------- PROJECT MODE -------------- //

                // Filter by people chosen, flatten and group by project if in project mode
                else
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
                            ChartTitle = $"Load for {name}",
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
                    opt.Xaxis.Min = DateTime.Today.AddDays(-14).ToUnixTimeMilliseconds();
                    opt.Xaxis.Max = endDateForChartNoQuery;
                }
                Debug.WriteLine($"** Reconfguring the chart on XAxis range {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Min} to {chartModels.FirstOrDefault()?.ChartOptions.Xaxis?.Max}");

                return Task.CompletedTask;

            }), configureChartTaskCancellationTokenSource.Token)
            .ContinueWith(task =>
            {
                Debug.WriteLine($"** ...task complete. Status = {task.Status}");

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
            // TODO: Check
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
    }
}
