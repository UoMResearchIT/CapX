using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class Timesheets : BaseProjectPage
    {
        [Inject]
        private TimesheetService TimesheetService { get; set; }

        //[Parameter]
        //[SupplyParameterFromQuery(Name = "pm")]
        //public string ProjectManagerShortName { get; set; }

        private IDictionary<Project, IEnumerable<Note>> ownedProjectsAndDueNotes;
        private Role userRole;

        protected override void OnInitialized()
        {
            base.OnInitialized();
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
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

        }

        private void LoadProjectData(bool initial)
        {
            // Initialise the project list
            List<Timesheet> sheets = TimesheetService.GetAll(Context).OrderBy(x => x.StartDate).ToList();

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();
        }
    }
}
