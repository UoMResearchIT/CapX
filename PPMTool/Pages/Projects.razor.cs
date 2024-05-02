using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private RolesService RoleService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "pm")]
        public string ProjectManagerShortName { get; set; }

        private IEnumerable<Project> projects;
        private IEnumerable<Project> ownedProjects;
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
            loading = true;

            // Look up the username
            var uname = AuthenticationState.User.Identity.Name.Trim().ToLower();
            userRole = RoleService.GetByUsername(context, uname);

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
            // Get projects from the database
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

            // Only show projects to developers that they are assigned to
            if (!EditAuthorised && userRole != null)
            {
                proj = proj.Where(x => x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person == userRole.Person))).ToList();
            }

            // Remove the ones that are not active if necessary
            if (!includeFinished) proj = proj.Where(x => !x.ProjectStatus.IsProjectFinishedOrCancelled()).ToList();

            // Extract the owned projects
            if (ProjectManagerShortName != null)
            {
                if (ProjectManagerShortName.ToLower() == "alerts")
                {
                    // Show just the list of alerts for all
                    ownedProjects = proj.Where(x => x.HasActiveStatusMessages()).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    ownedProjects = proj.Where(x => x.HasErrorMessages()).ToList();
                }
                else
                {
                    // Use query string to see someone else's list of cards
                    ownedProjects = proj.Where(x => x.ProjectManager?.ShortName.ToLower() == ProjectManagerShortName.ToLower()).ToList();
                }
            }
            else
            {
                // Show just the logged in user's projects
                ownedProjects = proj.Where(x => x.ProjectManager == userRole.Person).ToList();
            }

            // Update the summary of each project and save back to DB if initial load of the page
            if (initial && proj.Count > 0)
            {
                Debug.WriteLine($"** Updating project summary data for {proj.Count} project(s)...");
                for (int i = 0; i < proj.Count; ++i)
                {
                    var p = proj[i];
                    p.UpdateProjectSummary();
                    ProjectService.Update(context, p);
                }
            }

            // Assign data for the data grid
            projects = proj;

            // Disable spinner now load complete
            loading = false;

            Debug.WriteLine($"** {proj.Count()} projects loaded. Initial load = {initial}");
        }

        private async Task NavigateToProjectDetails(int id, bool newWindow = false)
        {
            if (newWindow)
            {
                await JSRuntime.InvokeAsync<object>("open", $"/projectdetails/{id}", "_blank");
            }
            else
            {
                Navigation.NavigateTo($"/projectdetails/{id}");
            }
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }

        private async Task DetailsButtonClicked(RadzenSplitButtonItem item, Project project)
        {
            if (item == null)
            {
                await NavigateToProjectDetails(project.ProjectId);
            }
            else if (item.Value == "NewWindow")
            {
                await NavigateToProjectDetails(project.ProjectId, true);
            }
            else if (item.Value == "Edit")
            {
                Navigation.NavigateTo($"/addproject/{project.ProjectId}");
            }
        }
    }
}
