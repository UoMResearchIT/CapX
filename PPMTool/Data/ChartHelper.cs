using PPMTool.Data.Entities;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Diagnostics;

namespace PPMTool.Data
{
    public class ChartHelper
    {
        /// <summary>
        /// For a given person, convert subtasks into an aggregated set of blocks for the timeline graph
        /// </summary>
        /// <param name="person">Person of interest</param>
        /// <param name="subTasks">Set of subtasks to aggregate</param>
        /// <param name="valueFunction">Function to define the primary value of a given block</param>
        /// <param name="colourFunction">Function to define the colour of a given block</param>
        /// <param name="label">Chart axis label for the data</param>
        /// <param name="startDate">Start of aggregation window</param>
        /// <param name="endDate">End of aggregation window</param>
        /// <param name="hatchedFunction">Function to determine the "hatched" state of the block</param>
        /// <param name="value2Function">Function to define the secondary value of a given block</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertSubTasksToChartItemsForPerson(
            Person person,
            IEnumerable<SubTask> subTasks,
            Func<SubTask, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<SubTask, bool> hatchedFunction = null,
            Func<double, DateTime, double> value2Function = null
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
                endDate = person.EndDate?.AddDays(1) ?? DateTime.Now.Date;
            }

            // Get the chart items
            var chartItems = AggregateSubTasksIntoBlocks(
                subTasks, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function
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
                Debug.WriteLine($"** Generating extra items at the beginning for {person.Name}");
                extraItems.AddRange(ConvertAvailabilityProfileToChartItems(person, startDate, endFill));             
            }

            // If there is a gap after the last chart item and the end date then fill in
            if (chartItems.Count() > 0 && chartItems.Last().EndDate < endDate)
            {
                Debug.WriteLine($"** Generating extra items at the end for {person.Name}");
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
                        Debug.WriteLine($"** Filling gap between {chartItems[i].EndDate} and {chartItems[i + 1].StartDate} for {person.Name}");
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
        /// For a given set of subtasks, convert subtasks into an aggregated set of blocks for the timeline graph
        /// </summary>
        /// <param name="subTasks">Set of subtasks to aggregate</param>
        /// <param name="valueFunction">Function to define the primary value of a given block</param>
        /// <param name="colourFunction">Function to define the colour of a given block</param>
        /// <param name="label">Chart axis label for the data</param>
        /// <param name="startDate">Start of aggregation window</param>
        /// <param name="endDate">End of aggregation window</param>
        /// <param name="hatchedFunction">Function to determine the "hatched" state of the block</param>
        /// <param name="value2Function">Function to define the secondary value of a given block</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> ConvertSubTasksToChartItems(
            IEnumerable<SubTask> subTasks,
            Func<SubTask, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<SubTask, bool> hatchedFunction = null,
            Func<double, DateTime, double> value2Function = null
        )
        {
            return AggregateSubTasksIntoBlocks(
                subTasks, valueFunction, colourFunction, label, startDate,
                endDate, hatchedFunction, value2Function
            ).OrderBy(x => x.StartDate).ToList();
        }

