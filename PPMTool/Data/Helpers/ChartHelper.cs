using System.Diagnostics;
using PPMTool.Data.Entities;

namespace PPMTool.Data.Helpers
{
    public abstract class ChartHelper
    {
        /// <summary>
        /// For a given person, convert assignments into an aggregated set of blocks for the timeline graph.
        /// Adds special logic to pad whitespace in the timelines and adjust for person start and end dates.
        /// </summary>
        /// <param name="assignments">Set of assignments to aggregate</param>
        /// <param name="valueFunction">Function to define the primary value of a given block</param>
        /// <param name="colourFunction">Function to define the colour of a given block</param>
        /// <param name="label">Chart axis label for the data</param>
        /// <param name="startDate">Start of aggregation window</param>
        /// <param name="endDate">End of aggregation window</param>
        /// <param name="person">Person of interest if required</param>
        /// <param name="hatchedFunction">Function to determine the "hatched" state of the block</param>
        /// <param name="value2Function">Function to define the secondary value of a given block</param>
        /// <param name="gapFillingFunction">Function that fills gaps in the chart items</param>
        /// <param name="tooltipMessageFormatter">Function to provide HTML string to be shown as tooltip messages for block based on list of assignments that fall within the block</param>
        /// <param name="ignoreZeroValue1Entries">If true, does not create a block if it has a value of 0 for value 1, leaving a gap</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertAssignmentsToChartItems(
            IEnumerable<BaseAssignment> assignments,
            Func<IEnumerable<BaseAssignment>, DateTime, double> valueFunction,
            Func<double, double, bool, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Person person = null,
            Func<IEnumerable<BaseAssignment>, bool> hatchedFunction = null,
            Func<IEnumerable<BaseAssignment>, double, DateTime, double> value2Function = null,
            Func<Person, DateTime, DateTime, IEnumerable<ChartItem>> gapFillingFunction = null,
            Func<IEnumerable<BaseAssignment>, string> tooltipMessageFormatter = null,
            bool ignoreZeroValue1Entries = false
        )
        {
            if (person != null)
            {
                if (person.StartDate > startDate)
                    startDate = person.StartDate;

                if (person.EndDate != null && person.EndDate < endDate)
                    endDate = person.EndDate?.AddDays(1) ?? DateTime.Today;
            }

            var chartItems = AggregateAssignmentsIntoBlocks(
                assignments, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function, tooltipMessageFormatter,
                ignoreZeroValue1Entries
            ).OrderBy(x => x.StartDate).ToList();

            Debug.WriteLine($"** Generated {chartItems.Count} block(s) {(person == null ? "" : person.Name)}");

            // Only action the gap filler if a person is provided
            if (person != null && gapFillingFunction != null)
            {
#if LOCAL
                var currentItemCount = chartItems.Count();
#endif
                var extraItems = new List<ChartItem>();

                if (!chartItems.Any() || chartItems.First().StartDate > startDate)
                {
                    var endFill = chartItems.Any() ? chartItems.First().StartDate : endDate;
                    extraItems.AddRange(gapFillingFunction(person, startDate, endFill));
                }

                if (chartItems.Any() && chartItems.Last().EndDate < endDate)
                {
                    extraItems.AddRange(gapFillingFunction(person, chartItems.Last().EndDate, endDate));
                }

                for (int i = 0; i < chartItems.Count - 1; ++i)
                {
                    if (chartItems[i].EndDate != chartItems[i + 1].StartDate)
                    {
                        extraItems.AddRange(gapFillingFunction(person, chartItems[i].EndDate, chartItems[i + 1].StartDate));
                    }
                }

                if (extraItems.Any())
                {
                    chartItems.AddRange(extraItems);
                    chartItems = chartItems.OrderBy(x => x.StartDate).ToList();
                }
#if LOCAL
                if (currentItemCount != chartItems.Count)
                {
                    Debug.WriteLine($"** {chartItems.Count} block(s) for {person.Name} after gap filling");
                }
#endif
            }

            return chartItems;
        }

