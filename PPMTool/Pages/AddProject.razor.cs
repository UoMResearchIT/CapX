using System;
using System.Collections.Generic;
using System.Linq;
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
        public int? ProjectId { get; set; }

        private Project projectModel = new Project();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectId != null)
            {
                using var context = new PPMToolContext();
                projectModel = ProjectService.GetById(context, ProjectId);
            }
        }

        private void HandleValidSubmit()
        {
            using (var context = new PPMToolContext())
            {
                if (ProjectId != null)
                {
                    Logger.LogInformation($"Edit project {ProjectId} saved...");
                    ProjectService.Update(context, projectModel);
                }
                else
                {
                    Logger.LogInformation("Adding new project...");

                    if (!ProjectService.AddProject(context, projectModel))
                    {
                        // TODO: Duplicate found -- do something
                    }
                }
            }

            Navigation.NavigateTo("projects");
        }
    }
}
