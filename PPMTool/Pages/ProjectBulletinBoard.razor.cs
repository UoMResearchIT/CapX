using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
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

        private IEnumerable<Project> availableProjects;
        private IEnumerable<IGrouping<ProjectStatus, Project>> availableProjectsGrouped;
        private IEnumerable<Project> allProjects;
        private DateTime? startDate;
        private DateTime? endDate;
        private bool groupByStatus = true;


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

        private void FilterProjects()
        {
            if (allProjects == null) return;

            var temp = allProjects;

            if (startDate != null)
            {
                // If start date specified then the task has to run after the start date for it to be a viable option
                temp = temp.Where(x => x.SubTasks.Any(x => x.UnmetDemand > 0 && startDate <= x.EndDate));
            }

            if (endDate != null)
            {
                // If an end date is specified then the task has to run before the end date for it to be viable
                temp = temp.Where(x => x.SubTasks.Any(x => x.UnmetDemand > 0 && x.StartDate <= endDate));
            }

            availableProjects = temp;

            if (groupByStatus)
            {
                availableProjectsGrouped = temp.GroupBy(x => x.ProjectStatus);
            }
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

            // Filter to just projects that are active with current or future unmet demand
            allProjects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && x.HasUnmetDemandNowOrInFuture() && x.ProjectStatus != ProjectStatus.Paused);

            // Filter the projects
            FilterProjects();

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();

            Debug.WriteLine($"** {availableProjects?.Count()} projects loaded.");
        }
    }
}
