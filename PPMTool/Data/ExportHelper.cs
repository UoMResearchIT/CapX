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
        /// Represents a chunk of an assignemnt with a constant grade and financial year
        /// </summary>
        public class AssignmentChunk
        {
            [DisplayName("Name")]
            public string EmployeeName { get; set; }

            public int Grade { get; set; }

            public double FTE { get; set; }

            public string Project { get; set; }

            public string Task { get; set; }

            public string PI { get; set; }

            public string Faculty { get; set; }

            public string School { get; set; }

            [DisplayName("Salary Cost Estimate")]
            public double SalaryCostEstimate { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime EndDate { get; set; }

            public int FinancialYear { get; set; }

            public AssignmentChunk()
            {

            }

            public AssignmentChunk(AssignmentChunk taskToCopy)
            {
                EmployeeName = taskToCopy.EmployeeName;
                Grade = taskToCopy.Grade;
                FTE = taskToCopy.FTE;
                Project = taskToCopy.Project;
                Faculty = taskToCopy.Faculty;
                School = taskToCopy.School;
                PI = taskToCopy.PI;
                Task = taskToCopy.Task;
                StartDate = new DateTime(taskToCopy.StartDate.Ticks);
                EndDate = new DateTime(taskToCopy.EndDate.Ticks);
                FinancialYear = taskToCopy.FinancialYear;
            }

        }

        /// <summary>
        /// Given a person, prepare data from database with a weekly granularity
        /// </summary>
        /// <param name="person">Person who is being exported</param>
        /// <param name="projects">All projects as retrieved from the project service</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="finrefs">All the financial references</param>
        /// <returns>List of data items</returns>
        public static IEnumerable<AssignmentChunk> GetExportDataForPerson(Person person, IEnumerable<Project> projects, DateTime startDate, DateTime endDate, IEnumerable<FinancialReference> finrefs)
        {
            // New list
            var data = new List<AssignmentChunk>();

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
                var tempWlm = person.GetWorkloadModelOnDateOrDefault(startDate);
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
                IList<AssignmentChunk> taskChunks = new List<AssignmentChunk>();
                var initialChunk = new AssignmentChunk
                {
                    EmployeeName = person.Name,
                    Grade = defaultWLM.Grade,
                    FTE = task.AssignedResources.FirstOrDefault(x => x.Person.PersonId == person.PersonId).AssignmentFTE,
                    Project = project.Name,
                    Faculty = project.Faculty.GetDescription(),
                    School = project.School.GetDescription(),
                    PI = project.PI,
                    Task = task.Name,
                    StartDate = task.StartDate,
                    EndDate = task.EndDate,
                    FinancialYear = FinancialReference.GetFinancialYear(task.StartDate)
                };

                // Are there any changes to grade for this person at all
                if (changesInGrade)
                {
                    // Find changes that are within the chunk
                    var changes = wlms.Where(x => x.ChangeDate > initialChunk.StartDate && x.ChangeDate <= initialChunk.EndDate);

                    foreach (var change in changes)
                    {
                        // Get previous by looking at day before change
                        var wlmBefore = person.GetWorkloadModelOnDateOrDefault(change.ChangeDate.AddDays(-1));

                        // Define a new task chunk for before period if necessary
                        if (wlmBefore.Grade != change.Grade)
                        {
                            taskChunks.Add(new AssignmentChunk(initialChunk)
                            {
                                EndDate = change.ChangeDate.AddDays(-1)
                            });
                        }
                    }

                    // If we did a split then need to add the final task chunk
                    if (taskChunks.Count > 0)
                    {
                        taskChunks.Add(new AssignmentChunk(initialChunk)
                        {
                            StartDate = new DateTime(taskChunks.Last().EndDate.AddDays(1).Ticks),
                            EndDate = new DateTime(initialChunk.EndDate.Ticks)
                        });
                    }

                    // If not then the initial chunk remains the only chunk
                    else
                    {
                        taskChunks.Add(initialChunk);
                    }
                }

                Debug.WriteLine($"** {project.GetFullName} => {task.Name} | {taskChunks.Count} chunks after Grade splitting");

                // Are there any financial year changes within the window
                if (changesInFinancialYear)
                {
                    var tempChunks = new List<AssignmentChunk>();
                    // Loop over chunks and see if a financial year change lands in the middle
                    foreach (var chunk in taskChunks)
                    {
                        // Get financial year the chunk starts and finishes in
                        var fyStart = FinancialReference.GetFinancialYear(chunk.StartDate);
                        var fyEnd = FinancialReference.GetFinancialYear(chunk.EndDate);

                        if (fyStart != fyEnd)
                        {
                            // For each financial year falling within the task, add chunks
                            for (var i = fyStart; i <= fyEnd; i++)
                            {
                                tempChunks.Add(new AssignmentChunk(chunk)
                                {
                                    StartDate = fyStart == i ? chunk.StartDate : new DateTime(i, 8, 1),
                                    EndDate = fyEnd == i ? chunk.EndDate : new DateTime(i, 7, 31),
                                    FinancialYear = i
                                });
                            }
                        }
                        else
                        {
                            tempChunks.Add(chunk);
                        }
                    }

                    // Replace the list with the new list of chunks
                    taskChunks = tempChunks;
                }

                Debug.WriteLine($"** {project.GetFullName} => {task.Name} | {taskChunks.Count} chunks after FY splitting");

                // Filter task chunk list to just those that intersect the window
                taskChunks = taskChunks.Where(x => x.StartDate <= endDate && x.EndDate >= startDate).ToList();

                Debug.WriteLine($"** {project.GetFullName} => {task.Name} | {taskChunks.Count} chunks run during the window");

                // Update the data for the filtered chunks
                foreach (var chunk in taskChunks)
                {
                    // Truncate dates if extends beyond window
                    if (chunk.StartDate < startDate)
                    {
                        chunk.StartDate = startDate;
                    }
                    if (chunk.EndDate > endDate)
                    {
                        chunk.EndDate = endDate;
                    }

                    // Compute cost estimate
                    var annualCosts = finrefs.GetSuitableFinancialReference(chunk.FinancialYear).GetMidGradeCosts(chunk.Grade);
                    var fractionOfYear = chunk.EndDate.Date.Subtract(chunk.StartDate).TotalDays / 365d;
                    chunk.SalaryCostEstimate = annualCosts * chunk.FTE * fractionOfYear;
                }

                // Add task to master list
                data.AddRange(taskChunks);
            }

            Debug.WriteLine($"** Built {data.Count} rows for {person.Name}");
            return data;
        }
    }
}
