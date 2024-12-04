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
    public partial class MyProjects : BaseProjectPage
    {
        [Inject]
        private NoteService NoteService { get; set; }

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
                    SessionStorage.SetItemAsync("my-project-show-active", includeFinished);
                    LoadProjectData(false);
                }
            }
        }

        [Parameter]
        [SupplyParameterFromQuery(Name = "pm")]
        public string ProjectManagerShortName { get; set; }

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
                LogError($"Role is null!");
            }

            LogInformation("Viewing my projects");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Load settings the first time
            if (firstRender)
            {
                // Navigate away if not a manager
                if (userRole.RoleType != RoleType.Superuser && userRole.RoleType != RoleType.Manager)
                {
                    LogInformation($"Role is not Manager or Superuser, redirecting...");
                    Navigation.NavigateTo("capacity");
                }

                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("my-project-show-active");

                // Load data
                LoadProjectData(true);
            }
        }

        private void LoadProjectData(bool initial)
        {
            // Initialise the project list
            List<Project> proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();

            // Remove the ones that are not active if necessary
            if (!includeFinished) proj = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled()).ToList();

            // Extract the owned projects and their due notes
            if (ProjectManagerShortName != null)
            {
                if (ProjectManagerShortName.ToLower() == "alerts")
                {
                    // Show just the list of alerts for all
                    proj = proj.Where(x =>
                    {
                        x.UpdateStatusMessages();
                        return x.HasActiveStatusMessages();
                    }).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    proj = proj.Where(x =>
                    {
                        x.UpdateStatusMessages();
                        return x.HasActiveErrorMessages();
                    }).ToList();
                }
                else
                {
                    // Use query string to see someone else's list of cards
                    proj = proj.Where(x => x.ProjectManager?.ShortName.ToLower() == ProjectManagerShortName.ToLower()).ToList();
                }
            }
            else
            {
                // Show just the logged in user's projects
                proj = proj.Where(x => x.ProjectManager?.PersonId == userRole.Person?.PersonId).ToList();
            }

            // Build the dictionary
            ownedProjectsAndDueNotes = new Dictionary<Project, IEnumerable<Note>>();
            foreach (var p in proj)
            {
                ownedProjectsAndDueNotes.Add(p, NoteService.GetDueNotesForProject(Context, p.ProjectId));
            }

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();

            Debug.WriteLine($"** {proj.Count()} projects loaded. Initial load = {initial}");
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
