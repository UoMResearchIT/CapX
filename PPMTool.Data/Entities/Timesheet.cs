// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using PPMTool.Data.Enums;

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
        public virtual Person Owner { get; set; } = null!;

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
        public string? Info { get; set; }

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
        public virtual Person? StatusChangedBy { get; set; }

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
        /// The owner, line manager or a superuser may view the timesheet.
        /// </summary>
        public bool IsPermittedToView(User user)
        {
            return IsOwner(user) || IsLineManager(user) || IsSuperuser(user);
        }

        /// <summary>
        /// Line managers and superusers may see WLM information.
        /// </summary>
        public bool IsPermittedToSeeWLM(User user)
        {
            return IsLineManager(user) || IsSuperuser(user);
        }

        /// <summary>
        /// Only the owner may change their timesheet template.
        /// Superuser and line-manager permissions do not grant this capability.
        /// </summary>
        public bool IsPermittedToReorderTemplate(User user)
        {
            return IsOwner(user);
        }

        /// <summary>
        /// The owner, line manager or a superuser may edit an unsubmitted
        /// or rejected timesheet.
        /// </summary>
        public bool IsPermittedToEditEntries(User user)
        {
            return (IsOwner(user) || IsLineManager(user) || IsSuperuser(user)) &&
                (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected);
        }

        /// <summary>
        /// The owner, line manager or a superuser may submit an unsubmitted
        /// or rejected timesheet.
        /// </summary>
        public bool IsPermittedToSubmit(User user)
        {
            return (IsOwner(user) || IsLineManager(user) || IsSuperuser(user)) &&
                (Status == TimesheetStatus.New || Status == TimesheetStatus.Rejected);
        }

        /// <summary>
        /// The owner, line manager or a superuser may retract a submitted timesheet.
        /// </summary>
        public bool IsPermittedToRetract(User user)
        {
            return (IsOwner(user) || IsLineManager(user) || IsSuperuser(user)) &&
                Status == TimesheetStatus.Submitted;
        }

        /// <summary>
        /// A line manager or superuser may approve a submitted timesheet.
        /// </summary>
        public bool IsPermittedToApprove(User user)
        {
            return (IsLineManager(user) || IsSuperuser(user)) &&
                Status == TimesheetStatus.Submitted;
        }

        /// <summary>
        /// A line manager or superuser may reject a submitted timesheet,
        /// or unapprove an approved timesheet.
        /// </summary>
        public bool IsPermittedToReject(User user)
        {
            return (IsLineManager(user) || IsSuperuser(user)) &&
                (Status == TimesheetStatus.Submitted || Status == TimesheetStatus.Approved);
        }

        /// <summary>
        /// Only a superuser may delete a complete timesheet.
        /// </summary>
        public bool IsPermittedToDelete(User user)
        {
            return IsSuperuser(user);
        }

        /// <summary>
        /// Local helper as it is called by other permissions checkers
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private static bool IsSuperuser(User user)
        {
            return user?.RoleType == RoleType.Superuser;
        }
    }
}