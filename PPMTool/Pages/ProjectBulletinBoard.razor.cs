// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Models;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Developer,Manager,Superuser,Reader")]
    public partial class ProjectBulletinBoard : BasePage
    {
        /// <summary>
        /// View mode for list of projects
        /// </summary>
        private enum ViewMode
        {
            All,
            Funded,
            Finished,
            Unmet
        }

        // Gives us the display order
        private readonly List<ProjectStatus> groupDisplayOrder = new List<ProjectStatus>
        {
            ProjectStatus.Active,
            ProjectStatus.Funded,
            ProjectStatus.Maintenance,
            ProjectStatus.Paused
            // Everything else I don't care about
        };

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SkillTagService SkillTagService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        private IEnumerable<ProjectWithAdditionalData> projectsToShow;
        private IEnumerable<IGrouping<ProjectStatus, ProjectWithAdditionalData>> projectsToShowGrouped;
        private IList<ProjectWithAdditionalData> allProjectsNotCancelled;
        private DateTime? startDate;
        private DateTime? endDate;
        private bool groupByStatus = true;
        private ViewMode viewMode = ViewMode.Funded;
        private IEnumerable<SkillTag> availableSkillsTags;
        private IEnumerable<int> selectedSkillTags = Enumerable.Empty<int>();

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();
            LogInformation("Viewing project bulletin board");

            // Set the loading flag and re-render to show spinner
            Loading = true;
            StateHasChanged();
            await Task.Yield();

            // Load the projects and filter based on options
            await LoadProjectDataAsync();
            await UpdateProjectsToShow();
        }

        private async Task UpdateProjectsToShow()
        {
            // Set the loading flag and re-render to show spinner
            Loading = true;
            StateHasChanged();
            await Task.Yield();

            // Filter the projects from the complete list
            FilterProjects();

            // Update the loading flag to re-render
            Loading = false;
            StateHasChanged();
        }

        /// <summary>
        /// Filter the projects based on the settings of the switches
        /// </summary>
        private void FilterProjects()
        {
            // If no projects have been loaded then set empty list
            if (allProjectsNotCancelled == null)
            {
                allProjectsNotCancelled = new List<ProjectWithAdditionalData>();
            }
            projectsToShow = allProjectsNotCancelled;

            // If we have both dates then ensure that the end date is not before the start date
            if (startDate != null && endDate != null)
            {
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }

            // Filter based on view mode
            if (viewMode != ViewMode.Unmet)
            {
                if (viewMode == ViewMode.Finished)
                {
                    // Just include finished
                    projectsToShow = allProjectsNotCancelled
                        .Where(x => x.Project.ProjectStatus == ProjectStatus.Finished);
                }
                else if (viewMode == ViewMode.Funded)
                {
                    // Just include funded and active
                    projectsToShow = allProjectsNotCancelled
                        .Where(x => x.Project.ProjectStatus.IsActive() || x.Project.ProjectStatus == ProjectStatus.Funded);
                }

                // Filter on date and order for all
                projectsToShow = projectsToShow
                    .Where(x => startDate != null ? x.Project.EndDate >= startDate : true)
                    .Where(x => endDate != null ? x.Project.StartDate <= endDate : true)
                    .OrderBy(x => x.Project.StartDate);
            }

            // Handle unmet differently so it maintains old behaviour
            else
            {
                // Get projects with unmet demand in the window and order by the start of the window then by the maximum unmet demand
                projectsToShow = allProjectsNotCancelled
                    .Where(x => x.Project.HasUnmetDemandInWindow(startDate, endDate))
                    .OrderBy(x =>
                    {
                        x.Project.GetUnmetDemandWindowDates(out var windowStart, out var windowEnd);
                        return windowStart;
                    })
                    .ThenByDescending(x => x.Project.SubTasks.Max(x => x.GetUnmetDemandInWindow(startDate, endDate)));
            }

            // Filter based on skills if the combination selected match the project
            if (selectedSkillTags?.Any() ?? false)
            {
                projectsToShow = projectsToShow
                    .Where(x => selectedSkillTags.All(tagId =>
                        x.Skills?.Any(s => s.SkillTagId == tagId) ?? false));
            }

            // Group the projects by status if the switch is set
            if (groupByStatus)
            {
                projectsToShowGrouped = projectsToShow
                    .GroupBy(x => x.Project.ProjectStatus)
                    .OrderBy(x =>
                    {
                        // Order by the display order if present in the display order list otherwise fallback to enum value
                        var index = groupDisplayOrder.IndexOf(x.Key);
                        return index >= 0 ? index : groupDisplayOrder.Count + (int)x.Key;
                    });
            }

            Debug.WriteLine($"** {projectsToShow?.Count()} projects filtered.");
        }

        /// <summary>
        /// Load all valid projects from the DB
        /// </summary>
        private async Task LoadProjectDataAsync()
        {
            // Initialise the list
            allProjectsNotCancelled = new List<ProjectWithAdditionalData>();

            // Get projects from the database into memory
            var proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();

            // Filter to just projects that are not cancelled or new requests
            var projects = proj.Where(x => !x.ProjectStatus.IsCancelled() && x.ProjectStatus != ProjectStatus.NewRequest);

            // Convert to new object type and load the skills and people on the fly
            foreach (var project in projects)
            {
                allProjectsNotCancelled.Add(new ProjectWithAdditionalData
                {
                    Project = project,
                    Skills = SkillTagService.GetSkillsForProject(Context, project.ProjectId),
                    AssignedResourceNames = (await SubTaskService.GetUniqueResourcesAsync(Context, project.ProjectId)).Where(x => x != project.ProjectManager?.Name)
                });
            }

            // Populate the available skills tags for filtering
            availableSkillsTags = SkillTagService.GetAll(Context).OrderBy(x => x.Name);

            Debug.WriteLine($"** {allProjectsNotCancelled?.Count()} projects loaded.");
        }

        private void OnSkillTagsChanged(IEnumerable<int> values)
        {
            selectedSkillTags = values ?? Enumerable.Empty<int>();
            FilterProjects();
        }
    }
}