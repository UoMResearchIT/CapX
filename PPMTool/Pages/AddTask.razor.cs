using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
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

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        public int TaskId { get; set; }

        private string predecessorId;
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

                // Assign the predecessor option
                if (taskModel.Predecessor != null) predecessorId = taskModel.Predecessor.SubTaskId.ToString();
            }

            // Update the resources
            foreach (var p in PersonService.GetAll(context).OrderBy(x => x.Name))
            {
                resources.Add(new Resource
                {
                    ResourceId = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.ResourceId ?? 0 : 0,
                    Person = p,
                    Percentage = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.Percentage ?? 0 : 0,
                    UseDefaultDayRate = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.UseDefaultDayRate ?? true : true,
                    DayRate = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.DayRate ?? null : null,
                    IsProvisional = TaskId > -1 ? taskModel.AssignedResources.FirstOrDefault(x => x.Person == p)?.IsProvisional ?? false : false
                });
            };

            // Subscribe listeners
            taskModel.TaskTypeChanged += UpdateUIState;
            taskModel.FixedStartChanged += UpdateUIState;
            taskModel.WorkDrivenChanged += UpdateUIState;
            taskModel.EndDateDrivenChanged += UpdateUIState;
            taskModel.DoneChanged += UpdateUIState;
            UpdateUIState(taskModel, new EventArgs());
        }

        /// <summary>
        /// Handler for the events on the sub task to update various UI flags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateUIState(object sender, EventArgs e)
        {
            startDateDisabled = !taskModel.HasFixedStart || taskModel.IsDone;
            workDisabled = taskModel.TaskType == TaskType.FixedDuration || (taskModel.TaskType == TaskType.FixedUnits && !taskModel.IsWorkDriven) || taskModel.IsDone;
            durationDisabled = taskModel.TaskType == TaskType.FixedWork || (taskModel.TaskType == TaskType.FixedUnits && taskModel.IsWorkDriven) || taskModel.TaskType == TaskType.FixedDuration && !taskModel.IsEndDateDriven || taskModel.IsDone;
        }

        private async void DeleteSubTask()
        {
            if (TaskId > -1)
            {
                bool confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete task {taskModel.Name} from project {projectModel?.Name}");
                if (confirmed)
                {
                    // Call delete on the subtask service and let it remove the resources
                    SubTaskService.DeleteTask(context, taskModel);

                    // Remove the sub-task from the project model
                    projectModel.SubTasks.Remove(taskModel);

                    // Update the project summary values
                    projectModel.UpdateProjectSummary();

                    // Update the project in the database
                    ProjectService.Update(context, projectModel);

                    // Return to the project details page
                    Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }

        private void DiscardChanges()
        {
            Navigation.NavigateTo($"projectdetails/{projectModel.ProjectId}");
        }

        /// <summary>
        /// Update the sub task properties but doesn't save to the database
        /// </summary>
        public void UpdateSubTask()
        {
            // Don't update the scheduling if the task is done
            if (taskModel.IsDone)
            {
                Logger.LogInformation("Not updating sub task as it is marked as Done...");
            }
            else
            {
                Logger.LogInformation("Updating sub task configuration...");

                // Get all the non-zero percentage resources
                var activeRes = resources.Where(x => x.Percentage > 0);

                // Remove resources that are no-longer active
                var toRemove = taskModel.AssignedResources.Where(x => !activeRes.Any(y => x.ResourceId == y.ResourceId));
                foreach (var r in toRemove.ToList())
                {
                    taskModel.AssignedResources.Remove(r);
                }

                // Update/Add the active resources
                foreach (var act in activeRes)
                {
                    Debug.WriteLine($"** ResId: {act.ResourceId} | PersonId: {act.Person.PersonId} | Percent: {act.Percentage}");
                    var existing = taskModel.AssignedResources.FirstOrDefault(x => x.ResourceId == act.ResourceId);
                    if (existing != null)
                    {
                        // Don't know why I have to update every individual property to get this to work
                        existing.Percentage = act.Percentage;
                        existing.DayRate = act.DayRate;
                        existing.UseDefaultDayRate = act.UseDefaultDayRate;
                        existing.IsProvisional = act.IsProvisional;
                    }
                    else
                    {
                        taskModel.AssignedResources.Add(act);
                    }
                }

                // Track total proportion of effort
                double totalResourceDaysPerDay = 0;
                foreach (var r in resources)
                {
                    // Update the total resource assigned
                    totalResourceDaysPerDay += r.Percentage / 100;
                }

                // Compute the average hourly cost across the resources from their day rate
                // scaled by the proportion to which they are assigned to the task
                // This only works if every is the same cost. If we change this then we would
                // need actuals entering per person.
                var people = PersonService.GetAll(context);
                double averageCostPerDayOfResources = 0;
                foreach (var r in taskModel.AssignedResources)
                {
                    var person = people.FirstOrDefault(x => x.PersonId == r.Person.PersonId);
                    if (person == null) continue;
                    // User the default day rate for the person if the assigned day rate is null
                    averageCostPerDayOfResources += (r.Percentage * (r.DayRate ?? person.DayRate)) / (100 * totalResourceDaysPerDay);
                }

                // Update the actual cost for the sub task
                // Truncate to 2 dp
                taskModel.ActualCost = Math.Round(taskModel.ActualWorkHours * averageCostPerDayOfResources * 100 / 7) / 100;

                // Create predecessor on the sub task
                if (int.TryParse(predecessorId, out var id))
                {
                    taskModel.Predecessor = projectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == id);
                }

                // Schedule
                error = taskModel.Schedule(false);
                isValid = error == null;

                // Call schedule() on the subtask that this is a predecssor for
                var error2 = SubTaskService.UpdateFollowerTasks(taskModel, projectModel.SubTasks);
                if (error2 != null)
                {
                    error = $"{error2.Item1}: {error2.Item2}";
                    isValid = false;
                }
            }

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
                        // Add the subtask to the database
                        SubTaskService.Add(context, taskModel);

                        // Add reference to the project
                        projectModel.SubTasks.Add(taskModel);
                    }

                    // Update the sub task in the database
                    SubTaskService.Update(context, taskModel);

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