        /// <summary>
        /// Time-marching method for summing up the contribution across assignments based on the value function provided.
        /// The results are arranged into irregular blocks of the same continuous value.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="assignments">Assignments to aggregate</param>
        /// <param name="valueFunction">Function used to generate the value for the block</param>
        /// <param name="colourFunction">Function used to generate the colour for the block based on value and value2</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="hatchedFunction">Function to determine whether any of the assignments evaluate the function to true</param>
        /// <param name="value2Function">Function used to generate a second value for the block based on the current week being examined</param>
        /// <param name="tooltipMessageFormatter">Function to return some HTML for a tooltip message based on list of assignments that fall within the block</param>
        /// <param name="ignoreZeroValue1Entries">If true, does not create a block if it has a value of 0 for value 1, leaving a gap</param>
        /// <returns></returns>
        private static IEnumerable<ChartItem> AggregateAssignmentsIntoBlocks(
            IEnumerable<BaseAssignment> assignments,
            Func<IEnumerable<BaseAssignment>, DateTime, double> valueFunction,
            Func<double, double, bool, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<IEnumerable<BaseAssignment>, bool> hatchedFunction = null,
            Func<IEnumerable<BaseAssignment>, double, DateTime, double> value2Function = null,
            Func<IEnumerable<BaseAssignment>, string> tooltipMessageFormatter = null,
            bool ignoreZeroValue1Entries = false
        )
        {
            // Each block is considered an element of a series.
            // We must define an element as a block of the same value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the changes.

            // Initialise
            var temp = new List<ChartItem>();

            // If no subtasks in the list
            if (assignments.Count() < 1)
            {
                // Return no blocks
                return temp;
            }

            // Start marching
            DateTime currentDay = startDate;
            DateTime currentBlockStartDay = startDate;

            // Parameters used to determine when a block should be completed and a new block started
            // Initialise tracked values to something unique so we can detect the first pass through
            double? valueTracked = null;
            double valueDay = 0d;
            bool? hatchedTracked = null;
            bool hatchedDay = false;
            double? value2Tracked = null;
            double value2Day = 0d;

            // March through
            while (currentDay < endDate)
            {
                // Find assignments running on current day
                var within = assignments.Where(x => x.IsWithin(currentDay));

                // Sum value for the current day -- truncate to 2 DP
                valueDay = valueFunction(within, currentDay);

                // Set hatched for the current day
                hatchedDay = hatchedFunction != null ? hatchedFunction(within) : false;

                // Set value2 for the current day
                value2Day = value2Function != null ? value2Function(within, valueDay, currentDay) : 0;

                // Set colour state for the first time
                if (value2Tracked == null) value2Tracked = value2Day;

                // Set hatched state for the first time
                if (hatchedTracked == null) hatchedTracked = hatchedDay;

                // Set the value for the first block
                if (valueTracked == null) valueTracked = valueDay;

                // If any of the tracked parameters have changed then complete block and reset tracking params
                if (valueDay != valueTracked || hatchedDay != hatchedTracked || value2Day != value2Tracked)
                {
                    // Only add a block if its value is non-zero if flag set
                    if (!ignoreZeroValue1Entries || ignoreZeroValue1Entries && valueTracked != 0d)
                    {
                        var assignmentsInBlock = assignments.Where(x => x.IsWithin(currentBlockStartDay, currentDay.AddDays(-1)));
                        // Add the chart item to the results
                        temp.Add(new ChartItem(
                            colourFunction(valueTracked ?? -99, value2Tracked ?? -99, hatchedTracked ?? false),
                            label,
                            currentBlockStartDay,
                            currentDay,
                            valueTracked ?? -99,
                            value2Tracked ?? -99,
                            hatchedTracked ?? false,
                            tooltipMessageFormatter != null ? tooltipMessageFormatter(assignmentsInBlock) : null
                        ));
                    }

                    currentBlockStartDay = currentDay;
                    valueTracked = valueDay;
                    hatchedTracked = hatchedDay;
                    value2Tracked = value2Day;
                }

                // Increment by 1 day
                currentDay = currentDay.AddDays(1);
            }

            // Add the final block if it had a non-zero value
            if (!ignoreZeroValue1Entries || ignoreZeroValue1Entries && valueTracked != 0d)
            {
                // Consider the end date to be inclusive of the final block so do not move back a day like above
                var assignmentsInBlock = assignments.Where(x => x.IsWithin(currentBlockStartDay, currentDay));
                temp.Add(new ChartItem(
                    colourFunction(valueDay, value2Day, hatchedDay),
                    label,
                    currentBlockStartDay,
                    currentDay,
                    valueDay,
                    value2Day,
                    hatchedDay,
                    tooltipMessageFormatter != null ? tooltipMessageFormatter(assignmentsInBlock) : null
                ));
            }
            return temp;
        }

