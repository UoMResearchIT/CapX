using PPMTool.Data.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PPMTool.Data
{
    public class ChartHelper
    {
        public static IEnumerable<ChartItem> ConvertSubTasksToChartDataForPerson(
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
            // TODO: If person starts after the start date then reset the start date to that date

            // TODO: If person leaves before the end date then reset the end date to that date

            // TODO: Get the chart items

            // TODO: If the first chart item starts after the (correct) start date then fill in with "zero items" based on availability profile

            // TODO: If there are any gaps in the chart items where they are free then fill in

            // TODO: If there is a gap after the last chart item and the end date then fill in
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
            // Each block for a person is considered an element of a series.
            // We must define an element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.

            // Initialise
            var temp = new List<ChartItem>();
            
            // If this person has no assignments then return "zero blocks" based on availability
            if (subTasks.Count() < 1)
            {
                // Return empty list
                return temp;
            }

            // Start marching at a 1 week resolution
            DateTime currentWeek = startDate;
            DateTime currentBlockStart = startDate;

            // Parameters used to determine when a block should be completed and a new block started
            // Initialise tracked values to something unique so we can detect the first pass through
            double valueTracked = -1d;
            double valueWeek = 0d;
            bool? hatchedTracked = null;
            bool hatchedWeek = false;
            double value2Tracked = -1d;
            double value2Week = 0d;

            // March through 1 week at a time
            while (currentWeek < endDate)
            {
                // Find assignments within current week
                var within = subTasks.Where(x => x.IsWithin(currentWeek));

                // Sum value for the current week
                valueWeek = within.Sum(x => valueFunction(x));

                // Set hatched for the current week
                hatchedWeek = hatchedFunction != null ? within.Any(x => hatchedFunction(x)) : false;

                // Set value2 for the current week
                value2Week = value2Function != null ? value2Function(valueWeek, currentWeek) : 0;

                // Set colour state for the first time
                if (value2Tracked == -1d) value2Tracked= value2Week;

                // Set hatched state for the first time
                if (hatchedTracked == null) hatchedTracked = hatchedWeek;

                // Set the value for the first block
                if (valueTracked == -1d) valueTracked = valueWeek;

                // If any of the tracked parameters have changed then complete block and reset tracking params
                if (valueWeek != valueTracked || hatchedWeek != hatchedTracked || value2Week != value2Tracked)
                {
                    // Only add a block if its value is non-zero
                    if (valueTracked != 0d)
                    {
                        // Add the chart item to the results
                        temp.Add(new ChartItem(
                            colourFunction(valueTracked, value2Tracked),
                            label,
                            currentBlockStart,
                            currentWeek,
                            valueTracked,
                            value2Tracked,
                            hatchedTracked ?? false
                        ));
                    }
                    currentBlockStart = currentWeek;
                    valueTracked = valueWeek;
                    hatchedTracked = hatchedWeek;
                    value2Tracked = value2Week;
                }

                // Increment by 1 day
                currentWeek = currentWeek.AddDays(1);
            }

            // Add the final block if it had a non-zero value
            if (valueTracked != 0d)
            {
                temp.Add(new ChartItem(
                    colourFunction(valueWeek, value2Week),
                    label,
                    currentBlockStart,
                    currentWeek,
                    valueWeek,
                    value2Week,
                    hatchedWeek
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
