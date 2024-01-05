using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using FluentDateTime;
using PPMTool.Data.Entities;

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


            private Dictionary<string, int?> dataByMonth = new Dictionary<string, int?>();

            public int? GetMonthlyValue(int month, int year)
            {
                var key = EncodeKey(month, year);
                if (dataByMonth.TryGetValue(key, out var data))
                {
                    return data;
                }
                return 0;
            }

            public void SetMonthlyValue(int month, int year, int? value)
            {
                var key = EncodeKey(month, year);
                if (dataByMonth.ContainsKey(key))
                {
                    dataByMonth[key] = value;
                }
                else
                {
                    dataByMonth.Add(key, value);
                }
            }

            private string EncodeKey(int month, int year)
            {
                return $"{month}-{year}";
            }
        }

        /// <summary>
        /// Test to see whether a date is within a 
        /// </summary>
        /// <param name="dateToTest"></param>
        /// <param name="currentMonth"></param>
        /// <returns></returns>
        private bool IsWithinMonth(DateTime dateToTest, DateTime currentMonth)
        {
            return dateToTest.Date >= currentMonth.BeginningOfMonth().Date && dateToTest.Date <= currentMonth.EndOfMonth().Date;
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
            var latestChange = availabilityChanges.Where(x => x.ChangeDate <= currentDate).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
            if (latestChange != null && latestChange.AvailabilityFTE < person.FTE)
            {
                // Add a baseline task
                var task = new TaskData
                {
                    ProjectAndTaskName = latestChange.BaselineActivities,
                    EmployeeName = person.Name,
                    FTE = person.FTE
                };
                task.SetIsBaseline(true);
                task.SetMonthlyValue(currentDate.Month, currentDate.Year, (int)Math.Round(100 * (person.FTE - latestChange.AvailabilityFTE)));
                data.Add(task);
            }

            // March forward month by month
            while (currentDate < endDate)
            {
                // Check for new availability changes in the current month which would constitute a new baseline task
                var currentMonthAvailabilityChanges = availabilityChanges.Where(x => IsWithinMonth(x.ChangeDate, currentDate)).ToList();
                if (currentMonthAvailabilityChanges.Count > 0)
                {
                    // Get the lowest availability for the month as the focus for the month
                    var focus = currentMonthAvailabilityChanges.OrderByDescending(x => x.AvailabilityFTE).FirstOrDefault();

                    // Add a new baseline task and value
                    var task = new TaskData
                    {
                        ProjectAndTaskName = focus.BaselineActivities,
                        EmployeeName = person.Name,
                        FTE = person.FTE
                    };
                    task.SetIsBaseline(true);
                    task.SetMonthlyValue(currentDate.Month, currentDate.Year, (int)Math.Round(100 * focus.AvailabilityFTE));
                    data.Add(task);
                }

                // If no change to availability then update the monthly value for the latest baseline task with the same value as the previous month
                // as long as this isn't the first month
                else if (currentDate != startDate.Date)
                {
                    var existing = data.LastOrDefault(x => x.GetIsBaseline());
                    existing?.SetMonthlyValue(currentDate.Month, currentDate.Year, existing?.GetMonthlyValue(currentDate.AddMonths(-1).Month, currentDate.AddMonths(-1).Year));
                }


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
                    if (proj == null)
                    {
                        Debug.WriteLine($"** We have a task without a project that has a resource! Task ID = {t.SubTaskId}, Task Name = {t.Name}, Person = {person.Name}!");
                        continue;
                    }
                    var name = $"{proj.GetFullName()} : {t.Name}";

                    // Add / update a row for every task running in the month
                    var existing = data.FirstOrDefault(x => x.ProjectAndTaskName == name);
                    if (existing != null)
                    {
                        // Add new month entry for existing task
                        existing.SetMonthlyValue(currentDate.Month, currentDate.Year, (int)Math.Round(t.AssignedResources.First(x => x.Person == person).Percentage * 100));
                    }
                    else
                    {
                        // Add new task
                        var task = new TaskData
                        {
                            EmployeeName = person.Name,
                            FTE = person.FTE,
                            ProjectAndTaskName = name,
                            InnateActivity = proj.InnateActivity,
                        };
                        task.SetMonthlyValue(currentDate.Month, currentDate.Year, (int)Math.Round(t.AssignedResources.First(x => x.Person == person).Percentage * 100));
                        data.Add(task);
                    }
                }
                currentDate = currentDate.AddMonths(1).Date;

            }

            // Block out tasks after they leave or before they start
            currentDate = startDate.Date;
            while (currentDate < endDate)
            {
                // If person hasn't started yet by this month or has already left then set values of their tasks to null
                if (person.StartDate > currentDate.EndOfMonth() || (person.EndDate != null && person.EndDate < currentDate))
                {
                    foreach (var task in data)
                    {
                        task.SetMonthlyValue(currentDate.Month, currentDate.Year, null);
                    }
                }
                currentDate = currentDate.AddMonths(1).Date;
            }

            Debug.WriteLine($"** Exported {data.Count} rows for {person.Name}");
            return data;

        }

    }
}