        /// <summary>
        /// Represents the effort data for a given resource on a given task
        /// </summary>
        public class ResourceEffort
        {
            /// <summary>
            /// ID of the person associated with the resource
            /// </summary>
            public int PersonId { get; }

            /// <summary>
            /// The planned work hours
            /// </summary>
            public double PlannedWorkHours { get; private set; }

            /// <summary>
            /// The actual number of hours worked
            /// </summary>
            public double ActualHours { get; private set; }

            /// <summary>
            /// Ctor simply assigns the properties
            /// </summary>
            /// <param name="personId"></param>
            /// <param name="plannedWorkHours"></param>
            /// <param name="actualHours"></param>
            public ResourceEffort(int personId, double plannedWorkHours, double actualHours)
            {
                PersonId = personId;
                PlannedWorkHours = plannedWorkHours;
                ActualHours = actualHours;
            }

            /// <summary>
            /// Update the values for this resource by adding new values to existing
            /// </summary>
            /// <param name="plannedWorkHoursForResource"></param>
            /// <param name="actualWorkHoursForResource"></param>
            internal void UpdateValues(double plannedWorkHoursForResource, double actualWorkHoursForResource)
            {
                PlannedWorkHours += plannedWorkHoursForResource;
                ActualHours += actualWorkHoursForResource;
            }
        }

        /// <summary>
        /// Represents the effort for tasks and assignments for a given week
        /// </summary>
        public class WeeklyTaskEffort
        {
            /// <summary>
            /// The date of the week beginning
            /// </summary>
            public DateTime WeekDate { get; }

            /// <summary>
            /// Sum of the planned work across all tasks assuming all demand is met
            /// </summary>
            public double PlannedWorkHoursDemandMet { get; }

            /// <summary>
            /// Sum of the planned work across all tasks based on assignments
            /// </summary>
            public double AssignedPlannedWorkHours { get; }

            /// <summary>
            /// The actual work hours done by all resources across all tasks in this week
            /// </summary>
            public double ActualWorkHours { get; }

            /// <summary>
            /// Details of every assignment. The sum of the planned work hours will be lower than the 
            /// <see cref="PlannedWorkHoursDemandMet"/> value if there is unmet demand.
            /// </summary>
            public IList<ResourceEffort> ResourceEffort { get; } = new List<ResourceEffort>();

