using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using System;
using System.Collections;
using System.Collections.Generic;
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
            public bool IsBaseline { get; set; }

            public string EmployeeNumber { get; set; }

            public string RRAR { get; set; }

            public string Domain { get; set; } = "RIT";

            public string EmployeeName { get; set; }

            public double FTE { get; set; }

            public string Manager { get; set; }

            public string TaskDescription { get; set; }

            public string InnateActivity { get; set; }


            private Dictionary<int, int?> dataByMonth = new Dictionary<int, int?>();

            public int? Get(int month)
            {
                if (dataByMonth.TryGetValue(month, out var data))
                {
                    return data;
                }
                return 0;
            }

            public void Set(int month, int? value)
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
        /// <param name="numMonthsIntoFuture">Number of months into the future we want data for</param>
        /// <returns>List of data items</returns>
        public IEnumerable<TaskData> GetExportDataForPerson(Person person, IEnumerable<SubTask> subTasks, int numMonthsIntoFuture)
        {
            // New list
            var data = new List<TaskData>();

            // Set reference months
            var now = DateTime.Now.Date;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var monthNum = 0;
            var endDate = startDate.AddMonths(numMonthsIntoFuture + 1);
            var currentDate = startDate.AddMonths(1).Date;

            // March forward month by month
            while (currentDate < endDate)
            {
                // If there is an availability change this month then update their baseline task
                var availabilityChanges = person.AvailabilityChanges.Where(x => x.ChangeDate.Month == currentDate.Month).ToList();
                if (availabilityChanges.Count > 0)
                {
                    // Get the lowest availability for the month as the focus for the month
                    var focus = availabilityChanges.OrderByDescending(x => x.AvailabilityFTE).FirstOrDefault();

                    // Find existing baseline task if exists
                    var existing = data.FirstOrDefault(x => x.IsBaseline);
                    if (existing != null)
                    {
                        // Update description and add value for this month
                        existing.InnateActivity = focus.BaselineActivities;
                        existing.Set(currentDate.Month, (int)Math.Round(100 * focus.AvailabilityFTE / .84));
                    }
                    else
                    {
                        // Add a new baseline task and value
                        var task = new TaskData
                        {
                            EmployeeName = person.Name,
                            FTE = (int)Math.Round(person.FTE / 0.84),
                            IsBaseline = true,
                            InnateActivity = focus.BaselineActivities
                        };
                        existing.Set(currentDate.Month, (int)Math.Round(100 * focus.AvailabilityFTE / .84));
                        data.Add(task);
                    }
                }

                // If person hasn't started yet in this month then set value of all existing tasks for this month to blank
                if (person.EndDate != null && person.EndDate.Value.Date.Month > currentDate.Date.Month)
                {
                    foreach (var task in data)
                    {
                        task.Set(currentDate.Month, null);
                    }
                }
                else
                {
                    // Find all subtasks that run in this month based on the following conditions:
                    // 1. Starts before month and finishes after month
                    // 2. Starts this month
                    // 3. Ends this month
                    var tasksThisMonth = subTasks.Where(x => (x.StartDate <= currentDate && x.EndDate >= currentDate) || x.StartDate.Month == currentDate.Month || x.EndDate.Month == currentDate.Month);

                    foreach (var t in tasksThisMonth)
                    {

                        // Add / update a row for every task running on the first of the month
                        var existing = data.FirstOrDefault(x => x.TaskDescription == t.Name);
                        if (existing != null)
                        {
                            // Add new month entry for existing task
                            existing.Set(currentDate.Month, (int)t.AssignedResources.First(x => x.Person == person).Percentage);
                        }
                        else
                        {
                            // Add new task
                            var task = new TaskData
                            {
                                EmployeeName = person.Name,
                                FTE = (int)Math.Round(person.FTE / 0.84),
                                TaskDescription = t.Name,
                                InnateActivity = t.InnateActivity
                            };
                            existing.Set(currentDate.Month, (int)t.AssignedResources.First(x => x.Person == person).Percentage);
                            data.Add(task);
                        }
                    }
                }
                monthNum++;
            }

            return data;
            
        }

    }
}
