using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class Projects : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> projects;
        private IEnumerable<Portfolio> portfolioOptions = (Portfolio[])Enum.GetValues(typeof(Portfolio));
        private PPMToolContext context;
        private bool showActiveOnly = true;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            LoadProjectData();
        }

        private void OnChange(bool? value)
        {
            Debug.WriteLine("** Change detected. Reloading data...");
            LoadProjectData();
        }

        private void LoadProjectData()
        {
            // Get projects from the database
            context = new PPMToolContext();
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

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
            Debug.WriteLine($"** {proj.Count()} projects loaded.");
        }

        private void ProjectDetails(int id)
        {
            Navigation.NavigateTo($"/projectdetails/{id}");
        }

        private void AddProject()
        {
            Navigation.NavigateTo($"/addproject/-1");
        }
    }
}
