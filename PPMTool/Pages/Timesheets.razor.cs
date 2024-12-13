using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class Timesheets : BasePage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        private Role activeUserRole;
        private bool hideStaffResults = true;
        private bool showAllMyTimesheets;
        private DateTime dateNextTimesheet;
        private List<Timesheet> myTimesheets;
        private List<Timesheet> myStaffTimesheets;
        private TaskQueue taskQueue = new TaskQueue();

        public bool ShowAllMyTimesheets
        {
            get => showAllMyTimesheets;
            private set
            {
                if (value != showAllMyTimesheets)
                {
                    showAllMyTimesheets = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-showall-mine", showAllMyTimesheets);
                    EnqueueLoadData();
                }
            }
        }

        private bool showAllMyStaffTimesheets;
        public bool ShowAllMyStaffTimesheets
        {
            get => showAllMyStaffTimesheets;
            private set
            {
                if (value != showAllMyStaffTimesheets)
                {
                    showAllMyStaffTimesheets = value;
                    SessionStorage.SetItemAsync<bool?>("timesheets-showall-reports", showAllMyStaffTimesheets);
                    EnqueueLoadData();
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Look up the username
            var uname = AuthenticationState.User.Identity.Name.Trim().ToLower();
            activeUserRole = RolesService.GetByUsername(Context, uname);

            // Log any time there is no role returned?
            if (activeUserRole == null)
            {
                LogError($"{uname}: Role is null!");
            }

            LogInformation("Viewing Timesheets");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender) return;

            // Load state from session storage and once finished, load the data
            var temp = await SessionStorage.GetItemAsync<bool?>("timesheets-showall-mine");
            if (temp != null) ShowAllMyTimesheets = temp ?? false;
            temp = await SessionStorage.GetItemAsync<bool?>("timesheets-showall-reports");
            if (temp != null) ShowAllMyStaffTimesheets = temp ?? false;
            EnqueueLoadData();
        }

        /// <summary>
        /// Put a load data request into the queue
        /// </summary>
        private void EnqueueLoadData()
        {
            _ = taskQueue.Enqueue(async () =>
            {
                await LoadData();
            });
        }

        /// <summary>
        /// Load in the timesheet data from the service
        /// </summary>
        /// <param name="showAll"></param>
        private async Task LoadData()
        {
            Loading = true;
            await InvokeAsync(StateHasChanged);

            // Get ALL timesheets for the user, then filter stuff out based the state of the ShowAll switch. 
            myTimesheets = new List<Timesheet>(); // Initialise the list
            myTimesheets = TimesheetService.GetMyTimesheets(Context, activeUserRole.Person).OrderByDescending(t => t.StartDate).ToList();

            // Set the start date for the next one
            dateNextTimesheet = TimesheetService.GetNextTimesheetStartDateForUser(Context, activeUserRole.Person);

            if (!ShowAllMyTimesheets)
            {
                // Remove items with Submitted or Approved status
                myTimesheets = myTimesheets.Where(t => t.Status != TimesheetStatus.Submitted && t.Status != TimesheetStatus.Approved).ToList();
            }

            // Show second grid if user manages staff - need to see the timesheets they have submitted.
            if (activeUserRole.Person.PeopleManaged.Count > 0)  // Is a manager
            {
                hideStaffResults = false;  // Show/Hide the second grid based on this
                myStaffTimesheets = new List<Timesheet>();

                foreach (Person p in activeUserRole.Person.PeopleManaged)
                {
                    myStaffTimesheets.AddRange(TimesheetService.GetMyTimesheets(Context, p).ToList());
                }

                if (!ShowAllMyStaffTimesheets)
                {
                    // Filter the list to only show items with Submitted status
                    myStaffTimesheets = myStaffTimesheets.Where(t => t.Status == TimesheetStatus.Submitted).ToList();
                }

                // Order the list, whatever it holds (but remove any New items as these haven't been submitted by the staff member yet!)
                myStaffTimesheets = myStaffTimesheets.Where(t => t.Status != TimesheetStatus.New).OrderByDescending(t => t.StartDate).ToList();
            }

            Loading = false;
            await InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Add a new timesheet
        /// </summary>
        void AddTimesheet()
        {
            Navigation.NavigateTo("addtimesheet/-1");
        }

        /// <summary>
        /// Navigate to the specific timesheet to view/edit it
        /// </summary>
        private void EditTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"addtimesheet/{timesheet.TimesheetId}");
        }
    }
}