            /// <summary>
            /// Ctor takes the tasks running and the current week to generate resource breakdown.
            /// </summary>
            /// <param name="currentWeek"></param>
            /// <param name="tasksRunningInWeek"></param>
            public WeeklyTaskEffort(DateTime currentWeek, IEnumerable<SubTask> tasksRunningInWeek, IEnumerable<Resource> allResourcesOnProject)
            {
                WeekDate = currentWeek;

                // For every resource on every task build a representation of the information
                foreach (var subTask in tasksRunningInWeek)
                {
                    // Duration of the task
                    int durationOfTask = subTask.DurationDays;

                    // Find the total FTE across all resources
                    double totalFTE = subTask.AssignedResources.Sum(x => x.AssignmentFTE);

                    // Total effort assuming demand met (zero demand tasks contribute nothing in this case)
                    double billableDays = SubTask.GetNumberOfBillableDays(subTask.StartDate, durationOfTask);
                    double effortDemandMet = Math.Floor(billableDays * 7 * subTask.Demand);

                    // Total effort planned based on assigned resources
                    // Note that subTask.PlannedWorkHours will be non-zero if no resources as it uses the demand so can't use that
                    double effortPlanned = Math.Floor(billableDays * 7 * totalFTE);

                    // Total effort actually delivered based on assigned resources
                    double effortActual = subTask.ActualWorkHours;

                    // How many whole days does task run this week
                    int taskDaysThisWeek = subTask.GetTaskDaysInWeek(currentWeek);

                    // Proportion of the task that runs this week
                    double proportionOfTaskThisWeek = taskDaysThisWeek / (double)durationOfTask;

                    // How many days has the task run so far (for actuals)
                    DateTime endDateActuals = subTask.EndDate < DateTime.Today ? subTask.EndDate : DateTime.Today;
                    int daysRunSoFar = (int)(endDateActuals.Subtract(subTask.StartDate).TotalDays) + 1;

                    // Calculate how many days the task ran IN THIS WEEK up to the actuals end date
                    // This prevents over-counting future days in the current week or applying current run-rate to past full weeks incorrectly
                    DateTime actualWeekStart = subTask.StartDate > currentWeek ? subTask.StartDate : currentWeek;
                    DateTime actualWeekEnd = endDateActuals < currentWeek.AddDays(6) ? endDateActuals : currentWeek.AddDays(6);
                    double actualsDaysInWeek = 0;

                    if (actualWeekEnd >= actualWeekStart)
                    {
                        actualsDaysInWeek = (actualWeekEnd - actualWeekStart).TotalDays + 1;
                    }

                    // Proportion of the actuals this week
                    double proportionOfActualsThisWeek = (daysRunSoFar <= 0) ? 0 : actualsDaysInWeek / daysRunSoFar;

                    // Add to the demand for the week across all tasks
                    PlannedWorkHoursDemandMet += effortDemandMet * proportionOfTaskThisWeek;

                    // Add to the planned hours based on assigned resources
                    var plannedHoursForTaskThisWeek = effortPlanned * proportionOfTaskThisWeek;
                    AssignedPlannedWorkHours += plannedHoursForTaskThisWeek;

                    // Add to the actual work hours based on the resources
                    var actualsThisWeek = effortActual * proportionOfActualsThisWeek;
                    ActualWorkHours += actualsThisWeek;

                    // For each resource on the task, calculate their contribution to the planned work
                    foreach (var res in subTask.AssignedResources)
                    {
                        // Work out the contribution of this resource to planned work and actuals
                        var plannedWorkHoursForResource = (res.AssignmentFTE / totalFTE) * plannedHoursForTaskThisWeek;
                        var actualWorkHoursForResource = effortActual > 0 ? (res.ActualWorkHours / effortActual) * actualsThisWeek : 0;

                        // Add a resource effort object if required or update existing
                        var existingResource = ResourceEffort.FirstOrDefault(x => x.PersonId == res.Person.PersonId);
                        if (existingResource == null)
                        {
                            ResourceEffort.Add(new ResourceEffort(res.Person.PersonId, plannedWorkHoursForResource, actualWorkHoursForResource));
                        }
                        else
                        {
                            existingResource.UpdateValues(plannedWorkHoursForResource, actualWorkHoursForResource);
                        }
                    }
                }

                // Check that there are entries for all resources who worked on the project even if not assigned during that week
                foreach (var personId in allResourcesOnProject.Select(x => x.Person.PersonId))
                {
                    if (!ResourceEffort.Any(x => x.PersonId == personId))
                    {
                        ResourceEffort.Add(new ResourceEffort(personId, 0, 0));
                    }
                }
            }

            /// <summary>
            /// Special cosntructor to allow aggregation of the values by adding the values to the previous week values
            /// </summary>
            /// <param name="currentWeek"></param>
            /// <param name="previousWeek"></param>
            public WeeklyTaskEffort(WeeklyTaskEffort currentWeek, WeeklyTaskEffort previousWeek)
            {
                WeekDate = currentWeek.WeekDate;
                PlannedWorkHoursDemandMet = (previousWeek?.PlannedWorkHoursDemandMet ?? 0) + currentWeek.PlannedWorkHoursDemandMet;
                AssignedPlannedWorkHours = (previousWeek?.AssignedPlannedWorkHours ?? 0) + currentWeek.AssignedPlannedWorkHours;

                // Get Monday of the current week
                var mondayThisWeek = DateTime.Today;
                if (mondayThisWeek.DayOfWeek != DayOfWeek.Monday)
                {
                    int daysToSubtract = ((int)mondayThisWeek.DayOfWeek + 6) % 7;
                    mondayThisWeek = mondayThisWeek.AddDays(-daysToSubtract);
                }

                // Copy actuals value from previous week if in the future
                var previousWeekActuals = previousWeek?.ActualWorkHours ?? 0;
                ActualWorkHours = WeekDate.Date <= mondayThisWeek.Date ? previousWeekActuals + currentWeek.ActualWorkHours : previousWeekActuals;

                // Breakout the individual resource data
                foreach (var res in currentWeek.ResourceEffort)
                {
                    double lastWeekPlanned = 0;
                    double lastWeekActual = 0;

                    // Find the existing resource effort for this person from previous week
                    var existingResource = previousWeek?.ResourceEffort?.FirstOrDefault(x => x.PersonId == res.PersonId);

                    // Update the planned and actuals based on this
                    if (existingResource != null)
                    {
                        // If exists, add the values to the existing one
                        lastWeekPlanned = existingResource.PlannedWorkHours;
                        lastWeekActual = existingResource.ActualHours;
                    }

                    // Now add the current week's values to the previous week values
                    ResourceEffort.Add(
                        new ResourceEffort(
                            res.PersonId,
                            lastWeekPlanned + res.PlannedWorkHours,
                            WeekDate.Date <= mondayThisWeek.Date ? lastWeekActual + res.ActualHours : lastWeekActual
                        )
                    );
                }

            }
        }

