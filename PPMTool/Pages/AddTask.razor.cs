using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data.Context;
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
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Parameter]
        public int? TaskId { get; set; }

        [Parameter]
        [SupplyParameterFromQuery(Name = "copy")]
        public bool IsCopy { get; set; }

        [Parameter]
        public bool IsSplit { get; set; }

        private SubTask taskModel;
        public SubTask TaskModel
        {
            get => taskModel;
            private set
            {
                if (value != taskModel)
                {
                    taskModel = value;
                }
            }
        }

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
            InitialiseComponent();
        }

        /// <summary>
        /// Initialises the component with the project and task models.
        /// </summary>
        /// <param name="referenceContext">Overwrite the current context with a new context of your choice (erase tracking information)</param>
        /// <param name="restoreModels">Restore the model based on its current context object</param>
        public void InitialiseComponent(PPMToolContext referenceContext = null, bool restoreModels = true)
        {
            // Overwrite the context
            if (referenceContext != null && referenceContext != Context)
            {
                Context = referenceContext;
            }

            people = PersonService.GetAll(Context)
                .Where(x => x.EndDate == null || x.EndDate >= DateTime.Now)
                .OrderBy(x => x.Name)
                .ToList();
            taskTypes = Enum.GetValues<TaskType>().ToList();

            // Get project model from DB and manually restore it in case it has been modified elsewhere
            ProjectModel = ProjectService.GetById(Context, ProjectId);
            if (restoreModels)
            {
                ProjectService.RestoreModel(Context, ref projectModel);
            }

            // No project then stop initialising
            if (ProjectModel == null)
            {
                LogError($"Project model failed to initialise! ID = {ProjectId}");
                return;
            }

            // Initialise sub tasks
            if (ProjectModel.SubTasks == null)
            {
                ProjectModel.SubTasks = new List<SubTask>();
            }

            // Initialise data grid entities
            dataGridEntities = new List<Resource>();

            // Load task and related data
            if (TaskId != null && TaskId > 0)
            {
                // Get task and restore it in case it has been modified elsewhere
                var referenceTask = ProjectModel.SubTasks.FirstOrDefault(x => x.SubTaskId == TaskId) ?? new SubTask();
                if (restoreModels) SubTaskService.RestoreModel(Context, ref referenceTask);

                Debug.WriteLine($"** Reference Task: Start: {referenceTask.StartDate.ToShortDateString()} | End: {referenceTask.EndDate.ToShortDateString()} | Work: {referenceTask.PlannedWorkHours} | Duration: {referenceTask.DurationDays}");

                // Clone the reference task if copying
                TaskModel = IsCopy ? SubTaskService.Clone(Context, referenceTask) : referenceTask;

                // Assign the predecessor option
                InitialisePredecessorBinding();

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
                .Where(x => x.SubTaskId != TaskModel.SubTaskId && x.Predecessor?.SubTaskId != TaskModel.SubTaskId)
                .OrderBy(x => x.EndDate)
                .ToList();

            // Subscribe listeners
            TaskModel.TaskTypeChanged += UpdateUIState;
            TaskModel.FixedStartChanged += UpdateUIState;
            TaskModel.EndDateDrivenChanged += UpdateUIState;
            TaskModel.DoneChanged += UpdateUIState;
            UpdateUIState(TaskModel, new EventArgs());

            // Assign edit context
            editContext = new EditContext(TaskModel);

            // If editing or adding a task, only allow the project manager of the owning project to do it or a superuser
            var user = AuthenticationState?.User;
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            EditAuthorised = (user?.IsInRole("Superuser") ?? false) || ((user?.IsInRole("Manager") ?? false) && ProjectModel.ProjectManager == role?.Person);

            LogInformation(TaskModel.SubTaskId > 0 ? $"Editing task {TaskModel?.Name} on {ProjectModel?.GetFullName()} | Copy = {IsCopy}" : $"Adding new task to {ProjectModel?.GetFullName()}");
        }

        /// <summary>
        /// Initialise the binding of the predecessor ID in the dropdown
        /// </summary>
        public void InitialisePredecessorBinding()
        {
            if (TaskModel.Predecessor != null)
            {
                selectedPredecessorId = TaskModel.Predecessor.SubTaskId;
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            // If no project then navigate away
            if (ProjectModel == null) Navigation.NavigateTo("nothinghere");
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
            startDateDisabled = !TaskModel.HasFixedStart;
            workDisabled = TaskModel.TaskType == TaskType.FixedDuration;
            durationDisabled = TaskModel.TaskType == TaskType.FixedWork || TaskModel.TaskType == TaskType.FixedDuration && TaskModel.HasFixedEndDate;
        }

        private async void DeleteSubTask()
        {
            if (TaskId != null && TaskId > 0)
            {
                bool confirmed = await DialogService.Confirm($"You are about to delete task {TaskModel.Name} from project {ProjectModel?.GetFullName()}",
                    "Delete Task") ?? false;
                if (confirmed)
                {
                    LogWarning($"Deleting task {TaskModel?.Name} on {ProjectModel?.GetFullName()}");

                    // Call delete on the subtask service and let it remove the resources
                    SubTaskService.Delete(Context, TaskModel);

                    // Remove the sub-task from the project model
                    ProjectModel.SubTasks.Remove(TaskModel);

                    // Update the project summary values
                    var finrefs = FinancialReferenceService.GetAll(Context);
                    ProjectModel.UpdateProjectMetaData(false, finrefs);

                    // Update the project in the database
                    ProjectService.Update(Context, ProjectModel);

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
            SubTaskService.RestoreModel(Context, ref resource);
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
        /// Updates the resources on the subtask model from the data grid and validates
        /// </summary>
        public void UpdateSubTaskModelFromResourceDataGrid()
        {
            LogInformation("Validating the sub task model...");
            editContext?.Validate();

            LogInformation("Updating sub task resources from data grid...");

            // Update the resources on the task model to match the data grid entities
            TaskModel.AssignedResources.Clear();
            foreach (var r in dataGridEntities)
            {
                Debug.WriteLine($"** Active Resource: ResId: {r.ResourceId} | PersonId: {r.Person.PersonId} | FTE: {r.AssignmentFTE} | Rate: {r.DayRate}");
                TaskModel.AssignedResources.Add(r);
            }

            // Update predecessor task
            TaskModel.Predecessor = ProjectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == selectedPredecessorId);

            LogInformation("Scheduling task...");

            // Schedule (updates planned work, duration etc.)
            error = TaskModel.Schedule(false, ProjectModel);

            LogInformation("Updating actual hours from resources...");

            // Update actual hours
            TaskModel.ActualWorkHours = 0;
            foreach (var res in TaskModel.AssignedResources)
            {
                TaskModel.ActualWorkHours += res.ActualWorkHours;
            }

            LogInformation("Updating costs...");

            // Update planned and actual costs from the resources now scheduling has completed
            var projectDayRate = ProjectModel.DayRate;
            var finref = FinancialReferenceService.GetFinancialReferenceForDate(Context, TaskModel.StartDate);
            TaskModel.UpdateSubTaskCosts(ProjectModel.CostModel, projectDayRate, finref);

            // Set validity based on scheduler result
            IsValid = error == null;

            // Call schedule() on the subtask that this is a predecssor for
            var error2 = SubTaskService.ScheduleFollowerTasks(TaskModel, ProjectModel);
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

            if (TaskModel.OriginalDemand <= 0)
            {
                error = "Original demand for a task must be greater than zero!";
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

            // Update UI
            StateHasChanged();
        }

        /// <summary>
        /// Handles the edit form submission. Can called by owning components.
        /// </summary>
        public async void HandleSubmit()
        {
            if (ProjectModel != null)
            {
                UpdateSubTaskModelFromResourceDataGrid();
                if (IsValid)
                {
                    // Warn of the fact that they are setting a zero demand
                    var confirmed = true;
                    if (TaskModel.Demand == 0)
                    {
                        var message = "You are about to set the demand for this task zero.";

                        if (TaskModel.AssignedResources.Count != 0)
                        {
                            message += " You also have resources assigned to this task despite its zero demand.";
                        }

                        message += " Are you sure you want to do this?";

                        confirmed = await DialogService.Confirm(message, "Zero Demand Task") ?? false;
                    }

                    // Bail early if they do not want to continue
                    if (!confirmed)
                    {
                        return;
                    }

                    LogInformation("Saving sub task...");

                    // Add new new to task list for project if it is a new one
                    if (TaskModel.SubTaskId <= 0)
                    {
                        // Add the subtask to the database
                        SubTaskService.Add(Context, TaskModel);

                        // Add reference to the project
                        ProjectModel.SubTasks.Add(TaskModel);
                    }
                    else
                    {
                        // Update the sub task in the database
                        SubTaskService.Update(Context, TaskModel);
                    }

                    // Update the project summary values
                    var finrefs = FinancialReferenceService.GetAll(Context);
                    ProjectModel.UpdateProjectMetaData(false, finrefs);

                    // Update the project in the database
                    ProjectService.Update(Context, ProjectModel);

                    // Return to the project details page if not triggered from a split task page
                    if (!IsSplit) Navigation.NavigateTo($"projectdetails/{ProjectId}");
                }
            }
        }

        /// <summary>
        /// Method to update the source for the resource dropdown to filter out based on search text
        /// </summary>
        /// <param name="args"></param>
        private void UpdatePeopleDropdownSource(LoadDataArgs args)
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

        /// <summary>
        /// Navigates to the split task page
        /// </summary>
        private void SplitSubTask()
        {
            Navigation.NavigateTo($"splittask/{projectModel.ProjectId}/{taskModel.SubTaskId}");
        }

        /// <summary>
        /// Return the EF context being used to track entities for this component
        /// </summary>
        /// <returns></returns>
        internal PPMToolContext GetContext()
        {
            return Context;
        }
    }
}