            /// <summary>
            /// Method to take the availability changes of a person and create chart items to represent "zero assignment" for the period specified
            /// </summary>
            /// <param name="person"></param>
            /// <param name="startDate"></param>
            /// <param name="endDate"></param>
            /// <returns></returns>
            private static IEnumerable<ChartItem> ConvertAvailabilityProfileToChartItems(Person person, DateTime startDate, DateTime endDate)
        {
            var blocks = new List<ChartItem>();

            // Get any availability changes in force at the beginning of the query or during it
            var changes = person.AvailabilityChanges.Where(x => x.ChangeDate < endDate).ToList();

            // Add to the changes any leaving date within the window as a zero availability
            if (person.EndDate != null)
            {
                changes.Add(new AvailabilityChange()
                {
                    Person = person,
                    ChangeDate = person.EndDate?.AddDays(1) ?? DateTime.Now.Date,
                    AvailabilityFTE = 0
                });

                // Keep only changes on or before the end date
                changes = changes.Where(x => x.ChangeDate <= person.EndDate?.AddDays(1)).ToList();
            }

            // Add to the changes any start date within the window as post FTE (if no availablity change on the start date)
            if (person.StartDate > startDate && !changes.Any(x => x.ChangeDate == person.StartDate))
            {
                changes.Add(new AvailabilityChange()
                {
                    Person = person,
                    ChangeDate = person.StartDate,
                    AvailabilityFTE = person.FTE
                });

                // Keep only changes on or after the start date
                changes = changes.Where(x => x.ChangeDate >= person.StartDate).ToList();

                // Enforce a zero availability before they start
                changes.Add(new AvailabilityChange()
                {
                    Person = person,
                    ChangeDate = startDate,
                    AvailabilityFTE = 0
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

            // Work through the availability changes to establish blocks of availability
            else
            {
                // We need to establish the availability at the beginning of the query window which will be post FTE by default
                double initialFTE = person.FTE;

                // Find the change immediately before the query window or on day one
                // if there is one on the first day of the query window
                var changeBefore = changes.Where(x => x.ChangeDate <= startDate).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
                if (changeBefore != null) initialFTE = changeBefore.AvailabilityFTE;

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
                        new ChartItem(ChartItem.GetColourStringFTE(0, changesAfter[i].AvailabilityFTE), person.Name, changesAfter[i].ChangeDate, 
                            i == changesAfter.Count - 1 ? endDate : changesAfter[i + 1].ChangeDate,
                            0, changesAfter[i].AvailabilityFTE, false
                        )
                    );
                }
            }

            return blocks;
        }

        /// <summary>
        /// Time-marching method for summing up the contribution across sub tasks based on the value function provided.
        /// The results are arranged into irregular blocks of the same continuous value.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks">Assignments to aggregate</param>
        /// <param name="valueFunction">Function used to generate the value for the block by summing the value returned by the function over the sub tasks</param>
        /// <param name="colourFunction">Function used to generate the colour for the block based on value and value2</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="hatchedFunction">Function to determine whether any of the subtasks evaluate the function to true</param>
        /// <param name="value2Function">Function used to generate a second value for the block based on the current week being examined</param>
        /// <returns></returns>
        private static IEnumerable<ChartItem> AggregateSubTasksIntoBlocks(
            IEnumerable<SubTask> subTasks,
            Func<SubTask, double> valueFunction,
            Func<double, double, string> colourFunction,
            string label,
            DateTime startDate,
            DateTime endDate,
            Func<SubTask, bool> hatchedFunction = null,
            Func<double, DateTime, double> value2Function = null
        )
        {
            // Each block is considered an element of a series.
            // We must define an element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.

            // Initialise
            var temp = new List<ChartItem>();
            
            // If no subtasks in the list
            if (subTasks.Count() < 1)
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
                var within = subTasks.Where(x => x.IsWithin(currentDay));

                // Sum value for the current day
                valueDay = within.Sum(x => valueFunction(x));

                // Set hatched for the current day
                hatchedDay = hatchedFunction != null ? within.Any(x => hatchedFunction(x)) : false;

                // Set value2 for the current day
                value2Day = value2Function != null ? value2Function(valueDay, currentDay) : 0;

                // Set colour state for the first time
                if (value2Tracked == -1d) value2Tracked= value2Day;

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
                        // Add the chart item to the results
                        temp.Add(new ChartItem(
                            colourFunction(valueTracked, value2Tracked),
                            label,
                            currentBlockStartDay,
                            currentDay,
                            valueTracked,
                            value2Tracked,
                            hatchedTracked ?? false
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
                temp.Add(new ChartItem(
                    colourFunction(valueDay, value2Day),
                    label,
                    currentBlockStartDay,
                    currentDay,
                    valueDay,
                    value2Day,
                    hatchedDay
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
        /// <param name="value1Function">Function to determine a value summed over subtasks in the current week</param>
        /// <param name="value2Function">Function to determine a second value summed over subtasks in the current week></param>
        /// <param name="hatchedFunction">Function to determine whether any of the subtasks evaluate the function to true</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateSubTasksByWeek(
            string label,
            IEnumerable<SubTask> subTasks,
            Func<SubTask, double> value1Function,
            Func<SubTask, double> value2Function = null,
            Func<SubTask, bool> hatchedFunction = null
        )
        {
            // Initialise
            var temp = new List<ChartItem>();
            if (subTasks.Count() < 1) return temp;

            // Get earliest assignment to get start date for marching
            DateTime start = subTasks.MinBy(x => x.StartDate).StartDate;

            // Get latest assignment finish so we know when to stop
            DateTime end = subTasks.MaxBy(x => x.EndDate).EndDate;

            // Start marching at a 1 week resolution
            DateTime currentWeek = start;
            while (currentWeek < end)
            {
                // Find assignments within current week
                var within = subTasks.Where(x => x.IsWithin(currentWeek));

                // Create a new block for this week applying the value functions
                temp.Add(
                    new ChartItem(
                        null,
                        label,
                        currentWeek,
                        currentWeek.AddDays(7),
                        within.Sum(x => value1Function(x)),
                        value2Function != null ? within.Sum(x => value2Function(x)) : 0,
                        hatchedFunction != null ? within.Any(x => hatchedFunction(x)) : false
                    )
                );

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }
            return temp;
        }
    }
}
