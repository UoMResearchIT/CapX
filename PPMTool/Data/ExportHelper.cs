using PPMTool.Data.Entities;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PPMTool.Data
{
    public class ExportHelper
    {
        /// <summary>
        /// Represents a task whose load is stored for 6 months
        /// </summary>
        public class TaskData
        {
            public bool IsBaseline { get; }

            // Hardcoded to 6 months. Could make it generic?
            float[] values = new float[6];

            string Name { get; }

            public TaskData(bool isBaseline, string name)
            {
                IsBaseline = isBaseline;
                Name = name;
            }

            public double Get(int month)
            {
                return values[month];
            }

            public void Set(int month, float value)
            {
                values[month] = value;
            }
        }

        /// <summary>
        /// Represents the collective data to be exported for an individual
        /// </summary>
        public class ExportData
        {
            IEnumerable<TaskData> TaskData { get; }

            string Name { get; }

            double FTE { get; }

            public ExportData(string personName, float fte)
            {
                TaskData = new List<TaskData>();
                Name = personName;
                FTE = fte;
            }

        }

        /// <summary>
        /// Given a person, prepare data
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        public ExportData GetExportDataForPerson(Person person)
        {
            // Setup the data structure for this person
            var data = new ExportData(person.Name, ((int)Math.Round(person.FTE * 100 / .84)) / 100f);

            // Set reference months
            var now = DateTime.Now.Date;
            var startMonth = new DateTime(now.Year, now.Month, 1);
            var monthNum = 0;
            var endMonth = startMonth.AddMonths(7);

            // March forward month by month
            while (startMonth.AddMonths(monthNum).Date < endMonth)
            {
                // TODO: Check for baseline tasks based on availability and FTE of the post


                // Create a baseline task if their default availability is lower than their FTE

                // Create a baseline task any time a person's availablity changes and remains below their baseline availability

                // Get all the subtasks



                monthNum++;
            }


            
        }

    }
}
