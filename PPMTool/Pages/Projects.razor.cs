using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexCharts;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen.Blazor;
using FluentDate;
using System.Diagnostics;
using PPMTool.Pages.Components;
using System.Text.RegularExpressions;

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> projects;
        RadzenDataGrid<Project> projectGrid;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private IEnumerable<ProjectStatus> fundingOptions = (ProjectStatus[])Enum.GetValues(typeof(ProjectStatus));
        private List<ProjectSummaryWidget.ProjectSummaryData> summaryData;
        private PPMToolContext context;
        private bool showActiveOnly = true;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LoadProjectData();
        }

        private void OnChange(bool? value)
        {
            Debug.WriteLine("Change detected. Reloading data...");
            LoadProjectData();
        }

        /// <summary>
        /// Temporary method for migrating the RTP code from the name to the new RTP field as an integer using RegEx matching
        /// </summary>
        private void Migrate()
        {
            foreach (var p in projects)
            {
                var match = Regex.Match(p.Name, "(RTP-)(\\d+)");
                try
                {
                    var remain = p.Name.Remove(match.Index, match.Length).Trim();
                    p.Name = remain;
                    p.RTP = int.Parse(match.ToString().Split('-').Last());
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"{ex.Message}");
                }
                ProjectService.Update(context, p);
            }
            LoadProjectData();
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

            // Build the summary widget data and sort by total
            summaryData = new List<ProjectSummaryWidget.ProjectSummaryData>();
            foreach (var p in portfolioOptions)
            {
                summaryData.Add(new ProjectSummaryWidget.ProjectSummaryData
                {
                    Portfolio = p,
                    Active = proj.Where(x => x.Portfolio == p && (x.ProjectStatus == ProjectStatus.Active || x.ProjectStatus == ProjectStatus.Paused || x.ProjectStatus == ProjectStatus.Maintenance)).Count(),
                    Incoming = proj.Where(x => x.Portfolio == p && (x.ProjectStatus == ProjectStatus.Unfunded || x.ProjectStatus == ProjectStatus.Funded)).Count(),
                    Complete = proj.Where(x => x.Portfolio == p && x.ProjectStatus == ProjectStatus.Finished).Count()
                });
            }
            summaryData = summaryData.OrderByDescending(x => x.GetTotal()).ToList();


            // Remove the ones that are not active for the data grid if necessary
            if (showActiveOnly) proj = proj.Where(x => !x.ProjectStatus.IsProjectFinishedOrCancelled()).ToList();

            // Update the summary of each project and save back to DB
            if (proj.Count > 0)
            {
                for (int i = 0; i < proj.Count; ++i)
                {
                    var p = proj[i];
                    p.UpdateProjectSummary();
                    ProjectService.Update(context, p);
                }
            }

            // Assign data for the data grid
            projects = proj;
            Debug.WriteLine($"{proj.Count()} projects loaded.");
        }

        private void ProjectClicked(int id)
        {
            Navigation.NavigateTo($"/projectdetails/{id}");
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }

        private void EditProject(Project project)
        {
            Navigation.NavigateTo($"/addproject/{project.ProjectId}");
        }
    }
}
