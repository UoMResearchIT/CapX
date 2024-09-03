using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using PPMTool.Data.Entities;

namespace PPMTool.Data
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
        /// <param name="tooltipMessageFormatter">Function to provide HTML string to be shown as tooltip messages for block based on list of assignments that fall within the block</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertAssignmentsToChartItemsForPerson(
            Person person,
            IEnumerable<Assignment> assignments,
            Func<IEnumerable<Assignment>, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<IEnumerable<Assignment>, bool> hatchedFunction = null,
            Func<IEnumerable<Assignment>, double, DateTime, double> value2Function = null,
            Func<IEnumerable<Assignment>, string> tooltipMessageFormatter = null
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
                endDate, hatchedFunction, value2Function, tooltipMessageFormatter
            ).OrderBy(x => x.StartDate).ToList();
            Debug.WriteLine($"** Generated {chartItems.Count} block(s) for {person.Name}");

            // Create an empty list
            var extraItems = new List<ChartItem>();

            // If no items or if the first chart item starts after the (corrected) start date
            // then fill in with "zero items" based on availability profile
            if (chartItems.Count() < 1 || chartItems.First().StartDate > startDate)
            {
                // Define fill region end date
                var endFill = chartItems.Count() < 1 ? endDate : chartItems.First().StartDate;

                // Generate the items
                extraItems.AddRange(ConvertAvailabilityProfileToChartItems(person, startDate, endFill));
            }

            // If there is a gap after the last chart item and the end date then fill in
            if (chartItems.Count() > 0 && chartItems.Last().EndDate < endDate)
            {
                extraItems.AddRange(ConvertAvailabilityProfileToChartItems(person, chartItems.Last().EndDate, endDate));
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
                        extraItems.AddRange(ConvertAvailabilityProfileToChartItems(person, chartItems[i].EndDate, chartItems[i + 1].StartDate));
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
        /// <param name="tooltipMessageFormatter">Function to provide HTML string to be shown as tooltip messages for block based on list of assignments that fall within the block</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertAssignmentsToChartItems(
            IEnumerable<Assignment> assignments,
            Func<IEnumerable<Assignment>, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<IEnumerable<Assignment>, bool> hatchedFunction = null,
            Func<IEnumerable<Assignment>, double, DateTime, double> value2Function = null,
            Func<IEnumerable<Assignment>, string> tooltipMessageFormatter = null
        )
        {
            return AggregateAssignmentsIntoBlocks(
                assignments, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function, tooltipMessageFormatter
            ).OrderBy(x => x.StartDate).ToList();
        }

        /// <summary>
        /// Method to take the workload model changes of a person and create chart items to represent "zero assignment" for the period specified.
        /// </summary>
        /// <param name="person"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        private static IEnumerable<ChartItem> ConvertAvailabilityProfileToChartItems(Person person, DateTime startDate, DateTime endDate)
        {
            var blocks = new List<ChartItem>();

            // Get any workload model changes in force at the beginning of the query or during it
            var changes = person.WorkloadModelChanges.Where(x => x.ChangeDate < endDate).ToList();

            // Add to the changes any leaving date within the window as a zero availability
            if (person.EndDate != null)
            {
                changes.Add(new WorkloadModelChange()
                {
                    Person = person,
                    ChangeDate = person.EndDate?.AddDays(1) ?? DateTime.Today,
                    ProjectWorkFTE = 0
                });

                // Keep only changes on or before the end date
                changes = changes.Where(x => x.ChangeDate <= person.EndDate?.AddDays(1)).ToList();
            }

            // Add to the changes any start date within the window as post FTE (if no availablity change on the start date)
            if (person.StartDate > startDate && !changes.Any(x => x.ChangeDate == person.StartDate))
            {
                changes.Add(new WorkloadModelChange()
                {
                    Person = person,
                    ChangeDate = person.StartDate,
                    ProjectWorkFTE = person.FTE
                });

                // Keep only changes on or after the start date
                changes = changes.Where(x => x.ChangeDate >= person.StartDate).ToList();

                // Enforce a zero availability before they start
                changes.Add(new WorkloadModelChange()
                {
                    Person = person,
                    ChangeDate = startDate,
                    ProjectWorkFTE = 0
                });
            }

            // Sort by date
            changes = changes.OrderBy(x => x.ChangeDate).ToList();

            // If no changes then use post FTE in a single block
            if (changes.Count == 0)
            {
                blocks.Add(
                    new ChartItem(ChartItem.GetColourStringFTE(0, person.FTE), person.Name, startDate, endDate,
                        0, person.FTE, false
                    )
                );
            }

            // Work through the workload model changes to establish blocks of availability
            else
            {
                // We need to establish the availability at the beginning of the query window which will be post FTE by default
                double initialFTE = person.FTE;

                // Find the change immediately before the query window or on day one
                // if there is one on the first day of the query window
                var changeBefore = changes.Where(x => x.ChangeDate <= startDate).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
                if (changeBefore != null) initialFTE = changeBefore.ProjectWorkFTE;

                // Any relevant changes after must be after the start of the window but before the end
                var changesAfter = changes.Where(x => x.ChangeDate > startDate && x.ChangeDate < endDate).OrderBy(x => x.ChangeDate).ToList();

                // First period uses the initial FTE up to the first change after the window begins or the end
                // of the window if there isn't any changes after
                blocks.Add(
                    new ChartItem(ChartItem.GetColourStringFTE(0, initialFTE), person.Name, startDate, changesAfter.FirstOrDefault()?.ChangeDate ?? endDate,
                        0, initialFTE, false
                    )
                );

                // Subsequent ones use the latest change information
                for (int i = 0; i < changesAfter.Count; ++i)
                {
                    // If the last change then use query end date for block end otherwise it is date of next change
                    blocks.Add(
                        new ChartItem(ChartItem.GetColourStringFTE(0, changesAfter[i].ProjectWorkFTE), person.Name, changesAfter[i].ChangeDate,
                            i == changesAfter.Count - 1 ? endDate : changesAfter[i + 1].ChangeDate,
                            0, changesAfter[i].ProjectWorkFTE, false
                        )
                    );
                }
            }

            return blocks;
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
        /// <returns></returns>
        private static IEnumerable<ChartItem> AggregateAssignmentsIntoBlocks(
            IEnumerable<Assignment> assignments,
            Func<IEnumerable<Assignment>, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<IEnumerable<Assignment>, bool> hatchedFunction = null,
            Func<IEnumerable<Assignment>, double, DateTime, double> value2Function = null,
            Func<IEnumerable<Assignment>, string> tooltipMessageFormatter = null
        )
        {
            // Each block is considered an element of a series.
            // We must define an element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.

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
            double valueTracked = -1d;
            double valueDay = 0d;
            bool? hatchedTracked = null;
            bool hatchedDay = false;
            double value2Tracked = -1d;
            double value2Day = 0d;

            // March through
            while (currentDay < endDate)
            {
                // Find assignments running on current day
                var within = assignments.Where(x => x.SubTask.IsWithin(currentDay));

                // Sum value for the current day -- truncate to 2 DP
                valueDay = valueFunction(within);

                // Set hatched for the current day
                hatchedDay = hatchedFunction != null ? hatchedFunction(within) : false;

                // Set value2 for the current day
                value2Day = value2Function != null ? value2Function(within, valueDay, currentDay) : 0;

                // Set colour state for the first time
                if (value2Tracked == -1d) value2Tracked = value2Day;

                // Set hatched state for the first time
                if (hatchedTracked == null) hatchedTracked = hatchedDay;

                // Set the value for the first block
                if (valueTracked == -1d) valueTracked = valueDay;

                // If any of the tracked parameters have changed then complete block and reset tracking params
                if (valueDay != valueTracked || hatchedDay != hatchedTracked || value2Day != value2Tracked)
                {
                    // Only add a block if its value is non-zero
                    if (valueTracked != 0d)
                    {
                        var assignmentsInBlock = assignments.Where(x => x.SubTask.IsWithin(currentBlockStartDay, currentDay.AddDays(-1)));
                        // Add the chart item to the results
                        temp.Add(new ChartItem(
                            colourFunction(valueTracked, value2Tracked),
                            label,
                            currentBlockStartDay,
                            currentDay,
                            valueTracked,
                            value2Tracked,
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
            if (valueTracked != 0d)
            {
                // Consider the end date to be inclusive of the final block so do not move back a day like above
                var assignmentsInBlock = assignments.Where(x => x.SubTask.IsWithin(currentBlockStartDay, currentDay));
                temp.Add(new ChartItem(
                    colourFunction(valueDay, value2Day),
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
    }
}
