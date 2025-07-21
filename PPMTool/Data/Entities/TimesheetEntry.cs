using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an entry in the timesheet indicating the number of hours worked on a particular timesheet task on a given day
    /// </summary>
    public class TimesheetEntry : ILoggableClass
    {
        /// <summary>
        /// Represents the ID of the timesheet entry record.
        /// </summary>
        public int TimesheetEntryId { get; set; }

        /// <summary>
        /// Represents the timesheet which owns the timesheet entry
        /// </summary>
        [Required]
        public int TimesheetId { get; set; }

        /// <summary>
        /// Represents the timesheet which owns the timesheet entry as a navigation property
        /// </summary>
        [Required]
        public Timesheet Timesheet { get; set; }

        /// <summary>
        /// Represents the innate code task associated with the timesheet entry.
        /// </summary>
        [Required]
        public InnateCodeTask InnateCodeTask { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Monday.
        /// </summary>
        public double MondayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Tuesday.
        /// </summary>
        public double TuesdayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Wednesday.
        /// </summary>
        public double WednesdayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Thursday.
        /// </summary>
        public double ThursdayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Friday.
        /// </summary>
        public double FridayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Saturday.
        /// </summary>
        public double SaturdayHours { get; set; }

        /// <summary>
        /// Represents the number of hours spent on the task on Sunday.
        /// </summary>
        public double SundayHours { get; set; }

        /// <summary>
        /// Total hours for the week for this entry
        /// </summary>
        [NotMapped]
        public double TotalHours { get; private set; }

        /// <summary>
        /// Represents whether this task is part of the user's template
        /// </summary>
        [NotMapped]
        public bool IsInTemplate { get; set; }

        /// <summary>
        /// Method to sum up the hours in the entry
        /// </summary>
        /// <returns></returns>
        public void UpdateTotalHours()
        {
            TotalHours = MondayHours + TuesdayHours + WednesdayHours + ThursdayHours + FridayHours + SaturdayHours + SundayHours;
        }

        /// <summary>
        /// Returns a useful string to identify the entity
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"TimesheetEntry: Entry: {TimesheetEntryId} | Timesheet: {Timesheet?.TimesheetId} | Owner: {Timesheet?.Owner.Name} | Task: {InnateCodeTask?.GetSensibleObjectName()}";
        }
    }
}