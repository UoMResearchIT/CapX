// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Data;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
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

        private IEnumerable<ProjectWithSkills> availableProjects;
        private IEnumerable<IGrouping<ProjectStatus, ProjectWithSkills>> availableProjectsGrouped;
        private IList<ProjectWithSkills> allProjectsWithSkills;
        private DateTime? startDate;
        private DateTime? endDate;
        private bool groupByStatus = true;
        private bool skillsInitialised;


        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;
            LogInformation("Viewing project bulletin board");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            // Load settings the first time
            if (!firstRender) return;

            // Load data from DB
            LoadProjectData();

            // Filter the data
            EnqueueLoadData(GetFilterTask);
        }

        /// <summary>
        /// Returns a task that filters the loaded project data
        /// </summary>
        /// <returns></returns>
        private Task GetFilterTask()
        {
            return Task.Run(() =>
            {
                FilterProjects();
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Method which sets the loading state and invokes the background task to filter
        /// </summary>
        private void ReloadWithFilters()
        {
            Loading = true;
            StateHasChanged();
            EnqueueLoadData(GetFilterTask);
        }

        /// <summary>
        /// Filter the available projects based on the settings
        /// </summary>
        private void FilterProjects()
        {
            if (allProjectsWithSkills == null) return;

            if (startDate != null && endDate != null)
            {
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }

            // First time through, initialise the skills
            if (!skillsInitialised)
            {
                foreach (var projWithSkills in allProjectsWithSkills)
                {
                    projWithSkills.Skills = SkillTagService.GetSkillsForProject(Context, projWithSkills.Project.ProjectId);
                }
                skillsInitialised = true;
            }

            // Get projects with unmet demand in the window and order by the start of the window then by the maximum unmet demand
            availableProjects = allProjectsWithSkills
                .Where(x => x.Project.HasUnmetDemandInWindow(startDate, endDate))
                .OrderBy(x =>
                {
                    x.Project.GetUnmetDemandWindowDates(out var windowStart, out var windowEnd);
                    return windowStart;
                })
                .ThenByDescending(x => x.Project.SubTasks.Max(x => x.GetUnmetDemandInWindow(startDate, endDate)));

            if (groupByStatus)
            {
                availableProjectsGrouped = availableProjects.GroupBy(x => x.Project.ProjectStatus).OrderByDescending(x => x.Key);
            }

            Debug.WriteLine($"** {availableProjects?.Count()} projects loaded.");
        }

        /// <summary>
        /// Load all valid projects from the DB
        /// </summary>
        private void LoadProjectData()
        {
            // Initialise the list
            allProjectsWithSkills = new List<ProjectWithSkills>();

            // Get projects from the database
            var proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();

            // Filter to just projects that are active with current or future unmet demand
            var projects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && x.HasUnmetDemandInWindow() && x.ProjectStatus != ProjectStatus.Paused);

            // Convert to new object type
            foreach (var project in projects)
            {
                allProjectsWithSkills.Add(new ProjectWithSkills
                {
                    Project = project,
                    Skills = new List<SkillTag>()
                });
            }
        }
    }
}
