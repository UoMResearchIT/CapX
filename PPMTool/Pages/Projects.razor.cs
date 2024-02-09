using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
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

        [Inject]
        private RolesService RoleService { get; set; }

        private IEnumerable<Project> projects;
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

            // Only show projects to developers that they are assigned to
            if (!EditAuthorised)
            {
                // Look up the username
                var uname = AuthenticationState.User.Identity.Name.Trim().ToLower();
                var role = RoleService.GetByUsername(context, uname);

                // Log any time there is no role returned?
                if (role == null)
                {
                    Logger.LogError($"{uname}: Role is null!");
                }

                proj = proj.Where(x => x.SubTasks.Any(x => x.AssignedResources.Any(x => x.Person == role.Person))).ToList();
            }

            // Remove the ones that are not active for the data grid if necessary
            if (showActiveOnly) proj = proj.Where(x => !x.ProjectStatus.IsProjectFinishedOrCancelled()).ToList();

            // Update the summary of each project and save back to DB
            if (proj.Count > 0)
            {
                Debug.WriteLine($"** Updating project summary data...");
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
