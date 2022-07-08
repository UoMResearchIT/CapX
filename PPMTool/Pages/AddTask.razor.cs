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
        private PersonService PersonService { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        public int TaskId { get; set; }

        public string PredecessorId { get; set; }

        private Project projectModel;
        private SubTask taskModel = new SubTask();
        private IList<Resource> resources = new List<Resource>();
        private bool isValid = true;
        private bool startDateDisabled;
        private bool workDisabled;
        private bool durationDisabled;
        private string error;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            using var context = new PPMToolContext();
            projectModel = ProjectService.GetById(context, ProjectId);
            if (projectModel.SubTasks == null) projectModel.SubTasks = new List<SubTask>();
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

            // Load task
            if (TaskId > -1)
            {
                taskModel = projectModel.SubTasks.FirstOrDefault(x => x.SubTaskId == TaskId) ?? new SubTask();
            }
        }

        /// <summary>
        /// Handler for the events on the sub task to update various UI flags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateUIState(object sender, EventArgs e)
        {
            startDateDisabled = !taskModel.HasFixedStart;
            workDisabled = taskModel.TaskType == TaskType.FixedDuration || (taskModel.TaskType == TaskType.FixedUnits && !taskModel.IsWorkDriven);
            durationDisabled = taskModel.TaskType == TaskType.FixedWork || (taskModel.TaskType == TaskType.FixedUnits && taskModel.IsWorkDriven);
        }

        /// <summary>
        /// Update the sub task properties
        /// </summary>
        public void OnUpdate()
        {
            Logger.LogInformation("Updating sub task configuration...");

            // Create resources on the sub task
            taskModel.AssignedResources = new List<Resource>();
            foreach (var r in resources)
            {
                if (r.Percentage > 0)
                {
                    taskModel.AssignedResources.Add(r);
                }
            }

            // Create predecessor on the sub task
            if (int.TryParse(PredecessorId, out var id))
            {
                using var context = new PPMToolContext();
                taskModel.Predecessor = projectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == id);
            }

            // Schedule
            error = taskModel.Schedule();
            isValid = error == null;

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
                    using (var context = new PPMToolContext())
                    {
                        // Add new new to task list for project if it is a new one
                        if (TaskId < 0)
                        {
                            projectModel.SubTasks.Add(taskModel);
                        }

                        // Update the project summary values
                        projectModel.UpdateProjectSummary();
                        ProjectService.Update(context, projectModel);
                    }

                    Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }
    }
}
