using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class AddTimesheet : BasePage
    {
        /// <summary>
        /// ID of the timesheet to edit if applicable
        /// </summary>
        [Parameter]
        public int? TimesheetId { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        private Timesheet timesheet = new Timesheet();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // If there is an ID, then lookup the timesheet
            if ((TimesheetId ?? 0) > 0)
            {
                timesheet = TimesheetService.GetById(Context, TimesheetId);
            }
            else
            {
                timesheet = new Timesheet();
            }
        }

        /// <summary>
        /// Add a new entry to the timesheet
        /// </summary>
        private void AddEntry()
        {
            timesheet.TimesheetEntries.Add(new TimesheetEntry());
        }

        /// <summary>
        /// Remove the entry from the current timesheet
        /// </summary>
        /// <param name="entry"></param>
        private void RemoveEntry(TimesheetEntry entry)
        {
            timesheet.TimesheetEntries.Remove(entry);
        }

        /// <summary>
        /// Discard changes
        /// </summary>
        private void DiscardTimesheet()
        {
            Navigation.NavigateTo("/timesheets");
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
        private void HandleSubmit()
        {
            // TODO: Some kind of validation and show a status message
            ErrorMessage = null;

            if (TimesheetId > 0)
            {
                TimesheetService.Update(Context, timesheet);
            }
            else
            {
                TimesheetService.Add(Context, timesheet);

            }
            Navigation.NavigateTo("/timesheets");
        }
    }
}
