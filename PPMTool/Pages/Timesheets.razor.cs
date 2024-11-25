using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class Timesheets : BaseProjectPage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        private Role userRole;
        private Dictionary<TimesheetStatus, BadgeStyle> mapBadgeStyle = new Dictionary<TimesheetStatus, BadgeStyle>();

        private bool showAll;
        public bool ShowAll
        {
            get => showAll;
            private set
            {
                if (value != showAll)
                {
                    showAll = value;
                    LoadData(ShowAll);
                }
            }
        }

        private IEnumerable<Timesheet> timesheets;

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

            if (userRole.RoleType != RoleType.Superuser && userRole.RoleType != RoleType.Manager && userRole.RoleType != RoleType.Developer)
            {
                LogInformation($"{uname}: Role is not Manager, Superuser or Developer. Redirecting...");
                Navigation.NavigateTo("/capacity");
            }

            LogInformation("Viewing timesheets");
            LoadData(ShowAll);
        }

        private void LoadData(bool showAll)
        {
            // PHB [24/11/24] : Only currently need to show New or Rejected timesheets to be available to Edit. 
            // Will need to take into account timesheets of direct reports when management hierarchy is
            // available in the database. Perhaps show a second datagrid for those of staff and
            // show/hide based on user's role?
            timesheets = TimesheetService.GetAll(Context).OrderByDescending(x => x.StartDate).ToList();
            timesheets = timesheets.Where(x => x.Owner?.PersonId == userRole.Person?.PersonId).ToList();

            if (!ShowAll)
            {
                timesheets = timesheets.Where(t => t.Status != TimesheetStatus.Submitted && t.Status != TimesheetStatus.Approved).ToList();
            }

            Loading = false;
        }

        void AddTimesheet()
        {
            Navigation.NavigateTo("/addtimesheet/");

            //// Calculate the date for the next timesheet
            //timesheets = timesheets.OrderBy(x => x.StartDate);
            //DateTime lastTimesheetDate = timesheets.Last().StartDate;

            //DateTime nextTimesheetStartDate = lastTimesheetDate.AddDays(7);
            //Timesheet newTimesheet = new Timesheet();
            //newTimesheet.Person = userRole.Person;
            //newTimesheet.StartDate = nextTimesheetStartDate;
            //newTimesheet.Status = TimesheetStatus.New;

            //Debug.WriteLine($"New timesheet start date = {nextTimesheetStartDate.ToString("dd/MM/yyyy")}");
        }
    }
}
