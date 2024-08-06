using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private RolesService RoleService { get; set; }

        [Inject]
        private NoteService NoteService { get; set; }

        [Inject]
        private ISessionStorageService SessionStorage { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "pm")]
        public string ProjectManagerShortName { get; set; }

        private IEnumerable<Project> projects;
        private IDictionary<Project, IEnumerable<Note>> ownedProjectsAndDueNotes;
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
            if (!includeFinished) proj = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled()).ToList();

            // Assign data for the data grid
            projects = proj;

            // Filter owned projects to only show active ones
            var tempProj = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled()).ToList();

            // Extract the owned projects and their due notes
            if (ProjectManagerShortName != null)
            {
                if (ProjectManagerShortName.ToLower() == "alerts")
                {
                    // Show just the list of alerts for all
                    tempProj = tempProj.Where(x =>
                    {
                        x.UpdateStatusMessages();
                        return x.HasActiveStatusMessages();
                    }).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    tempProj = tempProj.Where(x =>
                    {
                        x.UpdateStatusMessages();
                        return x.HasActiveErrorMessages();
                    }).ToList();
                }
                else
                {
                    // Use query string to see someone else's list of cards
                    tempProj = tempProj.Where(x => x.ProjectManager?.ShortName.ToLower() == ProjectManagerShortName.ToLower()).ToList();
                }
            }
            else
            {
                // Show just the logged in user's projects
                tempProj = tempProj.Where(x => x.ProjectManager == userRole.Person).ToList();
            }

            // Build the dictionary
            ownedProjectsAndDueNotes = new Dictionary<Project, IEnumerable<Note>>();
            foreach (var p in tempProj)
            {
                ownedProjectsAndDueNotes.Add(p, NoteService.GetDueNotesForProject(context, p.ProjectId));
            }

            // Disable spinner now load complete
            Loading = false;

            Debug.WriteLine($"** {proj.Count()} projects loaded. Initial load = {initial}");
        }

        private void NavigateToProjectDetails(int id, bool newWindow = false, bool filterDueNotes = false)
        {
            string url = $"/projectdetails/{id}";

            if (filterDueNotes)
            {
                url += "?filterDueNotes=true";
            }

            if (newWindow)
            {
                JSRuntime.InvokeAsync<object>("open", url, "_blank");
            }
            else
            {
                Navigation.NavigateTo(url);
            }
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }

        private void DetailsButtonClicked(RadzenSplitButtonItem item, Project project)
        {
            if (item == null)
            {
                NavigateToProjectDetails(project.ProjectId);
            }
            else if (item.Value == "NewWindow")
            {
                NavigateToProjectDetails(project.ProjectId, true);
            }
        }

        private void DueButtonClicked(Project project)
        {
            NavigateToProjectDetails(project.ProjectId, filterDueNotes: true);
        }
    }
}
