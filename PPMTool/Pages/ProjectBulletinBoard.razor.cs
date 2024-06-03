using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ProjectBulletinBoard : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private IEnumerable<Project> availableProjects;

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

        private void LoadProjectData()
        {
            // Get projects from the database
            var proj = ProjectService.GetAll(context).OrderBy(x => x.RTP).ToList();

            // Filter owned projects to only show active ones
            availableProjects = proj.Where(x => !x.ProjectStatus.IsFinishedOrCancelled() && x.HasUnmetDemandNowOrInFuture());

            // Disable spinner now load complete
            Loading = false;
            StateHasChanged();

            Debug.WriteLine($"** {availableProjects?.Count()} projects loaded.");
        }
    }
}