        /// <summary>
        /// Time-marching method for summing up the contribution week-by-week based on the value functions provided.
        /// The results are arranged into blocks exactly one week in length.
        /// </summary>
        /// <param name="subTasks">All subtasks associated with a project</param>
        /// <returns></returns>
        public static IEnumerable<WeeklyTaskEffort> GetWeeklyTaskEffortItems(IEnumerable<SubTask> subTasks)
        {
            // Initialise
            var temp = new List<WeeklyTaskEffort>();
            if (subTasks.Count() < 1) return temp;

            // Get all the unique resources on the subtasks
            var resources = subTasks.SelectMany(x => x.AssignedResources).DistinctBy(x => x.Person.PersonId);

            // Get earliest subtask to get start date for marching
            DateTime start = subTasks.MinBy(x => x.StartDate).StartDate;

            // Move to a Monday
            start = start.AddDays(-(int)start.DayOfWeek + (int)DayOfWeek.Monday);

            // Get latest subtask finish, adding a day so the marching stops when there is no work to be done.
            DateTime end = subTasks.MaxBy(x => x.EndDate).EndDate.AddDays(1);

            // Move to the next Sunday if not already a Sunday
            if (end.DayOfWeek != DayOfWeek.Sunday)
            {
                end = end.AddDays((6 - (int)end.DayOfWeek) % 7);
            }

            // Start marching at a 1 week resolution
            DateTime startOfWeek = start;
            DateTime endOfWeek = start.AddDays(6);
            while (startOfWeek < end)
            {
                // Find subtasks that run within current week
                var within = subTasks.Where(x => x.IsWithin(startOfWeek, endOfWeek));

                // Create a new block for this week applying the value functions
                temp.Add(new WeeklyTaskEffort(startOfWeek, within, resources));

                // Increment by 1 week
                startOfWeek = startOfWeek.AddDays(7);
                endOfWeek = endOfWeek.AddDays(7);
            }
            return temp;
        }

        /// <summary>
        /// Horrible hack required to get the Y-axis sorting to work correctly on Gantt charts with multiple series
        /// by adding zero width entries to ensure both series have the same number of Y categories
        /// </summary>
        /// <param name="mixedItems"></param>
        /// <param name="defaultObjectConstructor">Function to build a default object where an empty place needs filling in the list</param>
        /// <param name="confirmedItems"></param>
        /// <param name="provisionalItems"></param>
        public static void CompleteChartSeries<T>(IEnumerable<T> mixedItems, Func<T, T> defaultObjectConstructor, out List<T> confirmedItems, out List<T> provisionalItems) where T : IChartItem
        {
            confirmedItems = new List<T>();
            provisionalItems = new List<T>();

            foreach (var c in mixedItems)
            {
                if (!c.IsHatched())
                {
                    confirmedItems.Add(c);
                    provisionalItems.Add(defaultObjectConstructor(c));
                }
                else
                {
                    confirmedItems.Add(defaultObjectConstructor(c));
                    provisionalItems.Add(c);
                }
            }
        }

