using PPMTool.Data.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PPMTool.Data
{
    public class ChartHelper
    {
        /// <summary>
        /// Time-marching method for summing up the contribution week-by-week based on the value function provided.
        /// The results are arranged into irregular blocks of the same continuous value.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks">Assignments to aggregate</param>
        /// <param name="valueFunction">Function used to generate the value for the block by summing the value returned by the function over the sub tasks</param>
        /// <param name="colourFunction">Function used to generate the colour for the block based on its value and the week beginning of the block</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="hatchedFunction">Function to determine whether any of the subtasks evaluate the function to true</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateByWeekIntoBlocks(
            IEnumerable<SubTask> subTasks,
            Func<SubTask, double> valueFunction,
            Func<double, DateTime, string> colourFunction,
            string label,
            DateTime? startDate = null,
            DateTime? endDate = null,
            Func<SubTask, bool> hatchedFunction = null
        )
        {
            // Each block for a person is considered an element of a series (block).
            // We must define an element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.

            // Initialise
            var temp = new List<ChartItem>();
            
            // If this person has no assignments return a empty list
            if (subTasks.Count() < 1)
            {
                return temp;
            }

            // Get earliest assignment to get start date for marching
            DateTime start = startDate ?? subTasks.MinBy(x => x.StartDate).StartDate;

            // Get latest assignment finish so we know when to stop
            DateTime end = endDate ?? subTasks.MaxBy(x => x.EndDate).EndDate;

            // Start marching at a 1 week resolution
            DateTime currentWeek = start;
            DateTime currentBlockStart = start;

            // Parameters used to determine when a block should be completed and a new block started
            double valueTracked = -1d;    // Initialise to something unique so we can detect the first pass through
            double valueSumWeek = 0d;
            bool? hatchedTracked = null;
            bool hatchedWeek = false;
            string colourTracked = string.Empty;
            string colourWeek = string.Empty;

            // March through 1 week at a time
            while (currentWeek < end)
            {
                // Find assignments within current week
                var within = subTasks.Where(x => x.IsWithin(currentWeek));

                // Sum value for the current week
                valueSumWeek = within.Sum(x => valueFunction(x));

                // Set hatched for the current week
                hatchedWeek = hatchedFunction != null ? within.Any(x => hatchedFunction(x)) : false;

                // Set colour for the current week
                colourWeek = colourFunction(valueSumWeek, currentWeek);

                // Set colour state for the first time
                if (colourTracked == string.Empty) colourTracked = colourWeek;

                // Set hatched state for the first time
                if (hatchedTracked == null) hatchedTracked = hatchedWeek;

                // Set the value for the first block
                if (valueTracked == -1d) valueTracked = valueSumWeek;

                // If value changed or hatched flag has changed then complete block and reset tracking params
                if (valueSumWeek != valueTracked || hatchedWeek != hatchedTracked || colourWeek != colourTracked)
                {
                    // Only add an element if the value is non-zero
                    if (valueTracked != 0d)
                    {
                        // Decide on labelling
                        temp.Add(new ChartItem(
                            colourTracked,
                            label,
                            currentBlockStart,
                            currentWeek,
                            valueTracked,
                            0,
                            hatchedTracked ?? false
                        ));
                    }
                    currentBlockStart = currentWeek;
                    valueTracked = valueSumWeek;
                    hatchedTracked = hatchedWeek;
                    colourTracked= colourWeek;
                }

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }

            // Add the final element if it had a non-zero value
            if (valueTracked != 0d)
            {
                temp.Add(new ChartItem(
                    colourWeek,
                    label,
                    currentBlockStart,
                    currentWeek,
                    valueSumWeek,
                    0,
                    hatchedWeek
                ));
            }
            return temp;
        }

        /// <summary>
        /// Time-marching method for summing up the contribution week-by-week based on the value functions provided.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks"></param>
        /// <param name="value1Function">Function to determine a value from a subtask which will be summed</param>
        /// <param name="value2Function">Function to determine a value from a subtask which will be summed></param>
        /// <param name="hatchedFunction">Function to determine whether any of the subtasks evaluate the function to true</param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateByWeek(
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

                // Create a new element for this week applying the value functions
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
