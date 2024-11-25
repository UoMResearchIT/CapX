using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using static PPMTool.Data.StatusMessage;

namespace PPMTool.Pages
{
    public partial class AddTimesheet : DataGridPage<TimesheetEntry>
    {
        /// <summary>
        /// ID of the timesheet to edit if applicable
        /// </summary>
        [Parameter]
        public int? TimesheetId { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

        private Timesheet timesheet = new Timesheet();
        private IEnumerable<InnateCode> innateCodes = new List<InnateCode>();
        private IEnumerable<InnateCodeTask> innateCodeTasks = new List<InnateCodeTask>();
        private Person activeUser;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get the person associated with the active user
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            activeUser = role.Person;

            // Handle if the user is not found
            if (activeUser == null)
            {
                LogError($"No person found for {ActiveUserName} and they are accessing the add/edit timesheet page!");
                ErrorMessage = new StatusMessage("You do not have a person record. Please contact your line manager.", MessageType.Error);
                return;
            }

            // If there is an ID, then lookup the timesheet
            if ((TimesheetId ?? 0) > 0)
            {
                timesheet = TimesheetService.GetById(Context, TimesheetId);

                // Only superusers, owner or line manager of owner can edit the timesheet
                EditAuthorised =
                    role.RoleType == RoleType.Superuser ||
                    timesheet.Owner.PersonId == activeUser.PersonId ||
                    timesheet.Owner.LineManager.PersonId == activeUser.PersonId;
            }
            else
            {
                var lastTimesheetForThisUser = TimesheetService.GetLastForUser(Context, activeUser);

                timesheet = new Timesheet()
                {
                    Owner = activeUser,
                    StartDate = lastTimesheetForThisUser?.StartDate.AddDays(7).Date ?? activeUser.StartDate.Date
                };
            }
            dataGridEntities = timesheet.TimesheetEntries.ToList();

            LogInformation($"Viewing timesheet {timesheet.TimesheetId} for {timesheet.Owner?.Name}");
        }

        /// <summary>
        /// Discard changes
        /// </summary>
        private void DiscardTimesheet()
        {
            Navigation.NavigateTo("timesheets");
        }

        private async void DeleteTimesheet()
        {
            if (TimesheetId > 0)
            {
                // Prompt
                bool confirmed = await DialogService.Confirm($"You are about to delete this timesheet. This cannot be undone!",
                    "Delete Timesheet") ?? false;
                if (confirmed)
                {
                    LogInformation($"Deleting timesheet {timesheet.TimesheetId}");

                    // Delete from DB
                    TimesheetService.Delete(Context, timesheet);

                    // Navigate back
                    Navigation.NavigateTo("timesheets");
                }
            }
        }

        /// <summary>
        /// Handle the validation of the timesheet
        /// </summary>
        private void HandleValidSubmit()
        {
            // TODO: Some kind of validation and show a status message
            ErrorMessage = null;

            // Add the timesheet entries from the datagrid to the timesheet model
            timesheet.TimesheetEntries.Clear();
            foreach (var entry in dataGridEntities)
            {
                timesheet.TimesheetEntries.Add(entry);
            }

            // Carry out DB actions
            LogInformation($"Saving timesheet {timesheet.CreatedDate.ToShortDateString()} for {timesheet.Owner.Name}...");
            if (TimesheetId > 0)
            {
                TimesheetService.Update(Context, timesheet);
            }
            else
            {
                TimesheetService.Add(Context, timesheet);

            }
            Navigation.NavigateTo("timesheets");
        }

        private void InnateCodeChanged(object value)
        {
            // TODO: Load the innate tasks associated with the selected innate code
            Debug.WriteLine(value);
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnCreateRow(TimesheetEntry entity)
        {
            Reset();
            LogInformation($"Create row for <{entity?.GetSensibleObjectName()}>");
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(TimesheetEntry entity)
        {
            Reset();
            LogInformation($"Update row for <{entity?.GetSensibleObjectName()}>");
        }

        /// <summary>
        /// Necessary override since Timesheet and TimesheetEntry entities are edited on the same page
        /// </summary>
        /// <param name="entity"></param>
        protected override void CancelEdit(TimesheetEntry entity)
        {
            LogInformation($"Cancel edit row for <{entity?.GetSensibleObjectName()}>");
            Reset();
            TimesheetService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);
        }
    }
}
