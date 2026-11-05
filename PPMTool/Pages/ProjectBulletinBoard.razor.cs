// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Enums;
using PPMTool.Models;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Developer,Manager,Superuser")]
    public partial class ProjectBulletinBoard : BasePage
    {
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

        protected override async Task OnInitializedAsync()
        {
            base.OnInitialized();
            LogInformation("Viewing project bulletin board");

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
            if (allProjectsNotCancelled == null) return;

            // Check we have valid dates and adjust if not
            if (startDate != null && endDate != null)
            {
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }

            // TODO
            // Get projects with unmet demand in the window and order by the start of the window then by the maximum unmet demand
            projectsToShow = allProjectsNotCancelled
                .Where(x => x.Project.HasUnmetDemandInWindow(startDate, endDate))
                .OrderBy(x =>
                {
                    x.Project.GetUnmetDemandWindowDates(out var windowStart, out var windowEnd);
                    return windowStart;
                })
                .ThenByDescending(x => x.Project.SubTasks.Max(x => x.GetUnmetDemandInWindow(startDate, endDate)));

            // Group the projects by status if the switch is set
            if (groupByStatus)
            {
                projectsToShowGrouped = projectsToShow.GroupBy(x => x.Project.ProjectStatus).OrderByDescending(x => x.Key);
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
            var projects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && !x.ProjectStatus == ProjectStatus.NewRequest);

            // Convert to new object type and load the skills and people on the fly
            foreach (var project in projects)
            {
                allProjectsNotCancelled.Add(new ProjectWithAdditionalData
                {
                    Project = project,
                    Skills = SkillTagService.GetSkillsForProject(Context, project.ProjectId),
                    AssignedResourceNames = await SubTaskService.GetUniqueResourcesAsync(Context, project.ProjectId)
                });
            }

            Debug.WriteLine($"** {allProjectsNotCancelled?.Count()} projects loaded.");
        }
    }
}