using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using DotNetExtensions;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    public static class ExportHelper
    {
        /// <summary>
        /// Represents a task (row on the export sheet)
        /// </summary>
        public class TaskData
        {
            [DisplayName("Name")]
            public string EmployeeName { get; set; }

            public string Grade { get; set; }

            public string Project { get; set; }

            public string Task { get; set; }

            public string PI { get; set; }

            public string Faculty { get; set; }

            public string School { get; set; }

            [DisplayName("Salary Cost Estimate")]
            public string SalaryCostEstimate { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

        }

        /// <summary>
        /// Given a person, prepare data from database with a weekly granularity
        /// </summary>
        /// <param name="person">Person who is being exported</param>
        /// <param name="subTasks">All subtasks where person is an assigned resource</param>
        /// <param name="projects">All projects as retrieved from the project service</param>
        /// <param name="numMonthsIntoFuture">Number of months into the future we want data for</param>
        /// <returns>List of data items</returns>
        public static IEnumerable<TaskData> GetExportDataForPerson(Person person, IEnumerable<Project> projects, DateTime startDate, DateTime endDate)
        {
            // New list
            var data = new List<TaskData>();

            // Filter list of tasks to those running during the window
            var tasksInWindow = projects
                .SelectMany(x => x.SubTasks)
                .Where(x => x.AssignedResources
                .Any(x => x.Person.PersonId == person.PersonId))
                .Where(x => x.IsWithin(startDate, endDate));

            // Get WLM changes for this person that take place during the window
            var wlms = person.WorkloadModelChanges.Where(x => x.ChangeDate >= startDate && x.ChangeDate <= endDate).OrderByDescending(x => x.ChangeDate);

            // Set default WLM to be G6
            WorkloadModelChange defaultWLM = new WorkloadModelChange()
            {
                Person = person,
                ChangeDate = startDate,
                Grade = 6
            };

            // If they started before the window, get the WLM from before and overwrite default settings
            if (person.StartDate < startDate)
            {
                var tempWlm = person.WorkloadModelChanges.Where(x => x.ChangeDate < startDate).OrderBy(x => x.ChangeDate).LastOrDefault();
                if (tempWlm != null)
                {
                    defaultWLM.Grade = tempWlm.Grade;
                }
            }

            // Add the start WLM to the list of WLMs active in the window
            wlms.Append(defaultWLM);

            // Are there any changes in grade for this person?
            var changesInGrade = wlms.DistinctBy(x => x.Grade).Count() > 1;

            // Are there any changes in financial year in the window?
            var startFY = FinancialReference.GetFinancialYear(startDate);
            var endFY = FinancialReference.GetFinancialYear(endDate);
            var changesInFinancialYear = startFY != endFY;

            // Each assignment is at least one row of the report
            foreach (var task in tasksInWindow)
            {
                var project = projects.FirstOrDefault(x => x.SubTasks.Any(x => x.SubTaskId == task.SubTaskId));

                IEnumerable<TaskData> taskChunks = new List<TaskData>()
                {
                    new TaskData
                    {
                        EmployeeName = person.Name,
                        Project = project.Name,
                        Faculty = project.Faculty.GetDescription(),
                        School = project.School.GetDescription(),
                        PI = project.PI,
                        Task = task.Name,
                        StartDate = task.StartDate,
                        EndDate = task.EndDate
                    }
                };

                // Are there any changes to grade for this person at all
                if (changesInGrade)
                {
                    // TODO: Split on grade changes

                }

                // Are there any financial year changes within the window
                if (changesInFinancialYear)
                {
                    // TODO: Split the task if it crosses a financial year boundary
                }

                // For each task chunk
                foreach (var chunk in taskChunks)
                {
                    // Truncate dates if extends beyond window or person's start and end dates
                    if (chunk.StartDate < startDate)
                    {
                        chunk.StartDate = startDate;
                    }
                    if (chunk.EndDate > endDate)
                    {
                        chunk.EndDate = endDate;
                    }

                    // Get the WLM active during the chunk


                    // Get the financial reference

                    // Compute grade and cost and update


                }

                // Add task to master list
                data.AddRange(taskChunks);
            }

            Debug.WriteLine($"** Exported {data.Count} rows for {person.Name}");
            return data;
        }
    }
}
