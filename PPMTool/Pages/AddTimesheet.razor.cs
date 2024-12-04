using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FluentDateTime;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class AddTimesheet : DataGridPage<TimesheetEntry>
    {
        /// <summary>
        /// ID of the timesheet to edit if applicable
        /// </summary>
        [Parameter]
        public int? TimesheetId { get; set; }

        private bool IsSavingTimesheetProgress { get; set; } = false;

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        private Timesheet timesheet;
        private IEnumerable<InnateCode> innateCodes = new List<InnateCode>();
        private IEnumerable<InnateCodeTask> innateCodeTasks = new List<InnateCodeTask>();
        private Person activeUser;
        private double mondayHours;
        private double tuesdayHours;
        private double wednesdayHours;
        private double thursdayHours;
        private double fridayHours;
        private double saturdayHours;
        private double sundayHours;
        private double totalHours;
        private Role activeUserRole;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get the person associated with the active user
            activeUserRole = RolesService.GetByUsername(Context, ActiveUserName);
            activeUser = activeUserRole.Person;

            // Only superusers can delete a timesheet
            EditAuthorised = activeUserRole.RoleType == RoleType.Superuser;

            // Handle if the user is not found
            if (activeUser == null)
            {
                LogError($"No person found for {ActiveUserName} and they are accessing the add/edit timesheet page!");
                return;
            }

            // If there is an ID, then lookup the timesheet
            if ((TimesheetId ?? 0) > 0)
            {
                timesheet = TimesheetService.GetById(Context, TimesheetId);
            }

            // Check whether this user should have access or not
            if (timesheet != null && !IsPermittedToViewTimesheetDetailsPage())
            {
                timesheet = null;
            }

            // If no timesheet and intention is create
            if (timesheet == null && TimesheetId == -1)
            {
                var lastTimesheetForThisUser = TimesheetService.GetLastForUser(Context, activeUser);
                timesheet = new Timesheet()
                {
                    Owner = activeUser,
                    StartDate = lastTimesheetForThisUser?.StartDate.AddDays(7).Date ?? activeUser.StartDate.Date.FirstDayOfWeek()
                };

                // Immediately save the timesheet to the DB
                int newId = TimesheetService.Add(Context, timesheet);

                // If a duplicate is detected then throw an error as this should never happen
                if (newId == -1)
                {
                    throw new Exception("Error creating new timesheet!");
                }

                // Redirect to the newly created Timesheet so refrshing the page
                // with the -1 parameter doesn't create another new timesheet.
                Navigation.NavigateTo($"addtimesheet/{timesheet.TimesheetId}");
            }

            if (timesheet != null)
            {
                dataGridEntities = timesheet.TimesheetEntries.ToList();
                UpdateDailyTotals();
                innateCodes = InnateCodeService.GetAll(Context);
            }

            LogInformation($"Viewing timesheet {timesheet?.TimesheetId} for {timesheet?.Owner?.Name}");
        }

        /// <summary>
        /// Should this user be allowed to view the timesheet. Only superusers, the owner or the line manager.
        /// </summary>
        /// <returns></returns>
        private bool IsPermittedToViewTimesheetDetailsPage()
        {
            return (timesheet?.IsOwner(activeUser) ?? false) ||
                (timesheet?.IsLineManager(activeUser) ?? false) ||
                activeUserRole.RoleType == RoleType.Superuser;
        }

        /// <summary>
        /// Method to sum up the total hours per day and for the whole timesheet
        /// </summary>
        private void UpdateDailyTotals()
        {
            // The entity to add has not been included in the datagrid entites list yet
            var allEntities = dataGridEntities;
            Debug.WriteLine($"** Hours changed, datagrid count = {dataGridEntities.Count}");
            if (entityToInsert != null && !allEntities.Contains(entityToInsert))
            {
                allEntities.Add(entityToInsert);
                Debug.WriteLine($"** Added entity, all entities count = {allEntities.Count}");
            }

            // Now sum up the hours
            mondayHours = allEntities.Sum(x => x.MondayHours);
            tuesdayHours = allEntities.Sum(x => x.TuesdayHours);
            wednesdayHours = allEntities.Sum(x => x.WednesdayHours);
            thursdayHours = allEntities.Sum(x => x.ThursdayHours);
            fridayHours = allEntities.Sum(x => x.FridayHours);
            saturdayHours = allEntities.Sum(x => x.SaturdayHours);
            sundayHours = allEntities.Sum(x => x.SundayHours);
            totalHours = mondayHours + tuesdayHours + wednesdayHours + thursdayHours + fridayHours + saturdayHours + sundayHours;

            // Manually update the total hours on the entities. Urgh!
            foreach (var entity in allEntities)
            {
                entity.UpdateTotalHours();
            }

            StateHasChanged();
        }

        /// <summary>
        /// Delete a timesheet with prompt
        /// </summary>
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

        public void NoteChanged()
        {
            TimesheetService.Update(Context, timesheet);

            // Show notification for save action
            ShowNotification(new CapXNotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Saved",
                Detail = "Your timesheet has been updated."
            });
            return;
        }

        /// <summary>
        /// Handle the validation of the timesheet
        /// </summary>
        private async void HandleValidSubmit()
        {
            // Reset error message
            ErrorMessage = null;

            // If saving the timesheet progress just update the db for any changed notes
            if (IsSavingTimesheetProgress)
            {
                TimesheetService.Update(Context, timesheet);
                Debug.WriteLine("Timesheet ptogress saved");

                // Show notification for save action
                ShowNotification(new CapXNotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Saved",
                    Detail = "Your timesheet progress has been saved."
                });
                return;
            }

            // Validation on minimum hours etc. and show a status message
            if (timesheet.Status == TimesheetStatus.Submitted && dataGridEntities.Count == 0)
            {
                ErrorMessage = new StatusMessage("You must have at least one entry in your timesheet to submit it!", StatusMessage.MessageType.Error);
                timesheet.Status = TimesheetStatus.New;
                return;
            }

            // Decide what to do based on the status of the timesheet
            if (timesheet.Status == TimesheetStatus.New || timesheet.Status == TimesheetStatus.Submitted)
            {
                // Reset the timesheet entries on the model
                timesheet.TimesheetEntries.Clear();

                // Take a copy as the datagrid entities will change inside a foreach loop
                var temp = dataGridEntities.ToList();
                Debug.WriteLine($"** {temp.Count} items in the datagrid");

                // Add the timesheet entries from the datagrid to the timesheet model
                foreach (var entry in temp)
                {
                    // If a timesheet entry has no hours associated with it then
                    // delete it from the database and do not add it to the model
                    if (entry.TotalHours == 0 && timesheet.Status == TimesheetStatus.Submitted)
                    {
                        LogInformation($"Removing blank timesheet entry from DB for {entry.GetSensibleObjectName}...");
                        await DeleteRow(entry);
                    }
                    else
                    {
                        Debug.WriteLine($"** Adding entry {entry.GetSensibleObjectName()} to timesheet");
                        timesheet.TimesheetEntries.Add(entry);
                    }
                }
            }

            // Set status changed information
            if (timesheet.Status != TimesheetStatus.New)
            {
                timesheet.DateStatusChanged = DateTime.Now;
                timesheet.StatusChangedBy = activeUser;
            }

            // Save to database
            LogInformation($"Saving timesheet {timesheet.CreatedDate.ToShortDateString()} for {timesheet.Owner.Name}. New status = {timesheet.Status.ToNiceString()}...");
            TimesheetService.Update(Context, timesheet);

            // Only navigate away if the status is new as this means the save button has been clicked
            if (timesheet.Status != TimesheetStatus.New)
            {
                Navigation.NavigateTo("timesheets");
            }

            // Refresh the data grid
            await dataGrid.Reload();
            StateHasChanged();
        }

        /// <summary>
        /// Handle a change in the code on the first dropdown
        /// </summary>
        /// <param name="value"></param>
        private void InnateCodeChanged(object value)
        {
            // Load the innate tasks associated with the selected innate code
            Debug.WriteLine($"** Selected {value}");
            var tasks = innateCodes.FirstOrDefault(x => x.GetCodeAsString() == (value as string)).Tasks.ToList();

            // Find all exsiting entries that use this same code
            var tasksInUse = dataGridEntities.Where(x => x.InnateCodeTask.InnateCode.GetCodeAsString() == (value as string)).Select(x => x.InnateCodeTask).ToList();

            // Remove the tasks from the list that are already in use
            tasks.RemoveAll(x => tasksInUse.Contains(x));
            innateCodeTasks = tasks;
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnCreateRow(TimesheetEntry entity)
        {
            Reset();
            LogInformation($"Add row to database for <{entity?.GetSensibleObjectName()}>");
            TimesheetService.AddEntry(Context, entity);
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(TimesheetEntry entity)
        {
            Reset();
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");
            TimesheetService.UpdateEntry(Context, entity);
        }

        /// <summary>
        /// Necessary override since Timesheet and TimesheetEntry entities are edited on the same page
        /// </summary>
        /// <param name="entity"></param>
        protected override void CancelEdit(TimesheetEntry entity)
        {
            LogInformation($"Cancel Edit row in view for <{entity?.GetSensibleObjectName()}>");
            Reset();
            TimesheetService.RestoreModel(Context, ref entity);
            dataGrid.CancelEditRow(entity);

            // Remove entries that have not been added to the DB
            var itemsToRemove = dataGridEntities.Where(x => x.TimesheetEntryId == 0).ToList();
            foreach (var e in itemsToRemove)
            {
                Debug.WriteLine($"** Removing empty entry {e.GetSensibleObjectName()}");
                dataGridEntities.Remove(e);
            }

            // Update the totals
            UpdateDailyTotals();
            entity.UpdateTotalHours();
        }

        /// <summary>
        /// Override to make sure we add the timesheet reference to the entry
        /// </summary>
        /// <returns></returns>
        protected override async Task InsertRow()
        {
            await base.InsertRow();
            entityToInsert.Timesheet = timesheet;
        }

        /// <summary>
        /// Remove the entity from the DB table
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task DeleteRow(TimesheetEntry entity)
        {
            TimesheetService.DeleteEntry(Context, entity);
            await base.DeleteRow(entity);
            UpdateDailyTotals();
        }
    }
}
