using PPMTool.Data.Entities;
using System.Collections.Generic;
using System;
using System.Linq;

namespace PPMTool.Data
{
    public class ChartHelper
    {
        /// <summary>
        /// The time-marching aggregation method which sums up the contributions to the the load each week from within the provided assignment set
        /// </summary>
        /// <param name="subTasks">Assignments to aggregate</param>
        /// <param name="valueFunction"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public static IEnumerable<ChartItem> AggregateByWeek(IEnumerable<SubTask> subTasks, Func<SubTask, double> valueFunction, string label)
        {
            // Each block for a person is considered an element of a series.
            // We must define am element as a block of the same FTE value.
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


                        temp.Add(new ChartItem(label, currentSeriesStart, currentWeek, valueCurrent));
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
                temp.Add(new ChartItem(label, currentSeriesStart, currentWeek, ValueSum));
            }
            return temp;
        }
    }
}
