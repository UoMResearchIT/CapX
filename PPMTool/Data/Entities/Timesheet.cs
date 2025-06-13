using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using PPMTool.Enums;
using static PPMTool.Pages.Timesheets;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a timesheet entity, which is a one calendar week of time records.
    /// </summary>
    public class Timesheet
    {
        /// <summary>
        /// The unique identifier for the timesheet
        /// </summary>
        public int TimesheetId { get; set; }

        /// <summary>
        /// The person associated with the timesheet
        /// </summary>
        [Required]
        [InverseProperty("Timesheets")]
        public Person Owner { get; set; }

        /// <summary>
        /// The date when the timesheet was created
        /// </summary>
        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        /// <summary>
        /// The start date of the timesheet period (Monday)
        /// </summary>
        [Required]
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Additional information about the timesheet
        /// </summary>
        public string Info { get; set; }

        /// <summary>
        /// Represents the status of the timesheet (submitted, approved, rejected, etc.)
        /// </summary>
        public TimesheetStatus Status { get; set; }

        /// <summary>
        /// Represents the date of the status change.
        /// </summary>
        public DateTime DateStatusChanged { get; set; }

        /// <summary>
        /// Represents the person who made the status change.
        /// </summary>
        [InverseProperty("TimesheetsChanged")]
        public Person StatusChangedBy { get; set; }

        /// <summary>
        /// Represents the records of hours spent on tasks on the days associated with the specific timesheet.
        /// </summary>
        public ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();

        /// <summary>
        /// Checks to see if the user is the line manager of the timesheet owner
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsLineManager(Person user)
        {
            return user?.PersonId == (Owner?.LineManager?.PersonId ?? 0);
        }

        /// <summary>
        /// Checks to see if the user is the owner of the timesheet
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsOwner(Person user)
        {
            return user?.PersonId == (Owner?.PersonId ?? 0);
        }

        /// <summary>
        /// Checks to see if the user is the line manager of the timesheet owner but not the owner
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsLineManagerButNotOwner(Person user)
        {
            return IsLineManager(user) && !IsOwner(user);
        }

        /// <summary>
        /// Checks to see if the user is both the line manager of the timesheet owner and the owner (a self-approver)
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsSelfApprover(Person user)
        {
            return IsOwner(user) && IsLineManager(user);
        }

        /// <summary>
        /// Checks to see whether this timesheet is in a state that permits submission of the timesheet by the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToEditEntriesAndSubmit(Person user)
        {
            return IsOwner(user) && (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected);
        }

        /// <summary>
        /// Checks to see whether this timesheet is in a state that permits approval/rejection of the timesheet by the user.
        /// Note that lilne managers can use the reject button to unapprove a previously approved timesheet.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToApproveOrReject(Person user)
        {
            return (IsLineManager(user) && (Status == TimesheetStatus.Submitted || Status == TimesheetStatus.Approved)) ||
                (IsSelfApprover(user) && (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected || Status == TimesheetStatus.Approved));
        }

        /// <summary>
        /// If user may be able to edit in certain circumstance, this checks to see whether based on the current timesheet state whether they are allowed to only view
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToViewOnly(Person user)
        {
            return !IsSelfApprover(user) && ((IsOwner(user) && !IsPermittedToEditEntriesAndSubmit(user)) || (IsLineManager(user) && !IsPermittedToApproveOrReject(user)));
        }

        /// <summary>
        /// Returns all the relevant timesheet data packaged into Dto classes which
        /// total the hourse for each day and exclude specific Innate codes (Holidays etc.)
        /// </summary>
        /// <param name="excludedTaskCodes"></param>
        /// <returns></returns>
        public List<TimesheetDataDownloadDto> GetDailySummaries(HashSet<int> excludedTaskCodes)
        {
            if (TimesheetEntries.Count > 0)
            {
                var dayOffsets = new Dictionary<string, int>
                {
                    ["MondayHours"] = 0,
                    ["TuesdayHours"] = 1,
                    ["WednesdayHours"] = 2,
                    ["ThursdayHours"] = 3,
                    ["FridayHours"] = 4,
                    ["SaturdayHours"] = 5,
                    ["SundayHours"] = 6
                };

                var dailySummaries = new List<TimesheetDataDownloadDto>();

                foreach (var kv in dayOffsets)
                {
                    // Sum only the hours worked on the current day (kv.Key)
                    double totalHours = TimesheetEntries
                        .Where(entry => entry.InnateCodeTask != null && !excludedTaskCodes.Contains(entry.InnateCodeTask.InnateCodeTaskId)) // Ignore excluded tasks codes (Annual Leave / Closures etc.)
                        .Sum(entry => (double?)entry.GetType().GetProperty(kv.Key)?.GetValue(entry) ?? 0); // Only sum the current day's hours

                    if (totalHours > 0)
                    {
                        dailySummaries.Add(new TimesheetDataDownloadDto
                        {
                            PersonName = Owner.Name,
                            Date = StartDate.AddDays(kv.Value), // Correctly offset date
                            HoursWorked = totalHours
                        });
                    }
                }

                return dailySummaries;
            }
            else
            {
                return null;
            }
        }
    }
}