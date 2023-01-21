using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddProject : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Parameter]
        public int ProjectId { get; set; }

        private Project projectModel = new Project();
        private PPMToolContext context;
        private bool gotoDetails = false;
        private bool discardChanges = true;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();

            if (ProjectId > -1)
            {
                projectModel = ProjectService.GetById(context, ProjectId);
            }
        }

        private void HandleValidSubmit()
        {
            if (ProjectId > -1 && !discardChanges)
            {
                // Check to see if the project is marked as cancelled as then we need to remove resources.
                // Leave resources on completed projects so we have a historical record.
                if (projectModel.ProjectStatus == Enums.ProjectStatus.Cancelled)
                {
                    foreach (SubTask t in projectModel.SubTasks)
                    {
                        t.AssignedResources.Clear();
                    }
                }

                Logger.LogInformation($"Edit project {ProjectId} saved...");
                ProjectService.Update(context, projectModel);
            }
            else
            {
                if (!discardChanges)
                {
                    Logger.LogInformation("Adding new project...");

                    if (!ProjectService.AddProject(context, projectModel))
                    {
                        // TODO: Duplicate found -- do something
                    }
                }
            }

            if (gotoDetails)
            {
                Navigation.NavigateTo($"projectdetails/{projectModel.ProjectId}");
            }
            else
            {
                Navigation.NavigateTo("projects");
            }
        }
    }
}
