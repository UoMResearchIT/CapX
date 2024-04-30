using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddTask : DataGridPage<Resource>
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private RolesService RolesService { get; set; }

        [Inject]
        private IJSRuntime JsRuntime { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        public int TaskId { get; set; }

        private int? selectedPredecessorId;
        private Project projectModel;
        private SubTask taskModel = new SubTask();
        private IList<Person> people = new List<Person>();
        private bool isValid = true;
        private bool startDateDisabled;
        private bool workDisabled;
        private bool durationDisabled;
        private string error;
        private IEnumerable<TaskType> taskTypes = new List<TaskType>();
        private IList<SubTask> predecessorTasks = new List<SubTask>();

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntities = new List<Resource>();
            people = PersonService.GetAll(context)
                .Where(x => x.EndDate == null || x.EndDate >= DateTime.Now)
                .OrderBy(x => x.Name)
                .ToList();
            taskTypes = Enum.GetValues<TaskType>().ToList();
            projectModel = ProjectService.GetById(context, ProjectId);

            // No project then stop initialising
            if (projectModel == null) return;

            // Initialise sub tasks
            if (projectModel.SubTasks == null) projectModel.SubTasks = new List<SubTask>();

            // If editing or adding a task, only allow the project manager of the owning project to do it or a superuser
            var user = AuthenticationState?.User;
            var role = RolesService.GetByUsername(context, ActiveUser);
            EditAuthorised = (user?.IsInRole("Superuser") ?? false) || ((user?.IsInRole("Manager") ?? false) && projectModel.ProjectManager == role?.Person);

            // Load task
            if (TaskId > -1)
            {
                // Load model
                taskModel = projectModel.SubTasks.FirstOrDefault(x => x.SubTaskId == TaskId) ?? new SubTask();

                // Assign the predecessor option
                if (taskModel.Predecessor != null) selectedPredecessorId = taskModel.Predecessor.SubTaskId;

                // Assign resources
                foreach (var r in taskModel.AssignedResources)
                {
                    dataGridEntities.Add(r);
                }
            }

            // Populate predecessor dropdown source (exclude self)
            predecessorTasks = projectModel.SubTasks
                .Where(x => x.SubTaskId != taskModel.SubTaskId && x.Predecessor?.SubTaskId != taskModel.SubTaskId).ToList();

            // Subscribe listeners
            taskModel.TaskTypeChanged += UpdateUIState;
            taskModel.FixedStartChanged += UpdateUIState;
            taskModel.WorkDrivenChanged += UpdateUIState;
            taskModel.EndDateDrivenChanged += UpdateUIState;
            taskModel.DoneChanged += UpdateUIState;
            UpdateUIState(taskModel, new EventArgs());

            LogInformation(taskModel.SubTaskId > 0 ? $"Editing task {taskModel?.Name} on {projectModel?.GetFullName()}" : $"Adding new task to {projectModel?.GetFullName()}");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // If no project then navigate away
            if (projectModel == null) Navigation.NavigateTo("/nothinghere");
        }

        private string GetNiceString(Enum x)
        {
            return x.ToNiceString();
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
            durationDisabled = taskModel.TaskType == TaskType.FixedWork || (taskModel.TaskType == TaskType.FixedUnits && taskModel.IsWorkDriven) || taskModel.TaskType == TaskType.FixedDuration && taskModel.HasFixedEndDate || taskModel.IsDone;
        }

        private async void DeleteSubTask()
        {
            if (TaskId > -1)
            {
                bool confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete task {taskModel.Name} from project {projectModel?.GetFullName()}");
                if (confirmed)
                {
                    LogWarning($"Deleting task {taskModel?.Name} on {projectModel?.GetFullName()}");

                    // Call delete on the subtask service and let it remove the resources
                    SubTaskService.Delete(context, taskModel);

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

        protected override void CancelEdit(Resource resource)
        {
            LogInformation($"Cancel edit row for {resource.GetSensibleObjectName()}");
            Reset();
            SubTaskService.RestoreModel(context, ref resource);
            dataGrid.CancelEditRow(resource);
        }

        protected override void OnCreateRow(Resource resource)
        {
            LogInformation($"Created new row for {resource.GetSensibleObjectName()}");
            dataGridEntities.Add(resource);
            entityToInsert = null;
            taskModel.UpdateUnmetDemand(dataGridEntities);
        }

        protected override void OnUpdateRow(Resource entity)
        {
            Reset();
        }

        private void OnResourcePersonChange(object value)
        {
            Debug.WriteLine("** Resource Person Change");
            Person person = value as Person;
            if (person != null)
            {
                Resource resourceToChange;
                if (entityToInsert != null)
                {
                    resourceToChange = entityToInsert;
                }
                else if (entityToUpdate != null)
                {
                    resourceToChange = entityToUpdate;
                }
                else
                {
                    return;
                }

                // Update the day rate field if using the default
                if (resourceToChange.UseDefaultDayRate)
                {
                    resourceToChange.DayRate = person.DayRate;
                }
            }
        }

        protected override async Task DeleteRow(Resource entity)
        {
            await base.DeleteRow(entity);
            taskModel.UpdateUnmetDemand(dataGridEntities);
        }

        protected override async Task SaveRow(Resource entity)
        {
            await base.SaveRow(entity);
            taskModel.UpdateUnmetDemand(dataGridEntities);
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding task changes!");
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
                LogInformation("Not updating sub task as it is marked as Done...");
            }
            else
            {
                LogInformation("Updating sub task configuration...");

                // Update the resources on the task model to match the data grid entities
                taskModel.AssignedResources.Clear();
                foreach (var r in dataGridEntities)
                {
                    Debug.WriteLine($"** Active Resource: ResId: {r.ResourceId} | PersonId: {r.Person.PersonId} | FTE: {r.AssignmentFTE} | Rate: {r.DayRate}");
                    taskModel.AssignedResources.Add(r);
                }

                // Track total proportion of effort
                double totalResourceDaysPerDay = 0;
                foreach (var r in dataGridEntities)
                {
                    // Update the total resource assigned
                    totalResourceDaysPerDay += r.AssignmentFTE;
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
                    averageCostPerDayOfResources += (r.AssignmentFTE * (r.DayRate ?? person.DayRate)) / totalResourceDaysPerDay;
                }

                // Update the actual cost for the sub task
                // Truncate to 2 DP
                taskModel.ActualCost = Math.Round(taskModel.ActualWorkHours * averageCostPerDayOfResources * 100 / 7) / 100;

                // Update predecessor task
                taskModel.Predecessor = projectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == selectedPredecessorId);

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
            if (projectModel != null)
            {
                UpdateSubTask();
                if (isValid)
                {
                    LogInformation("Saving sub task...");

                    // Add new new to task list for project if it is a new one
                    if (TaskId < 0)
                    {
                        // Add the subtask to the database
                        SubTaskService.Add(context, taskModel);

                        // Add reference to the project
                        projectModel.SubTasks.Add(taskModel);
                    }
                    else
                    {
                        // Update the sub task in the database
                        SubTaskService.Update(context, taskModel);
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
