using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer,Reader")]
    public partial class Projects : BaseProjectPage
    {
        private IEnumerable<Project> projects;
        private Role userRole;

        private bool includeFinished;
        public bool IncludeFinished
        {
            get
            {
                return includeFinished;
            }
            set
            {
                if (includeFinished != value)
                {
                    includeFinished = value;
                    SessionStorage.SetItemAsync("project-show-active", includeFinished);
                    LoadProjectData(false);
                }
            }
        }

        private DataGridSettings settings;
        public DataGridSettings Settings
        {
            get
            {
                return settings;
            }
            set
            {
                if (settings != value)
                {
                    settings = value;
                    SessionStorage.SetItemAsync("project-settings", settings);
                }
            }
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;

            // Look up the username
            var uname = AuthenticationState.User.Identity.Name.Trim().ToLower();
            userRole = RolesService.GetByUsername(Context, uname);

            // Log any time there is no role returned?
            if (userRole == null)
            {
                LogError($"{uname}: Role is null!");
            }
            LogInformation("Viewing project grid");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Load settings the first time
            if (firstRender)
            {
                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("project-show-active");

                // Load data
                LoadProjectData(true);

                // Get the grid settings
                Debug.WriteLine($"** Loading saved session settings for the grid...");
                await LoadSettingsAsync();
            }
        }

        private async Task LoadSettingsAsync()
        {
            settings = await SessionStorage.GetItemAsync<DataGridSettings>("project-settings");
            StateHasChanged();
        }

        private void LoadProjectData(bool initial)
        {
            // Initialise the project list -- developers can only see projects to which they are assigned
            List<Project> proj;
            if (userRole.RoleType == RoleType.Developer)
            {
                proj = ProjectService.GetAll(Context)
                    .Where(x => x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person == userRole.Person)))
                    .OrderBy(x => x.RTP).ToList();
            }
            else
            {
                proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();
            }

            // Remove the ones that are not active if necessary
            if (!includeFinished) proj = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled()).ToList();

            // Assign data for the data grid
            projects = proj;

            // Disable spinner now load complete
            Loading = false;

            Debug.WriteLine($"** {proj.Count()} projects loaded. Initial load = {initial}");
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }
    }
}
