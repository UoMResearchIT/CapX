using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    public class CapacityProfile
    {
        public Person Person { get; }
        public IEnumerable<SubTask> Assignments { get; }

        public CapacityProfile(Person person, IEnumerable<SubTask> assignments)
        {
            Person = person;
            Assignments = assignments;
        }

        /// <summary>
        /// Method to get the week by week load as a list of capacity items for this person from their assignments
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CapacityItem> GetWeekByWeekLoad()
        {
            // Each block for a person is considered a series.
            // We must define a series as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save a series and start a new one when the FTE changes.
            var temp = new List<CapacityItem>();

            // Bail if no assignments for this resource
            if (Assignments.Count() < 1) return temp;

            // Get earliest assignment to get start date for marching
            DateTime start = Assignments.MinBy(x => x.StartDate).StartDate;

            // Get latest assignment finish so we know when to stop
            DateTime end = Assignments.MaxBy(x => x.EndDate).EndDate;

            // Start marching at a 1 week resolution
            DateTime currentWeek = start;
            DateTime currentSeriesStart = start;
            double fteCurrent = -1d;    // Initialise to something unique so we can detect the first pass through
            double fteTotal = 0d;
            while (currentWeek < end)
            {
                // Find assignments within current week
                var within = Assignments.Where(x => x.IsWithin(currentWeek));

                // Sum FTE for the current week
                fteTotal = within.Sum(x => x.AssignedResources.First(y => y.Person == Person).Percentage);

                // Set the value for the first series
                if (fteCurrent == -1d) fteCurrent = fteTotal;

                // If FTE changed then save and reset tracking params
                if (fteTotal != fteCurrent)
                {
                    // Only add a series if the value is non-zero
                    if (fteCurrent != 0d)
                    {
                        temp.Add(new CapacityItem(Person.Name, currentSeriesStart, currentWeek, fteCurrent));
                    }
                    currentSeriesStart = currentWeek;
                    fteCurrent = fteTotal;
                }

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }

            // Add the final series if it had a non-zero value
            if (fteCurrent != 0d)
            {
                temp.Add(new CapacityItem(Person.Name, currentSeriesStart, currentWeek, fteTotal));
            }

            return temp;
        }
    }
}
