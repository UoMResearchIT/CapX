using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Data
{
    public class CapacityProfile
    {
        /// <summary>
        /// Representation of a subtask with an attached project name
        /// </summary>
        public class Assignment
        {
            public SubTask SubTask { get; }

            public string ProjectName { get; }

            public Assignment(string projectName, SubTask subTask)
            {
                ProjectName = projectName;
                SubTask = subTask;
            }
        }


        public Person Person { get; }
        public IEnumerable<Assignment> Assignments { get; }

        public CapacityProfile(Person person, IEnumerable<Assignment> assignments)
        {
            Person = person;
            Assignments = assignments;
        }

        /// <summary>
        /// Method to get the week by week load as a list of capacity items for this person from all their assignments
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CapacityItem> GetWeekByWeekLoad()
        {
            // Each block for a person is considered an element of a series.
            // We must define am element as a block of the same FTE value.
            // Marching through at the chosen resolution, we can then save an element and start a new one when the FTE changes.
            var temp = new List<CapacityItem>();

            // Bail if no assignments for this resource
            if (Assignments.Count() < 1) return temp;

            // Get the list of items and return
            temp.AddRange(AggregateByWeek(Assignments));
            return temp;
        }

        /// <summary>
        /// Gets the capacity items for a particular user summing only over their individual projects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<CapacityItem> GetProjectByProjectLoad()
        {
            var temp = new List<CapacityItem>();
            if (Assignments.Count() < 1) return temp;

            // Additional loop over groups of subtask by project
            var assignmentsByProject = Assignments.GroupBy(x => x.ProjectName);
            foreach (var group in assignmentsByProject)
            {
                temp.AddRange(AggregateByWeek(group, group.Key));
            }
            return temp;
        }

        /// <summary>
        /// The time-marching aggregation method which sums up the contributions to the the load each week from within the provided assignment set
        /// </summary>
        /// <param name="assignments">Assignments to aggregate</param>
        /// <param name="projectName">Name of project to add to capacity item if assignments are of a single project</param>
        /// <returns></returns>
        public IEnumerable<CapacityItem> AggregateByWeek(IEnumerable<Assignment> assignments, string projectName = null)
        {
            // Initialise
            var temp = new List<CapacityItem>();
            if (assignments.Count() < 1) return temp;

            // Get earliest assignment to get start date for marching
            DateTime start = assignments.MinBy(x => x.SubTask.StartDate).SubTask.StartDate;

            // Get latest assignment finish so we know when to stop
            DateTime end = assignments.MaxBy(x => x.SubTask.EndDate).SubTask.EndDate;

            // Start marching at a 1 week resolution
            DateTime currentWeek = start;
            DateTime currentSeriesStart = start;
            double fteCurrent = -1d;    // Initialise to something unique so we can detect the first pass through
            double fteTotal = 0d;
            while (currentWeek < end)
            {
                // Find assignments within current week
                var within = assignments.Where(x => x.SubTask.IsWithin(currentWeek));

                // Sum FTE for the current week
                fteTotal = within.Sum(x => x.SubTask.AssignedResources.First(y => y.Person == Person).Percentage);

                // Set the value for the first element
                if (fteCurrent == -1d) fteCurrent = fteTotal;

                // If FTE changed then save and reset tracking params
                if (fteTotal != fteCurrent)
                {
                    // Only add an element if the value is non-zero
                    if (fteCurrent != 0d)
                    {
                        temp.Add(new CapacityItem(Person.Name, currentSeriesStart, currentWeek, fteCurrent, projectName));
                    }
                    currentSeriesStart = currentWeek;
                    fteCurrent = fteTotal;
                }

                // Increment by 1 week
                currentWeek = currentWeek.AddDays(7);
            }

            // Add the final element if it had a non-zero value
            if (fteCurrent != 0d)
            {
                temp.Add(new CapacityItem(Person.Name, currentSeriesStart, currentWeek, fteTotal));
            }
            return temp;
        }
    }
}
