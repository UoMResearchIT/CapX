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
        private IEnumerable<Project> allProjects;
        private DateTime? startDate;
        private DateTime? endDate;
        private bool groupByStatus;


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

        private void ClearDateFilter()
        {
            startDate = null;
            endDate = null;
            FilterProjects();
        }

        private void FilterProjects()
        {
            if (allProjects == null) return;

            availableProjects = allProjects;

            if (startDate != null)
            {

            }

            if (endDate != null)
            {

            }

            if (groupByStatus)
            {

            }
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

            // Filter to just projects that are active with current or future unmet demand
            allProjects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && x.HasUnmetDemandNowOrInFuture());

            // Filter the projects
            FilterProjects();

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();

            Debug.WriteLine($"** {availableProjects?.Count()} projects loaded.");
        }
    }
}
