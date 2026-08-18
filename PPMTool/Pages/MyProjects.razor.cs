// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Models;
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
                    SessionStorage.SetItemAsync($"{GetStorageTag()}-show-active", includeFinished);
                    InvokeAsync(async () => await LoadProjectDataAsync(false));
                }
            }
        }

        private bool showRequestSummary;
        public bool ShowRequestSummary
        {
            get
            {
                return showRequestSummary;
            }
            set
            {
                if (showRequestSummary != value)
                {
                    showRequestSummary = value;
                    SessionStorage.SetItemAsync($"{GetStorageTag()}-show-request-summary", showRequestSummary);
                    InvokeAsync(async () => await LoadProjectDataAsync(false));
                }
            }
        }

        [Parameter]
        [SupplyParameterFromQuery(Name = "pm")]
        public string ProjectManagerShortName { get; set; }

        private IDictionary<Project, IEnumerable<Note>> ownedProjectsAndDueNotes;
        private int? personIdToMatch;
        private IList<RequestClockSummary> summaries = new List<RequestClockSummary>();

        protected override string GetStorageTag() => "my-project";

        protected override void OnInitialized()
        {
            base.OnInitialized();
            personIdToMatch = ActiveUser?.Person?.PersonId;
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
                includeFinished = await SessionStorage.GetItemAsync<bool>($"{GetStorageTag()}-show-active");
                showRequestSummary = await SessionStorage.GetItemAsync<bool>($"{GetStorageTag()}-show-request-summary");

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

            // PersonId to match to, start by resetting to default
            personIdToMatch = ActiveUser.Person.PersonId;
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
            var allProjects = ProjectService
                .GetAll(Context)
                .OrderBy(x => x.RTP)
                .Where(x => includeFinished ? true : !x.ProjectStatus.IsFinishedOrCancelled());
            var filteredProjects = allProjects;

            // Extract the owned projects and their due notes
            if (ProjectManagerShortName != null)
            {
                if (ProjectManagerShortName.ToLower() == "alerts")
                {
                    // Show just the list of alerts for all
                    filteredProjects = filteredProjects.Where(x => ProjectStatusEvaluator.HasActiveStatusMessages(x)).ToList();
                }
                else if (ProjectManagerShortName.ToLower() == "errors")
                {
                    // Show just the list of errors for all
                    filteredProjects = filteredProjects.Where(x => ProjectStatusEvaluator.HasActiveErrorMessages(x)).ToList();
                }
                else
                {
                    // Use query string to see someone else's list of cards
                    filteredProjects = filteredProjects.Where(x => ShouldShowInList(x, ProjectManagerShortName.ToLower())).ToList();
                }
            }
            else
            {
                // Show just the logged in user's projects
                filteredProjects = filteredProjects.Where(x => ShouldShowInList(x)).ToList();
            }

            // Build the dictionary
            ownedProjectsAndDueNotes = new Dictionary<Project, IEnumerable<Note>>();
            foreach (var p in filteredProjects)
            {
                ownedProjectsAndDueNotes.Add(p, NoteService.GetDueNotesForProject(Context, p.ProjectId));
            }

            // Load the request summary widget
            if (ShowRequestSummary)
            {
                // Clear the current list
                summaries.Clear();

                // Get all projects that are new requests
                var requests = allProjects.Where(x => x.ProjectStatus == ProjectStatus.NewRequest);

                // Group by person
                var groupedRequests = requests.GroupBy(x => x.RequestOwner?.Name ?? "Not Set");

                // Map to the chart objects
                foreach (var group in groupedRequests)
                {
                    var mappedValues = group.Select(x => ProjectService.GetRequestClockDetails(x.CreatedDate));
                    summaries.Add(
                        new RequestClockSummary(
                            group.Key,
                            mappedValues.Count(x => x.ShouldError()),
                            mappedValues.Count(x => x.ShouldWarn()),
                            mappedValues.Count(x => !x.ShouldError() && !x.ShouldWarn())
                        )
                    );
                }

                // Sort the resulting data
                summaries = summaries
                    .OrderByDescending(x => x.RedCount)
                    .ThenByDescending(x => x.AmberCount)
                    .ThenByDescending(x => x.TotalCount)
                    .ThenBy(x => x.RequestOwner)
                    .ToList();
            }

            // Disable spinner now load complete
            Loading = false;
            await InvokeAsync(StateHasChanged);

            Debug.WriteLine($"** {filteredProjects.Count()} projects loaded. Initial load = {initial}");
        }

        /// <summary>
        /// Formatter for the data labels for the request summary chart
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string FormatSummaryDataLabels(object val)
        {
            // Don't show zero labels as they are confusing
            return Convert.ToInt16(val) == 0 ? "" : val.ToString();
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
