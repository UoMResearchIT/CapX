using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class Timesheets : BaseProjectPage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        private Role userRole;

        RadzenDataGrid<Timesheet> timesheetsGrid;
        private IEnumerable<Timesheet> timesheets;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            Loading = true;

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


            timesheets = TimesheetService.GetAll(Context).OrderBy(x => x.StartDate).ToList();
            timesheets = timesheets.Where(x => x.Person?.PersonId == userRole.Person?.PersonId).ToList();
        }


        void AddTimesheet()
        {
            // Calculate the date for the next timesheet
            timesheets = timesheets.OrderBy(x => x.StartDate);
            DateTime lastTimesheetDate = timesheets.Last().StartDate;

            DateTime nextTimesheetStartDate = lastTimesheetDate.AddDays(7);
            Timesheet newTimesheet = new Timesheet();
            newTimesheet.Person = userRole.Person;
            newTimesheet.StartDate = nextTimesheetStartDate;
            newTimesheet.Status = TimesheetStatus.New;

            Debug.WriteLine($"New timesheet start date = {nextTimesheetStartDate.ToString("dd/MM/yyyy")}");
        }
    }
}
