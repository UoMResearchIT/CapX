using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Pages.Components;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Developer,Manager,Superuser")]
    public partial class ProjectBulletinBoard : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> availableProjects;
        private IEnumerable<IGrouping<ProjectStatus, Project>> availableProjectsGrouped;
        private IEnumerable<Project> allProjects;
        private DateTime? startDate;
        private DateTime? endDate;
        private bool groupByStatus = true;
        private IList<ProjectBulletinCardComponent> projectBulletinCardComponents = new List<ProjectBulletinCardComponent>();


        protected override void OnInitialized()
        {
            base.OnInitialized();
            Loading = true;
            LogInformation("Viewing project bulletin board");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            // Load settings the first time
            if (firstRender)
            {
                // Load data
                LoadProjectData();
            }
        }

        /// <summary>
        /// Filter the available projects based on the settings
        /// </summary>
        private void FilterProjects()
        {
            if (allProjects == null) return;

            if (startDate != null && endDate != null)
            {
                if (endDate < startDate)
                {
                    endDate = startDate;
                }
            }

            // Get projects with unmet demand in the window and order by the start of the window then by the maximum unmet demand
            availableProjects = allProjects
                .Where(x => x.HasUnmetDemandInWindow(startDate, endDate))
                .OrderBy(x =>
                {
                    x.GetUnmetDemandWindowDates(out var windowStart, out var windowEnd);
                    return windowStart;
                })
                .ThenByDescending(x => x.SubTasks.Max(x => x.GetUnmetDemandInWindow(startDate, endDate)));

            if (groupByStatus)
            {
                availableProjectsGrouped = availableProjects.GroupBy(x => x.ProjectStatus).OrderByDescending(x => x.Key);
            }

            // Update the min/max on the card
            foreach (var component in projectBulletinCardComponents)
            {
                component.UpdateCardData(startDate, endDate);
            }
        }

        private void OnBulletinCardInitialised(ProjectBulletinCardComponent component)
        {
            // Stash a reference to the component so we can invoke the update method later
            if (!projectBulletinCardComponents.Contains(component))
            {
                projectBulletinCardComponents.Add(component);
            }
            component.UpdateCardData(startDate, endDate);
        }

        /// <summary>
        /// Load all valid projects from the DB
        /// </summary>
        private void LoadProjectData()
        {
            // Get projects from the database
            var proj = ProjectService.GetAll(Context).OrderBy(x => x.RTP).ToList();

            // Filter to just projects that are active with current or future unmet demand
            allProjects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && x.HasUnmetDemandInWindow() && x.ProjectStatus != ProjectStatus.Paused);

            // Filter the projects
            FilterProjects();

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();

            Debug.WriteLine($"** {availableProjects?.Count()} projects loaded.");
        }
    }
}
