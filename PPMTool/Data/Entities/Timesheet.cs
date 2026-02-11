// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PPMTool.Enums;

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
        public int OwnerId { get; set; }

        /// <summary>
        /// The person associated with the timesheet as a navigation property
        /// </summary>
        [Required]
        [InverseProperty("Timesheets")]
        public virtual Person Owner { get; set; }

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
        public virtual Person StatusChangedBy { get; set; }

        /// <summary>
        /// Represents the records of hours spent on tasks on the days associated with the specific timesheet.
        /// </summary>
        public virtual ICollection<TimesheetEntry> TimesheetEntries { get; set; } = new List<TimesheetEntry>();

        /// <summary>
        /// Checks to see if the user is the line manager of the timesheet owner
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsLineManager(User user)
        {
            return user?.Person?.PersonId == (Owner?.LineManager?.PersonId ?? 0);
        }

        /// <summary>
        /// Checks to see if the user is the owner of the timesheet
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsOwner(User user)
        {
            return user?.Person?.PersonId == (Owner?.PersonId ?? 0);
        }

        /// <summary>
        /// Checks to see if the user is the line manager of the timesheet owner but not the owner
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsLineManagerButNotOwner(User user)
        {
            return IsLineManager(user) && !IsOwner(user);
        }

        /// <summary>
        /// Checks to see if the user is both the line manager of the timesheet owner and the owner (a self-approver)
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsSelfApprover(User user)
        {
            return IsOwner(user) && IsLineManager(user);
        }

        /// <summary>
        /// Checks to see whether this timesheet is in a state that permits submission of the timesheet by the user
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToEditEntriesAndSubmit(User user)
        {
            return IsOwner(user) && (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected);
        }

        /// <summary>
        /// Checks to see whether this timesheet is in a state that permits approval/rejection of the timesheet by the user.
        /// Note that lilne managers can use the reject button to unapprove a previously approved timesheet.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToApproveOrReject(User user)
        {
            return (IsLineManager(user) && (Status == TimesheetStatus.Submitted || Status == TimesheetStatus.Approved)) ||
                (IsSelfApprover(user) && (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected || Status == TimesheetStatus.Approved));
        }

        /// <summary>
        /// If user may be able to edit in certain circumstance, this checks to see whether based on the current timesheet state whether they are allowed to only view
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public bool IsPermittedToViewOnly(User user)
        {
            return !IsSelfApprover(user) && ((IsOwner(user) && !IsPermittedToEditEntriesAndSubmit(user)) || (IsLineManager(user) && !IsPermittedToApproveOrReject(user)));
        }
    }
}