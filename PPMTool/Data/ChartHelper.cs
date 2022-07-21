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
        /// The results are arragned into irregular blocks of the same contrinuous value.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks">Assignments to aggregate</param>
        /// <param name="valueFunction"></param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateByWeekIntoBlocks(IEnumerable<SubTask> subTasks, Func<SubTask, double> valueFunction, string label)
        {
            // Each block for a person is considered an element of a series (block).
            // We must define an element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.

            // Initialise
            var temp = new List<ChartItem>();
            if (subTasks.Count() < 1) return temp;

            // Get earliest assignment to get start date for marching
            DateTime start = subTasks.MinBy(x => x.StartDate).StartDate;

            // Get latest assignment finish so we know when to stop
            DateTime end = subTasks.MaxBy(x => x.EndDate).EndDate;

            // Start marching at a 1 week resolution
            DateTime currentWeek = start;
            DateTime currentSeriesStart = start;
            double valueCurrent = -1d;    // Initialise to something unique so we can detect the first pass through
            double ValueSum = 0d;
            while (currentWeek < end)
            {
                // Find assignments within current week
                var within = subTasks.Where(x => x.IsWithin(currentWeek));

                // Sum value for the current week
                ValueSum = within.Sum(x => valueFunction(x));

                // Set the value for the first element
                if (valueCurrent == -1d) valueCurrent = ValueSum;

                // If value changed then save and reset tracking params
                if (ValueSum != valueCurrent)
                {
                    // Only add an element if the value is non-zero
                    if (valueCurrent != 0d)
                    {
                        // Decide on labelling
                        temp.Add(new ChartItem(label, currentSeriesStart, currentWeek, valueCurrent, 0));
                    }
                    currentSeriesStart = currentWeek;
                    valueCurrent = ValueSum;
                }

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }

            // Add the final element if it had a non-zero value
            if (valueCurrent != 0d)
            {
                temp.Add(new ChartItem(label, currentSeriesStart, currentWeek, ValueSum, 0));
            }
            return temp;
        }

        /// <summary>
        /// Time-marching method for summing up the contribution week-by-week based on the value functions provided.
        /// </summary>
        /// <param name="label"></param>
        /// <param name="subTasks"></param>
        /// <param name="value1Function"></param>
        /// <param name="value2Function"></param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateByWeek(string label, IEnumerable<SubTask> subTasks, Func<SubTask, double> value1Function, Func<SubTask, double> value2Function = null)
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
                        label,
                        currentWeek,
                        currentWeek.AddDays(7),
                        within.Sum(x => value1Function(x)),
                        value2Function != null ? within.Sum(x => value2Function(x)) : 0
                    )
                );

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }
            return temp;
        }
    }
}