        /// <summary>
        /// Generic method for filling in the gaps in chart items with value1 and value2 driven by 
        /// functions which accept the current WLM active on the day.
        /// </summary>
        /// <param name="person"></param>
        /// <param name="wlmChanges">All WLM changes for this person</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="value1FromWLMFunction"></param>
        /// <param name="value2FromWLMFunction"></param>
        /// <param name="colourFunction"></param>
        /// <param name="tooltipMessageGenerator"></param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> FillGapsBetweenChartItemsFromWorkloadModels(
            Person person,
            IEnumerable<WorkloadModelChange> wlmChanges,
            DateTime startDate,
            DateTime endDate,
            Func<WorkloadModelChange, double> value1FromWLMFunction,
            Func<WorkloadModelChange, double> value2FromWLMFunction,
            Func<double, double, bool, string> colourFunction,
            Func<Person, string> tooltipMessageGenerator = null
        )
        {
            var blocks = new List<ChartItem>();

            // Get any workload model changes in force at the beginning of, or during, the window
            var changes = wlmChanges.Where(x => x.ChangeDate < endDate).ToList();

            // If person has a leaving date in the window then set zero availability after by adding a fake change
            if (person.EndDate != null)
            {
                changes.Add(new WorkloadModelChange()
                {
                    Person = person,
                    ChangeDate = person.EndDate?.AddDays(1) ?? DateTime.Today
                });

                // Keep only changes on or before the end date
                changes = changes.Where(x => x.ChangeDate <= person.EndDate?.AddDays(1)).ToList();
            }

            // If person starts within the window but doesn't have a WLM in place on the day they start
            // set their availability to zero with a fake WLM
            if (person.StartDate > startDate && !changes.Any(x => x.ChangeDate == person.StartDate))
            {
                // Keep only changes on or after the start date
                changes = changes.Where(x => x.ChangeDate >= person.StartDate).ToList();

                // Enforce a zero availability before they start by adding a fake change
                changes.Add(new WorkloadModelChange()
                {
                    Person = person,
                    ChangeDate = startDate
                });
            }

            // Sort by date
            changes = changes.OrderBy(x => x.ChangeDate).ToList();

            // If no changes then use default value functions provide if no WLM provided to the function
            if (changes.Count == 0)
            {
                blocks.Add(
                    new ChartItem(colourFunction(value1FromWLMFunction(null), value2FromWLMFunction(null), false), person.Name, startDate, endDate,
                        value1FromWLMFunction(null), value2FromWLMFunction(null), false, tooltipMessageGenerator != null ? tooltipMessageGenerator(person) : null
                    )
                );
            }

            // Work through the workload model changes to establish blocks of availability
            else
            {
                // Establish the default values
                double value1 = value1FromWLMFunction(null);
                double value2 = value2FromWLMFunction(null);

                // Find the change immediately before the query window or on day one
                // if there is one on the first day of the query window
                var changeBefore = changes.Where(x => x.ChangeDate <= startDate).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
                if (changeBefore != null)
                {
                    value1 = value1FromWLMFunction(changeBefore);
                    value2 = value2FromWLMFunction(changeBefore);
                }

                // Any relevant changes after must be after the start of the window but before the end
                var changesAfter = changes.Where(x => x.ChangeDate > startDate && x.ChangeDate < endDate).OrderBy(x => x.ChangeDate).ToList();

                // First period uses the initial values up to the first change after the window begins or the end
                // of the window if there isn't any changes after
                blocks.Add(
                    new ChartItem(colourFunction(value1, value2, false), person.Name, startDate, changesAfter.FirstOrDefault()?.ChangeDate ?? endDate,
                        value1, value2, false, tooltipMessageGenerator != null ? tooltipMessageGenerator(person) : null
                    )
                );

                // Subsequent ones use the latest change information
                for (int i = 0; i < changesAfter.Count; ++i)
                {
                    // If the last change then use query end date for block end otherwise it is date of next change
                    blocks.Add(
                        new ChartItem(colourFunction(value1FromWLMFunction(changesAfter[i]), value2FromWLMFunction(changesAfter[i]), false), person.Name, changesAfter[i].ChangeDate,
                            i == changesAfter.Count - 1 ? endDate : changesAfter[i + 1].ChangeDate,
                            value1FromWLMFunction(changesAfter[i]), value2FromWLMFunction(changesAfter[i]), false,
                            tooltipMessageGenerator != null ? tooltipMessageGenerator(person) : null
                        )
                    );
                }
            }

            return blocks;
        }
    }
}
