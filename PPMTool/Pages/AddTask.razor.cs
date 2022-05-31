using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class AddTask : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Parameter]
        public int ProjectId { get; set; }

        private Project projectModel;
        private SubTask taskModel = new SubTask();
        private IList<Resource> resources = new List<Resource>();
        private bool isValid = true;
        private bool StartDateDisabled { get; set; }
        private bool WorkDisabled { get; set; }
        private bool DurationDisabled { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            using var context = new PPMToolContext();
            projectModel = ProjectService.GetById(context, ProjectId);
            foreach (var p in PersonService.GetAll(context))
            {
                resources.Add(new Resource
                {
                    Person = p,
                    Percentage = 0
                });
            };
            taskModel.TaskTypeChanged += UpdateUIState;
            taskModel.FixedStartChanged += UpdateUIState;
            taskModel.WorkDrivenChanged += UpdateUIState;
            UpdateUIState(taskModel, new EventArgs());
        }

        /// <summary>
        /// Handler for the events on the sub task to update various UI flags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateUIState(object sender, EventArgs e)
        {
            StartDateDisabled = !taskModel.HasFixedStart;
            WorkDisabled = taskModel.TaskType == TaskType.FixedDuration || (taskModel.TaskType == TaskType.FixedUnits && !taskModel.IsWorkDriven);
            DurationDisabled = taskModel.TaskType == TaskType.FixedWork || (taskModel.TaskType == TaskType.FixedUnits && taskModel.IsWorkDriven);
        }

        public void OnUpdate()
        {
            Logger.LogInformation("Updating sub task configuration...");

            // Refresh resources on task
            taskModel.AssignedResources = new List<Resource>();
            foreach (var r in resources)
            {
                if (r.Percentage > 0)
                {
                    taskModel.AssignedResources.Add(r);
                }
            }

            // Schedule
            isValid = taskModel.Schedule();

            // Update UI
            StateHasChanged();
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new sub task...");
            if (projectModel != null)
            {
                OnUpdate();
                if (isValid)
                {
                    using var context = new PPMToolContext();
                    SubTaskService.AddSubTask(context, taskModel);
                    projectModel.Tasks.Add(taskModel);
                    ProjectService.Update(context, projectModel);
                    Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }
    }
}
