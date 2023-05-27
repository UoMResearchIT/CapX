using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using System.Linq;

namespace PPMTool.Pages
{
    public partial class AddProject : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

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
                if (projectModel.ProjectStatus.IsProjectCancelled())
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

        private async void DeleteProject()
        {
            if (ProjectId > -1)
            {
                // Prompt
                bool confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete project {projectModel.GetFullName()}. " +
                    $"If this project was cancelled or didn't get funded then do not delete it but change its status instead so we can keep a record of unfunded projects.");
                if (confirmed)
                {
                    // Delete all the subtasks for the project
                    var numToDelete = projectModel.SubTasks.Count;
                    for (int i = 0; i < numToDelete; ++i)
                    {
                        if (projectModel.SubTasks.Count > 0)
                        {
                            Logger.LogInformation($"Deleting subtask ID {projectModel.SubTasks.First()?.SubTaskId}");

                            SubTaskService.DeleteTask(context, projectModel.SubTasks.First());
                        }
                    }

                    Logger.LogInformation($"Deleting project {projectModel.GetFullName()}, ID {projectModel.ProjectId}");

                    // Delete the project from the database
                    ProjectService.DeleteProject(context, projectModel);

                    // Navigate back to the projects list
                    Navigation.NavigateTo("projects");
                }
            }
        }
    }
}
