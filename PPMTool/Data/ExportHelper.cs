using PPMTool.Data.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

namespace PPMTool.Data
{
    public class ExportHelper
    {
        /// <summary>
        /// Represents a task (row on the export sheet)
        /// </summary>
        public class TaskData
        {
            public string EmployeeNumber { get; set; }

            public string RRAR { get; set; }

            public string Domain { get; set; } = "RIT";

            public string Team { get; set; } = "Research IT/Research Software Engineering";

            [DisplayName("Full Name")]
            public string EmployeeName { get; set; }

            public double FTE { get; set; }

            public string Manager { get; set; }

            public string ProjectAndTaskName { get; set; }

            public string InnateActivity { get; set; }

            public string BaselineOrProject { get; private set; } = "Project";

            public bool GetIsBaseline()
            {
                return BaselineOrProject == "Baseline";
            }

            public void SetIsBaseline(bool value)
            {
                BaselineOrProject = value ? "Baseline" : "Project";
            }


            private Dictionary<int, int?> dataByMonth = new Dictionary<int, int?>();

            public int? GetMonthlyValue(int month)
            {
                if (dataByMonth.TryGetValue(month, out var data))
                {
                    return data;
                }
                return 0;
            }

            public void SetMonthlyValue(int month, int? value)
            {
                if (dataByMonth.ContainsKey(month))
                {
                    dataByMonth[month] = value;
                }
                else
                {
                    dataByMonth.Add(month, value);
                }
            }
        }

        /// <summary>
        /// Given a person, prepare data from database with a monthly granularity
        /// </summary>
        /// <param name="person">Person who is being exported</param>
        /// <param name="subTasks">All subtasks as retireved from the subtask service</param>
        /// <param name="projects">All projects as retireved from the project service</param>
        /// <param name="numMonthsIntoFuture">Number of months into the future we want data for</param>
        /// <returns>List of data items</returns>
        public IEnumerable<TaskData> GetExportDataForPerson(Person person, IEnumerable<SubTask> subTasks, IEnumerable<Project> projects, int numMonthsIntoFuture)
        {
            // New list
            var data = new List<TaskData>();

            // Set reference months
            var now = DateTime.Now.Date;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(numMonthsIntoFuture);
            var currentDate = startDate.Date;
            var availabilityChanges = person.AvailabilityChanges.ToList();

            // Configure a baseline task if there is an availability change in place that takes them below the post FTE
            var latestChange = availabilityChanges.Where(x => x.ChangeDate <= currentDate).OrderBy(x => x.ChangeDate).FirstOrDefault();
            if (latestChange != null && latestChange.AvailabilityFTE < person.FTE)
            {
                // Add a baseline task
                var task = new TaskData
                {
                    ProjectAndTaskName = latestChange.BaselineActivities,
                    EmployeeName = person.Name,
                    FTE = (int)(Math.Round(100 * person.FTE / 0.84)/ 100)
                };
                task.SetIsBaseline(true);
                task.SetMonthlyValue(currentDate.Month, (int)Math.Round(100 * (person.FTE - latestChange.AvailabilityFTE) / .84));
                data.Add(task);
            }

            // March forward month by month
            while (currentDate < endDate)
            {
                // Redefine availablity changes to just be those for the future
                var currentMonthAvailabilityChanges = availabilityChanges.Where(x => x.ChangeDate.Month == currentDate.Month && x.ChangeDate.Year == currentDate.Year).ToList();
                if (currentMonthAvailabilityChanges.Count > 0)
                {
                    // Get the lowest availability for the month as the focus for the month
                    var focus = currentMonthAvailabilityChanges.OrderByDescending(x => x.AvailabilityFTE).FirstOrDefault();

                    // Find existing baseline task if exists
                    var existing = data.FirstOrDefault(x => x.GetIsBaseline());
                    if (existing != null)
                    {
                        // Update description and add value for this month
                        existing.ProjectAndTaskName = focus.BaselineActivities;
                        existing.SetMonthlyValue(currentDate.Month, (int)Math.Round(100 * focus.AvailabilityFTE / .84));
                    }
                    else
                    {
                        // Add a new baseline task and value
                        var task = new TaskData
                        {
                            ProjectAndTaskName = focus.BaselineActivities,
                            EmployeeName = person.Name,
                            FTE = (int)(Math.Round(100 * person.FTE / 0.84) / 100)
                        };
                        task.SetIsBaseline(true);
                        task.SetMonthlyValue(currentDate.Month, (int)Math.Round(100 * focus.AvailabilityFTE / .84));
                        data.Add(task);
                    }
                }

                // If person hasn't started yet in this month then set value of all existing tasks for this month to blank
                if (person.EndDate != null && person.EndDate.Value.Date.Month > currentDate.Date.Month)
                {
                    foreach (var task in data)
                    {
                        task.SetMonthlyValue(currentDate.Month, null);
                    }
                }
                else
                {
                    // Find all subtasks that run in this month based on the following conditions:
                    // 1. Starts before month and finishes after month
                    // 2. Starts this month
                    // 3. Ends this month
                    var tasksThisMonth = subTasks.Where(x => 
                        (x.StartDate <= currentDate && x.EndDate >= currentDate) || 
                        (x.StartDate > currentDate && x.StartDate < currentDate.AddMonths(1)) || 
                        (x.EndDate > currentDate && x.EndDate < currentDate.AddMonths(1))
                    );

                    foreach (var t in tasksThisMonth)
                    {
                        // Build task name
                        var proj = projects.FirstOrDefault(x => x.SubTasks.Any(x => x.SubTaskId == t.SubTaskId));
                        var name = $"{proj.Name} : {t.Name}";

                        // Add / update a row for every task running in the month
                        var existing = data.FirstOrDefault(x => x.ProjectAndTaskName == name);
                        if (existing != null)
                        {
                            // Add new month entry for existing task
                            existing.SetMonthlyValue(currentDate.Month, (int)Math.Round(t.AssignedResources.First(x => x.Person == person).Percentage / .84));
                        }
                        else
                        {
                            // Add new task
                            var task = new TaskData
                            {
                                EmployeeName = person.Name,
                                FTE = (int)Math.Round(person.FTE / 0.84),
                                ProjectAndTaskName = name,
                                InnateActivity = t.InnateActivity,
                            };
                            task.SetMonthlyValue(currentDate.Month, (int)Math.Round(t.AssignedResources.First(x => x.Person == person).Percentage / .84));
                            data.Add(task);
                        }
                    }
                }
                currentDate = currentDate.AddMonths(1).Date;
            }

            Debug.WriteLine($"Exported {data.Count} rows for {person.Name}");
            return data;
            
        }

    }
}
