using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddProject : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        [Parameter]
        public int ProjectId { get; set; }

        EditForm ProjectForm { get; set; }

        private Project projectModel = new Project();
        private bool gotoDetails = false;
        private bool discardChanges = true;
        private IEnumerable<InnateCode> innateActivities = new List<InnateCode>();
        private IEnumerable<Person> projectManagers = new List<Person>();
        private IEnumerable<Portfolio> portfolios = new List<Portfolio>();
        private IEnumerable<ProjectStatus> statuses = new List<ProjectStatus>();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectId > -1)
            {
                projectModel = ProjectService.GetById(context, ProjectId);

                // If editing a project, only allow the project manager to edit it or a superuser
                var user = AuthenticationState?.User;
                var role = RolesService.GetByUsername(context, ActiveUser);
                EditAuthorised = (user?.IsInRole("Superuser") ?? false) || ((user?.IsInRole("Manager") ?? false) && projectModel.ProjectManager == role?.Person);
            }

            innateActivities = InnateCodeService.GetAll(context).OrderBy(x => x.ActivityCode).ToList();
            portfolios = Enum.GetValues<Portfolio>().ToList();
            statuses = Enum.GetValues<ProjectStatus>().ToList();
            var people = PersonService.GetAll(context).OrderBy(x => x.Name).ToList();
            var roles = RolesService.GetAll(context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            projectManagers = people.Where(x => roles.Any(y => y.Person == x)).ToList();

            LogInformation(projectModel.ProjectId > 0 ? $"Editing project {projectModel?.GetFullName()}" : $"Adding new project");
        }

        private string GetNiceString(Enum x)
        {
            return x.ToNiceString();
        }

        private void HandleSubmit()
        {
            // Form valid
            if (ProjectForm.EditContext.Validate())
            {
                if (!discardChanges)
                {
                    if (ProjectId > -1)
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

                        LogInformation($"Edit project {projectModel?.GetFullName()} saved...");
                        ProjectService.Update(context, projectModel);
                    }
                    else
                    {
                        LogInformation("Adding new project...");

                        if (ProjectService.Add(context, projectModel) < 0)
                        {
                            // TODO: Duplicate found -- do something
                            LogWarning($"Duplicate project found with name {projectModel?.Name}");
                        }
                    }
                }

                NavigatePostSubmit();
            }

            // Form invalid
            else
            {
                if (discardChanges)
                {
                    LogInformation($"Discarding project changes!");
                    NavigatePostSubmit();
                }
            }
        }

        private void NavigatePostSubmit()
        {
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
                            LogInformation($"Deleting subtask ID {projectModel.SubTasks.First()?.SubTaskId}");

                            SubTaskService.Delete(context, projectModel.SubTasks.First());
                        }
                    }

                    LogInformation($"Deleting project {projectModel.GetFullName()}, ID {projectModel.ProjectId}");

                    // Delete the project from the database
                    ProjectService.Delete(context, projectModel);

                    // Navigate back to the projects list
                    Navigation.NavigateTo("projects");
                }
            }
        }
    }
}
