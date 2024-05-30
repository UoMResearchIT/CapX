using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

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

        [Parameter]
        [SupplyParameterFromQuery(Name = "copy")]
        public bool IsCopy { get; set; }

        [Parameter]
        public bool IsSplit { get; set; }

        public SubTask TaskModel { get; private set; } = new SubTask();

        private Project projectModel;
        public Project ProjectModel
        {
            get => projectModel;
            private set
            {
                if (value != projectModel)
                {
                    projectModel = value;
                }
            }
        }

        public bool IsValid { get; private set; } = true;

        private int? selectedPredecessorId;
        private IList<Person> people = new List<Person>();
        private IList<Person> filteredPeople = new List<Person>();
        private bool startDateDisabled;
        private bool workDisabled;
        private bool durationDisabled;
        private string error;
        private IEnumerable<TaskType> taskTypes = new List<TaskType>();
        private IList<SubTask> predecessorTasks = new List<SubTask>();
        private EditContext editContext;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            people = PersonService.GetAll(context)
                .Where(x => x.EndDate == null || x.EndDate >= DateTime.Now)
                .OrderBy(x => x.Name)
                .ToList();
            taskTypes = Enum.GetValues<TaskType>().ToList();

            InitialiseModels();

            // If editing or adding a task, only allow the project manager of the owning project to do it or a superuser
            var user = AuthenticationState?.User;
            var role = RolesService.GetByUsername(context, ActiveUser);
            EditAuthorised = (user?.IsInRole("Superuser") ?? false) || ((user?.IsInRole("Manager") ?? false) && ProjectModel.ProjectManager == role?.Person);

            LogInformation(TaskModel.SubTaskId > 0 ? $"Editing task {TaskModel?.Name} on {ProjectModel?.GetFullName()} | Copy = {IsCopy}" : $"Adding new task to {ProjectModel?.GetFullName()}");
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // If no project then navigate away
            if (ProjectModel == null) Navigation.NavigateTo("/nothinghere");
        }

        public void InitialiseModels()
        {
            // Get project model from DB
            ProjectModel = ProjectService.GetById(context, ProjectId);
            ProjectService.RestoreModel(context, ref projectModel);

            // No project then stop initialising
            if (ProjectModel == null)
            {
                LogError($"Project model failed to initialise! ID = {ProjectId}");
                return;
            }

            // Initialise sub tasks
            if (ProjectModel.SubTasks == null) ProjectModel.SubTasks = new List<SubTask>();

            // Initialise
            dataGridEntities = new List<Resource>();

            // Load task and related data
            if (TaskId > 0)
            {
                // Get task or clone if copying
                var referenceTask = ProjectModel.SubTasks.FirstOrDefault(x => x.SubTaskId == TaskId) ?? new SubTask();
                TaskModel = IsCopy ? SubTaskService.Clone(context, referenceTask) : referenceTask;

                // Assign the predecessor option
                if (TaskModel.Predecessor != null) selectedPredecessorId = TaskModel.Predecessor.SubTaskId;

                // Assign resources
                foreach (var r in TaskModel.AssignedResources)
                {
                    dataGridEntities.Add(r);
                }
            }
            else
            {
                TaskModel = new SubTask();
            }

            // Populate predecessor dropdown source (exclude self)
            predecessorTasks = ProjectModel.SubTasks
                .Where(x => x.SubTaskId != TaskModel.SubTaskId && x.Predecessor?.SubTaskId != TaskModel.SubTaskId).ToList();

            // Subscribe listeners
            TaskModel.TaskTypeChanged += UpdateUIState;
            TaskModel.FixedStartChanged += UpdateUIState;
            TaskModel.EndDateDrivenChanged += UpdateUIState;
            TaskModel.DoneChanged += UpdateUIState;
            UpdateUIState(TaskModel, new EventArgs());

            // Assign edit context
            editContext = new EditContext(TaskModel);
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
            startDateDisabled = !TaskModel.HasFixedStart || TaskModel.IsDone;
            workDisabled = TaskModel.TaskType == TaskType.FixedDuration || TaskModel.IsDone;
            durationDisabled = TaskModel.TaskType == TaskType.FixedWork || TaskModel.TaskType == TaskType.FixedDuration && TaskModel.HasFixedEndDate || TaskModel.IsDone;
        }

        private async void DeleteSubTask()
        {
            if (TaskId > 0)
            {
                bool confirmed = await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete task {TaskModel.Name} from project {ProjectModel?.GetFullName()}");
                if (confirmed)
                {
                    LogWarning($"Deleting task {TaskModel?.Name} on {ProjectModel?.GetFullName()}");

                    // Call delete on the subtask service and let it remove the resources
                    SubTaskService.Delete(context, TaskModel);

                    // Remove the sub-task from the project model
                    ProjectModel.SubTasks.Remove(TaskModel);

                    // Update the project summary values
                    ProjectModel.UpdateProjectSummary();

                    // Update the project in the database
                    ProjectService.Update(context, ProjectModel);

                    // Return to the project details page
                    Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }

        /// <summary>
        /// Method to change the resource day rate when selected
        /// </summary>
        /// <param name="value"></param>
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
                if (resourceToChange.UseProjectDayRate)
                {
                    resourceToChange.DayRate = ProjectModel.DayRate;
                }
            }
        }

        protected override void CancelEdit(Resource resource)
        {
            LogInformation($"Cancel edit row for {resource.GetSensibleObjectName()}");
            Reset();
            SubTaskService.RestoreModel(context, ref resource);
            dataGrid.CancelEditRow(resource);
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        protected override void OnCreateRow(Resource resource)
        {
            LogInformation($"Created new row for {resource.GetSensibleObjectName()}");
            dataGridEntities.Add(resource);
            entityToInsert = null;
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        protected override void OnUpdateRow(Resource entity)
        {
            Reset();
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        protected override async Task DeleteRow(Resource entity)
        {
            await base.DeleteRow(entity);
            UpdatePeopleDropdownSource(new LoadDataArgs());
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        protected override async Task SaveRow(Resource entity)
        {
            await base.SaveRow(entity);
            UpdatePeopleDropdownSource(new LoadDataArgs());
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        protected override async Task InsertRow()
        {
            await base.InsertRow();
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        protected override async Task EditRow(Resource entity)
        {
            await base.EditRow(entity);
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding task changes!");
            Navigation.NavigateTo($"projectdetails/{ProjectModel.ProjectId}");
        }

        /// <summary>
        /// Update the sub task properties but doesn't save to the database
        /// </summary>
        public void UpdateSubTask()
        {
            // Don't update the scheduling if the task is done
            if (TaskModel.IsDone)
            {
                LogInformation("Not updating sub task as it is marked as Done...");
            }
            else
            {
                LogInformation("Validating the sub task model...");
                editContext?.Validate();

                LogInformation("Updating sub task configuration...");

                // Update the resources on the task model to match the data grid entities
                TaskModel.AssignedResources.Clear();
                foreach (var r in dataGridEntities)
                {
                    Debug.WriteLine($"** Active Resource: ResId: {r.ResourceId} | PersonId: {r.Person.PersonId} | FTE: {r.AssignmentFTE} | Rate: {r.DayRate}");
                    TaskModel.AssignedResources.Add(r);
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
                foreach (var r in TaskModel.AssignedResources)
                {
                    var person = people.FirstOrDefault(x => x.PersonId == r.Person.PersonId);
                    if (person == null) continue;
                    // User the default day rate for the project if the assigned day rate is null
                    averageCostPerDayOfResources += (r.AssignmentFTE * (r.DayRate ?? ProjectModel.DayRate)) / totalResourceDaysPerDay;
                }

                // Update the actual cost for the sub task
                // Truncate to 2 DP
                TaskModel.ActualCost = Math.Round(TaskModel.ActualWorkHours * averageCostPerDayOfResources * 100 / 7) / 100;

                // Update predecessor task
                TaskModel.Predecessor = ProjectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == selectedPredecessorId);

                // Schedule
                error = TaskModel.Schedule(false, ProjectModel);
                IsValid = error == null;

                // Call schedule() on the subtask that this is a predecssor for
                var error2 = SubTaskService.UpdateFollowerTasks(TaskModel, ProjectModel);
                if (error2 != null)
                {
                    error = $"{error2.Item1}: {error2.Item2}";
                    IsValid = false;
                }

                if (!SubTaskService.IsUniqueTaskNameInProject(ProjectModel, TaskModel))
                {
                    error = "Task name must be unique within the project";
                    IsValid = false;
                };

                if (TaskModel.Demand <= 0)
                {
                    error = "Demand for a task must be greater than zero!";
                    IsValid = false;
                }

                if (TaskModel.TaskType == TaskType.FixedWork && TaskModel.PlannedWorkHours == 0)
                {
                    error = "Fixed work tasks must have a value of work greater than zero!";
                    IsValid = false;
                }

                if (TaskModel.TaskType == TaskType.FixedDuration && TaskModel.DurationDays == 0)
                {
                    error = "Fixed duration tasks must have a value of duration greater than zero!";
                    IsValid = false;
                }
            }

            // Update UI
            StateHasChanged();
        }

        /// <summary>
        /// Handles the edit form submission. Can called by owning components.
        /// </summary>
        public void HandleSubmit()
        {
            if (ProjectModel != null)
            {
                UpdateSubTask();
                if (IsValid)
                {
                    LogInformation("Saving sub task...");

                    // Add new new to task list for project if it is a new one
                    if (TaskModel.SubTaskId <= 0)
                    {
                        // Add the subtask to the database
                        SubTaskService.Add(context, TaskModel);

                        // Add reference to the project
                        ProjectModel.SubTasks.Add(TaskModel);
                    }
                    else
                    {
                        // Update the sub task in the database
                        SubTaskService.Update(context, TaskModel);
                    }

                    // Update the project summary values
                    ProjectModel.UpdateProjectSummary();

                    // Update the project in the database
                    ProjectService.Update(context, ProjectModel);

                    // Return to the project details page if not triggered from a split task page
                    if (!IsSplit) Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }

        /// <summary>
        /// Method to update the source for the resource dropdown to filter out based on search text
        /// </summary>
        /// <param name="args"></param>
        void UpdatePeopleDropdownSource(LoadDataArgs args)
        {
            var temp = people.AsQueryable();
            if (!string.IsNullOrEmpty(args.Filter))
            {
                temp = temp.Where(p => p.Name.ToLower().Contains(args.Filter.ToLower()));
            }

            // Remove any people already selected as resources
            filteredPeople = temp.Where(x => !dataGridEntities.Any(y => y.Person.PersonId == x.PersonId)).ToList();
            InvokeAsync(StateHasChanged);
        }
    }
}
