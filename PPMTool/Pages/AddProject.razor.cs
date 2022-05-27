using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddProject : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        private Project projectModel = new Project();

        protected override async Task OnInitializedAsync()
        {
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new project...");

            using var context = new PPMToolContext();
            if (!ProjectService.AddProject(context, projectModel))
            {
                // TODO: Duplicate found -- do something
            }

            Navigation.NavigateTo("projects");
        }
    }
}
