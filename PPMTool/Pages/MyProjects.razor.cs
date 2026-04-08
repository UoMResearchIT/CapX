using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
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
                    Task.Run(() => LoadProjectDataAsync(false));
                }
            }
        }

        [Parameter]
        [SupplyParameterFromQuery(Name = "pm")]
        public string ProjectManagerShortName { get; set; }

        private IDictionary<Project, IEnumerable<Note>> ownedProjectsAndDueNotes;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LogInformation("Viewing my projects");
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Load settings the first time
            if (firstRender)
            {
                // Navigate away if not a manager
                if (ActiveUserRoleType != RoleType.Superuser && ActiveUserRoleType != RoleType.Manager)
                {
                    LogInformation($"Role is not Manager or Superuser, redirecting...");

                    if (ActiveUserRoleType == RoleType.Developer || ActiveUserRoleType == RoleType.Reader)
                    {
                        Navigation.NavigateTo("capacity");
                    }
                    else if (ActiveUserRoleType == RoleType.Finance)
                    {
                        Navigation.NavigateTo("managefinancialitems/summary");
                    }
                    else
                    {
                        Navigation.NavigateTo("datadashboard");
                    }
                }

                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("my-project-show-active");

                // Load data
                await LoadProjectDataAsync(true);
            }
        }

        private async Task LoadProjectDataAsync(bool initial)
        {
            Loading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            // Initialise the project list
            var proj = ProjectService
                .GetAll(Context)
                .OrderBy(x => x.RTP)
                .Where(x => includeFinished ? true : !x.ProjectStatus.IsFinishedOrCancelled());

            // Extract the owned projects and their due notes
            if (ProjectManagerShortName != null)
            {
                if (ProjectManagerShortName.ToLower() == "alerts")
                {
                    // Show just the list of alerts for all
                    proj = proj.Where(x =>
                    {
                        x.GetLatestStatusMessages();
                        return x.HasActiveStatusMessages();
                    }).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    proj = proj.Where(x =>
                    {
                        x.GetLatestStatusMessages();
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
                proj = proj.Where(x => x.ProjectManager?.PersonId == ActiveUser?.Person?.PersonId).ToList();
            }

            // Build the dictionary
            ownedProjectsAndDueNotes = new Dictionary<Project, IEnumerable<Note>>();
            foreach (var p in proj)
            {
                ownedProjectsAndDueNotes.Add(p, NoteService.GetDueNotesForProject(Context, p.ProjectId));
            }

            // Disable spinner now load complete
            Loading = false;
            await InvokeAsync(StateHasChanged);

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
