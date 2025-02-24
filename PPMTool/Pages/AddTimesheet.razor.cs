using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
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

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        [Inject]
        public EmailService EmailService { get; set; }

        private Timesheet timesheet;
        private IList<InnateCode> innateCodeDropdownSource = new List<InnateCode>();
        private IEnumerable<InnateCodeTask> innateCodeTaskDropdownSource = new List<InnateCodeTask>();
        private double mondayHours;
        private double tuesdayHours;
        private double wednesdayHours;
        private double thursdayHours;
        private double fridayHours;
        private double saturdayHours;
        private double sundayHours;
        private double totalHours;
        private Role activeUserRole;
        private int entryMinimum = 0;
        private double entryStep = 0.25;
        private Dictionary<string, string> DayColours = new Dictionary<string, string>
        {
            { "mon", "#EEE" },
            { "wed", "#EEE" },
            { "fri", "#EEE" },
            { "sat", "#FDFBD4" },
            { "sun", "#FDFBD4" }
        };
        private TimesheetStatus newStatus;
        private Timesheet previousTimesheet;
        private Timesheet nextTimesheet;
        private WorkloadModelChange currentWLM;
        private WLMWeeklyDataChartItem wlmChartItem;
        private double totalFTEForTimesheet;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Loading = true;
            StateHasChanged();

            EnqueueLoadData(GetTask);
        }

        /// <summary>
        /// Get the task to run in the background
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private Task GetTask()
        {
            return Task.Run(() =>
            {
                Debug.WriteLine("** Starting initialisation task...");

                // Get the person associated with the active user
                activeUserRole = RolesService.GetByUsername(Context, ActiveUserName);

                // Only superusers can delete a timesheet
                EditAuthorised = activeUserRole.RoleType == RoleType.Superuser;

                // Handle if the user is not found
                if (ActiveUser == null)
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
                    return;
                }

                if (timesheet != null)
                {
                    // Get details for the prev/next timesheets based on the owner of the timesheet being viewed
                    // (to accommodate a manager looking at a staff timesheet).
                    previousTimesheet = null;
                    nextTimesheet = null;
                    DateTime previousDate = timesheet.StartDate.AddDays(-7);
                    DateTime nextDate = timesheet.StartDate.AddDays(7);
                    List<Timesheet> prevandnext = TimesheetService.GetAllTimesheetsForPersonInDateRange(Context, timesheet.Owner, previousDate, nextDate).ToList();
                    foreach (Timesheet t in prevandnext)
                    {
                        if (t.StartDate == previousDate) { previousTimesheet = t; }
                        if (t.StartDate == nextDate) { nextTimesheet = t; }
                    }

                    PopulateDataGridDataSource();
                    UpdateDailyTotals();

                    // Innate codes are limited to active ones initially
                    LoadInnateCodes();

                    // Get WLM details of the staff member active at the time of the timesheet
                    Person personWithWLMData = PersonService.GetById(Context, timesheet.Owner.PersonId);
                    currentWLM = personWithWLMData.GetWorkloadModelOnDateOrDefault(timesheet.StartDate);
                    wlmChartItem = WorkloadModelChartHelper.GetWorkloadModelChartData(timesheet.Owner, timesheet.StartDate, new List<Timesheet> { timesheet });

                    // Get total hours for the week across all duties
                    totalFTEForTimesheet = wlmChartItem.WeeklyValuesByDuty.Sum(x => x.Value);
                }

                LogInformation($"Viewing timesheet {timesheet?.TimesheetId} for {timesheet?.Owner?.Name}");
            }).ContinueWith(t =>
            {
                if (timesheet == null && TimesheetId == -1)
                {
                    // Get the start date for the new timesheet
                    var nextTimesheetStartDate = TimesheetService.GetNextTimesheetStartDateForUser(Context, ActiveUser);
                    timesheet = new Timesheet()
                    {
                        Owner = ActiveUser,
                        StartDate = nextTimesheetStartDate
                    };

                    // Immediately save the timesheet to the DB
                    int newId = TimesheetService.Add(Context, timesheet);

                    // If a duplicate is detected then throw an error as this should never happen
                    if (newId == -1)
                    {
                        throw new Exception("Error creating new timesheet!");
                    }
                    else
                    {
                        // Set-up the timesheet from the template
                        TimesheetService.SetupTimesheetFromTemplate(Context, timesheet, ActiveUser, InnateCodeService.GetAllTasks(Context));
                    }

                    // Redirect to the newly created Timesheet so refreshing the page
                    // with the -1 parameter doesn't create another new timesheet.
                    Navigation.NavigateTo($"timesheets/addtimesheet/{timesheet.TimesheetId}");

                    LogInformation($"Timesheet created with ID {timesheet?.TimesheetId} for {timesheet?.Owner?.Name}");
                }
                else
                {
                    Loading = false;
                    InvokeAsync(StateHasChanged);
                }
                Debug.WriteLine("** ...complete!");
            });
        }

        /// <summary>
        /// Method to get the timesheet entries for the datagrid
        /// </summary>
        private void PopulateDataGridDataSource()
        {
            if (ActiveUser.PersonId == timesheet.Owner.PersonId)
            {
                // Use the staff member's timesheet template for the ordering
                string templateOrdering = ActiveUser.TimesheetTemplateData;
                dataGridEntities = OrderByTemplate(timesheet, templateOrdering);
            }
            else
            {
                // Order by Duty if Line Manager viewing
                dataGridEntities = timesheet.TimesheetEntries.OrderBy(x => x.InnateCodeTask.Duty).ToList();
            }
        }

        /// <summary>
        /// Gets the user's template data to work with, and orders the timesheet entries accordingly.
        /// Items not part of the template get put at the end of the ordered list, ordered by InnateCodeTaskId
        /// </summary>
        /// <param name="timesheet">The timesheet being viewed</param>
        /// <param name="templateOrderDetail">The string of pipe-separated TaskIds detailing the user's template items</param>
        private List<TimesheetEntry> OrderByTemplate(Timesheet timesheet, string templateOrderDetail)
        {
            List<int> order = new List<int>();

            if (!string.IsNullOrEmpty(templateOrderDetail))
            {
                foreach (string s in templateOrderDetail.Split("|"))
                {
                    order.Add(int.Parse(s));
                }

                // Custom ordering
                var orderedResults = timesheet.TimesheetEntries
                    .OrderBy(r => order.IndexOf(r.InnateCodeTask.InnateCodeTaskId) == -1 ? int.MaxValue : order.IndexOf(r.InnateCodeTask.InnateCodeTaskId))
                    .ThenBy(r => r.InnateCodeTask.InnateCodeTaskId)
                    .ToList();

                // Sets a boolean for use in the datagrid to show which items are part of the template
                foreach (TimesheetEntry e in orderedResults)
                {
                    e.IsInTemplate = order.Contains(e.InnateCodeTask.InnateCodeTaskId);
                }

                return orderedResults;
            }
            else
            {
                return timesheet.TimesheetEntries.ToList();
            }
        }

        /// <summary>
        /// Method tied to the datagrid called when the column sorting icons are clicked.
        /// Uses the data passed to calculate what needs to be ordered and how.
        /// </summary>
        /// <param name="args"></param>
        void OnDataLoad(LoadDataArgs args)
        {
            IQueryable<TimesheetEntry> query = timesheet.TimesheetEntries.AsQueryable();

            // The ordering of timesheet entries epends on who is logged in 
            // and whose timesheet they are viewing
            if (!string.IsNullOrEmpty(args.OrderBy)) // Sorting link has been clicked
            {
                if (args.OrderBy.Contains("Duty"))
                {
                    // "WLM Task & Duty" column sorting has been clicked
                    query = args.OrderBy.EndsWith("desc")
                    ? query.OrderByDescending(e => e.InnateCodeTask.Duty.ToNiceString()).ThenBy(e => e.InnateCodeTask.TaskName)
                    : query.OrderBy(e => e.InnateCodeTask.Duty.ToNiceString()).ThenBy(e => e.InnateCodeTask.TaskName);
                }
                else
                {
                    // Innate code column sorting has been clicked
                    query = args.OrderBy.EndsWith("desc")
                    ? query.OrderByDescending(e => e.InnateCodeTask.InnateCode.ActivityCode)
                    : query.OrderBy(e => e.InnateCodeTask.InnateCode.ActivityCode);
                }

                dataGridEntities = query.ToList();
            }
            else
            {
                // Default is to order by the user's template if viewing their own timesheet
                PopulateDataGridDataSource();
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>
        /// Load innate codes to populate the dropdown source. If there is a timesheet then remove codes that have been used and have no tasks left.
        /// </summary>
        private void LoadInnateCodes()
        {
            Debug.WriteLine("** Loading innate codes...");

            // Get all active from the DB
            var temp = InnateCodeService.GetActive(Context).ToList();

            // If this timesheet contains codes that are not in the dropdown list then add them in
            foreach (var code in timesheet.TimesheetEntries.Select(x => x.InnateCodeTask.InnateCode))
            {
                if (!temp.Any(x => x.InnateCodeId == code.InnateCodeId))
                {
                    Debug.WriteLine($"** Loaded timesheet has inactive code: {code.GetCodeAsString()} -- adding to dropdown...");
                    temp.Add(code);
                }
            }

            // Remove codes that have been used on the timesheet already and have no tasks left
            var codesInUse = dataGridEntities.Select(x => x.InnateCodeTask).GroupBy(x => x.InnateCode);
            foreach (var code in codesInUse.Select(x => x.Key))
            {
                // Match code in use to active code in initial source
                var match = temp.FirstOrDefault(x => x.InnateCodeId == code.InnateCodeId);
                if (match != null)
                {
                    // If all tasks for this code are in use then remove the code from the dropdown source
                    if (match.Tasks.Count == codesInUse.FirstOrDefault(x => x.Key == code)?.Count())
                    {
                        temp.Remove(match);
                    }
                }
            }

            Debug.WriteLine($"** Populate code dropdown with {temp.Count} tasks");
            innateCodeDropdownSource = temp;
            OnInnateCodeChanged(null);
        }

        /// <summary>
        /// Should this user be allowed to view the timesheet. Only superusers, the owner or the line manager.
        /// </summary>
        /// <returns></returns>
        private bool IsPermittedToViewTimesheetDetailsPage()
        {
            return (timesheet?.IsOwner(ActiveUser) ?? false) ||
                (timesheet?.IsLineManager(ActiveUser) ?? false) ||
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

            InvokeAsync(StateHasChanged);
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

        /// <summary>
        /// Method fired when the timesheet note is changed
        /// </summary>
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
            // Prompt
            var confirmed = await DialogService.Confirm($"By continuing you will change the status of this timesheet to \"{newStatus}\".",
                "Change Timesheet Status") ?? false;
            if (!confirmed) return;
            timesheet.Status = newStatus;

            // Reset error message
            ErrorMessage = null;

            // Validation on minimum hours etc. and show a status message
            if ((timesheet.Status == TimesheetStatus.Submitted || SubmittingAsSelfApprover()) && dataGridEntities.Count == 0)
            {
                ErrorMessage = new StatusMessage("You must have at least one entry in your timesheet to submit it!", StatusMessage.MessageType.Error);
                timesheet.Status = TimesheetStatus.New;
                return;
            }

            // Decide what to do based on the status of the timesheet (if being submitted or saved or self-approved)
            if (timesheet.Status == TimesheetStatus.New || timesheet.Status == TimesheetStatus.Submitted || SubmittingAsSelfApprover())
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
                    if (entry.TotalHours == 0 && (timesheet.Status == TimesheetStatus.Submitted || SubmittingAsSelfApprover()))
                    {
                        LogInformation($"Removing blank timesheet entry from DB for {entry.GetSensibleObjectName()}...");
                        TimesheetService.DeleteEntry(Context, entry, false);
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
                timesheet.StatusChangedBy = ActiveUser;
            }

            // Save to database
            LogInformation($"Saving timesheet {timesheet.CreatedDate.ToShortDateString()} for {timesheet.Owner.Name}. New status = {timesheet.Status.ToNiceString()}...");
            TimesheetService.Update(Context, timesheet);
            TimesheetService.GetIssueCount(Context, ActiveUser.PersonId);

            // Send an email to the Line manager if it's the user submitting their timesheet (and not self approving)
            if (timesheet.Owner == ActiveUser)
            {
                Debug.Write("** Sending an email to the Line Manager...");
                EmailService.SendTimesheetSubmissionEmailNotification(ActiveUser, timesheet);
            }

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
        /// Check whether the active user is a self approver of this timesheet and has just changed the status to Approved
        /// </summary>
        /// <returns></returns>
        private bool SubmittingAsSelfApprover()
        {
            return timesheet.IsSelfApprover(ActiveUser) && timesheet.Status == TimesheetStatus.Approved;
        }

        /// <summary>
        /// Handle a change in the code on the first dropdown
        /// </summary>
        /// <param name="value"></param>
        private void OnInnateCodeChanged(object value)
        {
            // If value is null then just clear the lists
            if (value == null)
            {
                Debug.WriteLine($"** Clearing task list");
                innateCodeTaskDropdownSource = new List<InnateCodeTask>();
                return;
            }

            // Load the innate tasks associated with the selected innate code
            Debug.WriteLine($"** Selected {value}");
            var tasks = innateCodeDropdownSource
                .FirstOrDefault(x => x.GetCodeAsString() == (value as string))?.Tasks
                .ToList();

            // Find all existing entries that use this same code
            var tasksInUse = dataGridEntities
                .Where(x => x.InnateCodeTask.InnateCode.GetCodeAsString() == (value as string))
                .Select(x => x.InnateCodeTask)
                .ToList();

            // Remove the tasks from the list that are already in use
            tasks?.RemoveAll(x => tasksInUse.Contains(x));

            // Assign the tasks
            innateCodeTaskDropdownSource = tasks;
            Debug.WriteLine($"** {tasks.Count} tasks in list");

            // If there is only one task then select it
            if (tasks.Count == 1)
            {
                entityToInsert.InnateCodeTask = tasks.First();
            }

            // Force a re-render
            StateHasChanged();
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnCreateRow(TimesheetEntry entity)
        {
            LogInformation($"Add row to database for <{entity?.GetSensibleObjectName()}>");
            TimesheetService.AddEntry(Context, entity);
            TimesheetService.AddToTemplate(Context, ActiveUser, entity.InnateCodeTask);

            ShowNotification(new CapXNotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Updated",
                Detail = "Your timesheet template has been updated. The added task row will show when you next create a new timesheet."
            });
            LoadInnateCodes();
        }

        /// <summary>
        /// Do not track TimesheetEntry changes with Timesheet service so override
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(TimesheetEntry entity)
        {
            LogInformation($"Update row in database for <{entity?.GetSensibleObjectName()}>");
            TimesheetService.UpdateEntry(Context, entity);
        }

        /// <summary>
        /// Necessary override since Timesheet and TimesheetEntry entities are edited on the same page
        /// </summary>
        /// <param name="entity"></param>
        protected override void CancelEdit(TimesheetEntry entity)
        {
            LogInformation($"Restore model and cancel edit row in view for <{entity?.GetSensibleObjectName()}>");
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

            // Refresh the code and task dropdowns
            LoadInnateCodes();
        }

        /// <summary>
        /// Override to make sure we add the timesheet reference to the entry
        /// </summary>
        /// <returns></returns>
        protected override async Task InsertRow()
        {
            await base.InsertRow();
            entityToInsert.Timesheet = timesheet;
            LogInformation($"(Override) Add row in view for <{entityToInsert?.GetSensibleObjectName()}>");
        }

        /// <summary>
        /// Remove the entity from the DB table
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task DeleteRow(TimesheetEntry entity)
        {
            bool confirmDeletion = await DialogService.Confirm($"This task will be removed from your timesheet template. If you want the task to still be added to future timesheets automatically (but just don't need it for this one) then just leave it empty when you submit the timesheet.",
                   "Delete Task Row") ?? false;
            if (confirmDeletion)
            {
                TimesheetService.DeleteFromTemplate(Context, ActiveUser, entity.InnateCodeTask);
                TimesheetService.DeleteEntry(Context, entity);
                await base.DeleteRow(entity);
                UpdateDailyTotals();
                LoadInnateCodes();

                ShowNotification(new CapXNotificationMessage
                {
                    Severity = NotificationSeverity.Error,
                    Summary = "Task removed",
                    Detail = "The task row has been removed from your template and will no longer show by default when creating a new timesheet."
                });
            }
        }

        /// <summary>
        /// Fired when a cell of the datagrid is rendered. Used to set the colour styling of the cells.
        /// </summary>
        /// <param name="args"></param>
        private void CellRender(DataGridCellRenderEventArgs<TimesheetEntry> args)
        {
            if (args != null)
            {
                if (args.Column.Property != null)
                {
                    string theDay = args.Column.Title.ToLower().Trim();
                    if (DayColours.ContainsKey(theDay))
                    {
                        args.Attributes.Add("style", $"background-color : {DayColours[theDay]}");
                    }

                    if (args.Column.Property == "IsInTemplate" && args.Data.IsInTemplate == true)
                    {
                        args.Attributes.Add("style", $"background-color :  var(--rz-panel-menu-item-2nd-level-active-background-color)");
                        args.Attributes.Add("title", "Task is part of your default template");
                    }
                }
            }
        }

        /// <summary>
        /// Method to check the valid entered into an input and correct it if not in the allowable set of values
        /// </summary>
        /// <param name="value"></param>
        /// <param name="entry"></param>
        /// <param name="propertyName"></param>
        private void ValidateNumericInput(double value, TimesheetEntry entry, string propertyName)
        {
            // Ensure the value is within the range and adheres to the step
            var hasBeenCorrected = false;
            var idealValue = Math.Round(value / entryStep) * entryStep;
            if (value < entryMinimum)
            {
                value = entryMinimum;
                hasBeenCorrected = true;
            }
            else if (idealValue != value)
            {
                value = idealValue;
                hasBeenCorrected = true;
            }

            // Update the property with the validated value
            if (hasBeenCorrected)
            {
                switch (propertyName)
                {
                    case nameof(entry.MondayHours):
                        entry.MondayHours = value;
                        break;
                    case nameof(entry.TuesdayHours):
                        entry.TuesdayHours = value;
                        break;
                    case nameof(entry.WednesdayHours):
                        entry.WednesdayHours = value;
                        break;
                    case nameof(entry.ThursdayHours):
                        entry.ThursdayHours = value;
                        break;
                    case nameof(entry.FridayHours):
                        entry.FridayHours = value;
                        break;
                    case nameof(entry.SaturdayHours):
                        entry.SaturdayHours = value;
                        break;
                    case nameof(entry.SundayHours):
                        entry.SundayHours = value;
                        break;
                }

                // Show a notification to the user that their value has been corrected
                ShowNotification(new CapXNotificationMessage
                {
                    Severity = NotificationSeverity.Info,
                    Summary = "Value Adjusted",
                    Detail = $"Value must be greater than {entryMinimum} and in steps of {entryStep}. Value has been corrected."
                });
            }

            // Update the daily totals regardless of correction
            UpdateDailyTotals();
        }

        /// <summary>
        /// Navigate to timesheet
        /// </summary>
        /// <param name="timesheet"></param>
        public void GoToTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"timesheets/addtimesheet/{timesheet.TimesheetId}");
        }

        /// <summary>
        /// Opens the dialog with the ReorderTimesheet page as its content. The passed timesheet
        /// is only so we can return and reload the correct page that we came from
        /// </summary>
        /// <param name="timesheet"></param>
        private async void ReorderTimesheetTemplate(Timesheet timesheet)
        {
            await DialogService.OpenAsync<ReorderTimesheet>("Drag and drop tasks to reorder them",
               new Dictionary<string, object>()
               {
                   { "TimesheetId", timesheet.TimesheetId },
                   { nameof(ReorderTimesheet.FormClosed), () => FormClosedHandler() }
               },
               new DialogOptions()
               {
                   ShowClose = false,
                   Width = "50%"
               });
        }

        /// <summary>
        /// Callback which runs when the form closes
        /// </summary>
        private void FormClosedHandler()
        {
            // Force a reload of the page - seems the only way to be sure that we're getting the fresh data!
            Navigation.NavigateTo($"/timesheets/addtimesheet/{timesheet.TimesheetId.ToString()}", true);
        }
    }
}
