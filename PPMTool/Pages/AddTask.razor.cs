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
    public partial class AddTask : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Parameter]
        public int ProjectId { get; set; }

        private Project projectModel = new Project();

        private SubTask taskModel = new SubTask();

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new sub task...");

            using var context = new PPMToolContext();
            SubTaskService.AddSubTask(context, taskModel);
            Navigation.NavigateTo($"projectdetails/{ProjectId}");
        }
    }
}
