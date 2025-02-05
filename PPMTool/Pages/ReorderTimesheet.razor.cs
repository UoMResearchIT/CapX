using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class ReorderTimesheet : DataGridPage<TimesheetEntry>
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
        private Role activeUserRole;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Loading = true;

            try
            {
                await Task.Run(() =>
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
                    if (timesheet != null && !CanEditTheTaskOrder())
                    {
                        timesheet = null;
                    }

                    if (timesheet != null)
                    {
                        dataGridEntities = timesheet.TimesheetEntries.OrderByDescending(e => e.InnateCodeTask.Duty.ToNiceString()).ThenBy(e => e.InnateCodeTask.TaskName).ToList();
                        Loading = false;
                    }
                });
            }
            catch (TaskCanceledException)
            {
                // We intend it to be cancelled so this is fine to ignore
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>
        /// Should this user be allowed to view the timesheet. Only superusers, the owner or the line manager.
        /// </summary>
        /// <returns></returns>
        private bool CanEditTheTaskOrder()
        {
            return (timesheet?.IsOwner(ActiveUser) ?? false);
        }

        /// <summary>
        /// Navigate to timesheet
        /// </summary>
        /// <param name="timesheet"></param>
        public void GoToTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"timesheets/addtimesheet/{timesheet.TimesheetId}");
        }
    }
}
