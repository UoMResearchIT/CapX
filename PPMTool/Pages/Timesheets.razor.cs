using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Formatters;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class Timesheets : BaseProjectPage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        private Role userRole;
        private bool hideStaffResults = true;
        private Dictionary<TimesheetStatus, BadgeStyle> mapBadgeStyle = new Dictionary<TimesheetStatus, BadgeStyle>();

        private bool showAllMyTimesheets;
        public bool ShowAllMyTimesheets
        {
            get => showAllMyTimesheets;
            private set
            {
                if (value != showAllMyTimesheets)
                {
                    showAllMyTimesheets = value;
                    LoadData();
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
                    LoadData();
                }
            }
        }

        private List<Timesheet> myTimesheets;
        private List<Timesheet> myStaffTimesheets;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Loading = true;

            // Populate the dictionary used by the status badges in the grid
            if (mapBadgeStyle.Count == 0)
            {
                mapBadgeStyle.Add(TimesheetStatus.New, BadgeStyle.Info);
                mapBadgeStyle.Add(TimesheetStatus.Submitted, BadgeStyle.Primary);
                mapBadgeStyle.Add(TimesheetStatus.Rejected, BadgeStyle.Danger);
                mapBadgeStyle.Add(TimesheetStatus.Approved, BadgeStyle.Success);
            }

            // Look up the username
            var uname = AuthenticationState.User.Identity.Name.Trim().ToLower();
            userRole = RoleService.GetByUsername(Context, uname);

            // Log any time there is no role returned?
            if (userRole == null)
            {
                LogError($"{uname}: Role is null!");
            }

            LogInformation("Viewing myTimesheets");
            LoadData();
        }

        /// <summary>
        /// Load in the timesheet data from the service
        /// </summary>
        /// <param name="showAll"></param>
        private void LoadData()
        {
            // Get ALL timesheets for the user, then filter stuff out based the state of the ShowAll switch. 
            myTimesheets = new List<Timesheet>(); // Initialise the list
            myTimesheets = TimesheetService.GetMyTimesheets(Context, userRole.Person).OrderByDescending(t => t.StartDate).ToList();

            if (!ShowAllMyTimesheets)
            {
                // Remove items with Submitted or Approved status
                myTimesheets = myTimesheets.Where(t => t.Status != TimesheetStatus.Submitted && t.Status != TimesheetStatus.Approved).ToList();
            }

            // Show second grid if user manages staff - need to see the timesheets they have submitted.
            if (userRole.Person.PeopleManaged.Count > 0)  // Is a manager
            {
                hideStaffResults = false;  // Show/Hide the second grid based on this
                myStaffTimesheets = new List<Timesheet>();

                foreach (Person p in userRole.Person.PeopleManaged)
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
