// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using PPMTool.Services.StatusEvaluators;
using Radzen.Blazor;

namespace PPMTool.Pages
{
    public partial class MyProjects : BaseProjectPage
    {
        [Inject]
        private NoteService NoteService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private ProjectStatusEvaluator ProjectStatusEvaluator { get; set; }

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
            await base.OnAfterRenderAsync(firstRender);

            // Load settings the first time
            if (firstRender)
            {
                // Navigate away if feature not enabled
                if (!FeatureService.IsFeatureEnabled(FeatureType.ProjectsAndCapacity))
                {
                    Navigation.NavigateTo("people");
                    return;
                }

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
                    return;
                }

                // Get switch setting
                includeFinished = await SessionStorage.GetItemAsync<bool>("my-project-show-active");

                // Load data
                await LoadProjectDataAsync(true);
            }
        }

        /// <summary>
        /// Determines whether the specified project should be shown in the list for the active user or a specified short name.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="shortName"></param>
        /// <returns></returns>
        private bool ShouldShowInList(Project project, string shortName = null)
        {
            // If no user then always false
            if (ActiveUser == null || ActiveUser.Person == null)
            {
                return false;
            }

            // PersonId to match to
            var personIdToMatch = ActiveUser.Person.PersonId;
            if (shortName != null)
            {
                // If there is no matching user then set to zero so comparison will fail anyway
                personIdToMatch = PersonService.GetByShortName(Context, shortName)?.PersonId ?? 0;
            }

            // Check if should visible due to PM status
            bool isProjectManager = false;
            if (project.ProjectManager?.PersonId == personIdToMatch)
            {
                isProjectManager = true;
            }

            // Check if should visible due to request owner status
            bool isRequestOwner = false;
            if (project.RequestOwnerId == personIdToMatch && project.ProjectStatus == ProjectStatus.NewRequest)
            {
                isRequestOwner = true;
            }

            // One or the other needs to be true to show in the list
            return isProjectManager || isRequestOwner;
        }

        /// <summary>
        /// Loads the project data and their due notes into the ownedProjectsAndDueNotes dictionary.
        /// </summary>
        /// <param name="initial"></param>
        /// <returns></returns>
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
                    proj = proj.Where(x => ProjectStatusEvaluator.HasActiveStatusMessages(x)).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    proj = proj.Where(x => ProjectStatusEvaluator.HasActiveErrorMessages(x)).ToList();
                }
                else
                {
                    // Use query string to see someone else's list of cards
                    proj = proj.Where(x => ShouldShowInList(x, ProjectManagerShortName.ToLower())).ToList();
                }
            }
            else
            {
                // Show just the logged in user's projects
                proj = proj.Where(x => ShouldShowInList(x)).ToList();
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
