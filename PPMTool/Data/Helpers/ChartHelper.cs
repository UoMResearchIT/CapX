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
        /// <param name="person">Person of interest</param>
        /// <param name="assignments">Set of assignments to aggregate</param>
        /// <param name="valueFunction">Function to define the primary value of a given block</param>
        /// <param name="colourFunction">Function to define the colour of a given block</param>
        /// <param name="label">Chart axis label for the data</param>
        /// <param name="startDate">Start of aggregation window</param>
        /// <param name="endDate">End of aggregation window</param>
        /// <param name="hatchedFunction">Function to determine the "hatched" state of the block</param>
        /// <param name="value2Function">Function to define the secondary value of a given block</param>
        /// <param name="gapFillingFunction">Function that fills gaps in the chart items</param>
        /// <param name="tooltipMessageFormatter">Function to provide HTML string to be shown as tooltip messages for block based on list of assignments that fall within the block</param>
        /// <param name="ignoreZeroValue1Entries">If true, does not create a block if it has a value of 0 for value 1, leaving a gap</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertAssignmentsToChartItemsForPerson(
            Person person,
            IEnumerable<BaseAssignment> assignments,
            Func<IEnumerable<BaseAssignment>, DateTime, double> valueFunction,
            Func<double, double, bool, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<IEnumerable<BaseAssignment>, bool> hatchedFunction = null,
            Func<IEnumerable<BaseAssignment>, double, DateTime, double> value2Function = null,
            Func<Person, DateTime, DateTime, IEnumerable<ChartItem>> gapFillingFunction = null,
            Func<IEnumerable<BaseAssignment>, string> tooltipMessageFormatter = null,
            bool ignoreZeroValue1Entries = false
        )
        {
            // If person starts after the start date then reset the start date to that date
            if (person.StartDate > startDate)
            {
                startDate = person.StartDate;
            }

            // If person leaves before the end date then reset the end date to that date
            if (person.EndDate != null && person.EndDate < endDate)
            {
                endDate = person.EndDate?.AddDays(1) ?? DateTime.Today;
            }

            // Get the chart items
            var chartItems = AggregateAssignmentsIntoBlocks(
                assignments, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function, gapFillingFunction, tooltipMessageFormatter,
                ignoreZeroValue1Entries
            ).OrderBy(x => x.StartDate).ToList();
            Debug.WriteLine($"** Generated {chartItems.Count} block(s) for {person.Name}");

            if (gapFillingFunction != null)
            {
                // Create an empty list
                var extraItems = new List<ChartItem>();

                // If no items or if the first chart item starts after the (corrected) start date
                // then fill in gaps
                if (chartItems.Count() < 1 || chartItems.First().StartDate > startDate)
                {
                    // Define fill region end date
                    var endFill = chartItems.Count() < 1 ? endDate : chartItems.First().StartDate;

                    // Generate the items
                    extraItems.AddRange(gapFillingFunction(person, startDate, endFill));
                }

                // If there is a gap after the last chart item and the end date then fill in
                if (chartItems.Count() > 0 && chartItems.Last().EndDate < endDate)
                {
                    extraItems.AddRange(gapFillingFunction(person, chartItems.Last().EndDate, endDate));
                }

                // Add the extra items to the chart data
                if (extraItems.Count > 0)
                {
                    // Add the items to the chart items list and reorder
                    chartItems.AddRange(extraItems);
                    chartItems = chartItems.OrderBy(x => x.StartDate).ToList();
                }

                // If there are any gaps in the chart items where they are free then fill in
                extraItems.Clear();
                for (int i = 0; i < chartItems.Count(); ++i)
                {
                    // Ignore the last item in the list
                    if (i < chartItems.Count() - 1)
                    {
                        // If there is a gap
                        if (chartItems[i].EndDate != chartItems[i + 1].StartDate)
                        {
                            // Generate chart items from availability to fill the gap
                            extraItems.AddRange(gapFillingFunction(person, chartItems[i].EndDate, chartItems[i + 1].StartDate));
                        }
                    }
                }

                // Add the extra items to the chart data
                if (extraItems.Count > 0)
                {
                    // Add the items to the chart items list and reorder
                    chartItems.AddRange(extraItems);
                    chartItems = chartItems.OrderBy(x => x.StartDate).ToList();
                }
            }

            return chartItems;
        }

        /// <summary>
        /// For a given set of assignments, convert into an aggregated set of blocks for the timeline graph.
        /// </summary>
        /// <param name="assignments">Set of assignments to aggregate</param>
        /// <param name="valueFunction">Function to define the primary value of a given block</param>
        /// <param name="colourFunction">Function to define the colour of a given block</param>
        /// <param name="label">Chart axis label for the data</param>
        /// <param name="startDate">Start of aggregation window</param>
        /// <param name="endDate">End of aggregation window</param>
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
            Func<IEnumerable<BaseAssignment>, bool> hatchedFunction = null,
            Func<IEnumerable<BaseAssignment>, double, DateTime, double> value2Function = null,
            Func<Person, DateTime, DateTime, IEnumerable<ChartItem>> gapFillingFunction = null,
            Func<IEnumerable<BaseAssignment>, string> tooltipMessageFormatter = null,
            bool ignoreZeroValue1Entries = false
        )
        {
            return AggregateAssignmentsIntoBlocks(
                assignments, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function, gapFillingFunction, tooltipMessageFormatter,
                ignoreZeroValue1Entries
            ).OrderBy(x => x.StartDate).ToList();
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
        /// <param name="gapFillingFunction">Function that fills gaps in the chart items</param>
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
            Func<Person, DateTime, DateTime, IEnumerable<ChartItem>> gapFillingFunction = null,
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
        /// Time-marching method for summing up the contribution week-by-week based on the value functions provided.
        /// The results are arranged into blocks exactly one week in length.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks"></param>
        /// <param name="value1Function">Function to determine a value for subtasks in the current week</param>
        /// <param name="value2Function">Function to determine a second value for subtasks in the current week></param>
        /// <param name="hatchedFunction">Function to determine hatched status for subtasks in the current week</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateSubTasksByWeek(
            string label,
            IEnumerable<SubTask> subTasks,
            Func<IEnumerable<SubTask>, DateTime, double> value1Function,
            Func<IEnumerable<SubTask>, DateTime, double> value2Function = null,
            Func<IEnumerable<SubTask>, bool> hatchedFunction = null
        )
        {
            // Initialise
            var temp = new List<ChartItem>();
            if (subTasks.Count() < 1) return temp;

            // Get earliest assignment to get start date for marching
            DateTime start = subTasks.MinBy(x => x.StartDate).StartDate;

            // Move to a Monday
            start = start.AddDays(-(int)start.DayOfWeek + (int)DayOfWeek.Monday);

            // Get latest assignment finish, adding a day so it is the first day when no work will be done.
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
                // Find assignments that run within current week
                var within = subTasks.Where(x => x.IsWithin(startOfWeek, endOfWeek));

                // Create a new block for this week applying the value functions
                temp.Add(
                    new ChartItem(
                        null,
                        label,
                        startOfWeek,
                        endOfWeek,
                        value1Function(within, startOfWeek),
                        value2Function != null ? value2Function(within, startOfWeek) : 0,
                        hatchedFunction != null ? hatchedFunction(within) : false
                    )
                );

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
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="value1FromWLMFunction"></param>
        /// <param name="value2FromWLMFunction"></param>
        /// <param name="colourFunction"></param>
        /// <param name="tooltipMessageGenerator"></param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> FillGapsBetweenChartItemsFromWorkloadModels(
            Person person,
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
            var changes = person.WorkloadModelChanges.Where(x => x.ChangeDate < endDate).ToList();

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
