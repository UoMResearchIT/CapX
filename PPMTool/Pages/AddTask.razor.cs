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
        private PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            context = new PPMToolContext();
            projectModel = ProjectService.GetById(context, ProjectId);
            if (projectModel.SubTasks == null) projectModel.SubTasks = new List<SubTask>();

            // Load task
            if (TaskId > -1)
            {
                taskModel = projectModel.SubTasks.FirstOrDefault(x => x.SubTaskId == TaskId) ?? new SubTask();
            }

            // Update the resources
            foreach (var p in PersonService.GetAll(context))
            {
                resources.Add(new Resource
                {
                    Person = p,
                    Percentage = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.Percentage ?? 0 : 0
                });
            };

            // Subscribe listeners
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
            startDateDisabled = !taskModel.HasFixedStart;
            workDisabled = taskModel.TaskType == TaskType.FixedDuration || (taskModel.TaskType == TaskType.FixedUnits && !taskModel.IsWorkDriven);
            durationDisabled = taskModel.TaskType == TaskType.FixedWork || (taskModel.TaskType == TaskType.FixedUnits && taskModel.IsWorkDriven);
        }

        /// <summary>
        /// Update the sub task properties but doesn't save to the database
        /// </summary>
        public void UpdateSubTask()
        {
            Logger.LogInformation("Updating sub task configuration...");

            // Create resources on the sub task and track total proportion of effort
            taskModel.AssignedResources = new List<Resource>();
            double totalResourcePerDayHours = 0;
            foreach (var r in resources)
            {
                if (r.Percentage > 0)
                {
                    taskModel.AssignedResources.Add(r);

                    // Update the total resource assigned
                    totalResourcePerDayHours += r.Percentage * 7 / 100;
                }
            }

            // Compute the average hourly cost across the resources from their hourly rate
            // scaled by the proportion to which they are assigned to the task
            var people = PersonService.GetAll(context);
            double averageCostPerHourOfResources = 0;            
            foreach (var r in taskModel.AssignedResources)
            {
                var person = people.FirstOrDefault(x => x.Name == r.Person.Name);
                averageCostPerHourOfResources += (r.Percentage * 7 * person?.HourlyRate ?? 0) / (100 * totalResourcePerDayHours);
            }
            averageCostPerHourOfResources /= taskModel.AssignedResources.Count;

            // Update the actual cost for the sub task
            taskModel.ActualCost = taskModel.ActualWorkHours * averageCostPerHourOfResources;

            // Create predecessor on the sub task
            if (int.TryParse(PredecessorId, out var id))
            {
                taskModel.Predecessor = projectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == id);
            }

            // Schedule
            error = taskModel.Schedule();
            isValid = error == null;

            // TODO: Need to call schedule() on the subtask that this is a predecssor for too. Should naturally forward propagate.
            // The actual saving and updating of the project summary can then be done in the HandleValidSubmit like usual.

            // Update UI
            StateHasChanged();
        }

        private void HandleValidSubmit()
        {
            Logger.LogInformation("Adding new sub task...");
            if (projectModel != null)
            {
                UpdateSubTask();
                if (isValid)
                {
                    // Add new new to task list for project if it is a new one
                    if (TaskId < 0)
                    {
                        projectModel.SubTasks.Add(taskModel);
                    }

                    // Update the project summary values
                    projectModel.UpdateProjectSummary();

                    // Update the project in the database
                    ProjectService.Update(context, projectModel);

                    // Return to the project details page
                    Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }
    }
}
