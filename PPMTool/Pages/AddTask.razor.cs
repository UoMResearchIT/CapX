using System.Diagnostics;
using FluentDateTime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;
using Radzen.Blazor;
using static PPMTool.Shared.MainLayout;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddTask : DataGridPage<Resource>
    {
        /// <summary>
        /// A class representing a row of the actuals reporting grid
        /// </summary>
        public class ActualsReportRow
        {
            /// <summary>
            /// Resource associated with the cell
            /// </summary>
            public Person Resource { get; set; }

            /// <summary>
            /// Task to which the time was booked
            /// </summary>
            public InnateCodeTask Task { get; set; }

            /// <summary>
            /// Dictionary representing the weeks and values for this particular row
            /// </summary>
            public IDictionary<DateTime, double> Hours { get; set; } = new Dictionary<DateTime, double>();

            /// <summary>
            /// Total number of hours on the row
            /// </summary>
            public double RowTotal { get; set; }
        }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Inject]
        private TimesheetService TimesheetService { get; set; }

        [Inject]
        private SkillTagService SkillTagService { get; set; }

        [Inject]
        private FundingSourceService FundingSourceService { get; set; }

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

        private RadzenDataGrid<ActualsReportRow> actualsGrid;
        public RadzenDataGrid<ActualsReportRow> ActualsGrid
        {
            get => actualsGrid;
            set
            {
                if (value != actualsGrid)
                {
                    actualsGrid = value;
                    if (actualsGrid != null) UpdateActualsColumnSums();
                }
            }
        }

        public bool IsValid { get; private set; } = true;

        private int? selectedPredecessorId;
        private IList<Person> people = new List<Person>();
        private IList<Person> filteredPeople = new List<Person>();
        private IEnumerable<Rate> availableRates = new List<Rate>();
        private IEnumerable<FundingSource> availableSources = new List<FundingSource>();
        private bool startDateDisabled;
        private bool workDisabled;
        private bool durationDisabled;
        private bool endDateDisabled;
        private bool specifyEndDateDisabled;
        private bool defineByEndDate;
        private string error;
        private IEnumerable<TaskType> taskTypes = new List<TaskType>();
        private IList<SubTask> predecessorTasks = new List<SubTask>();
        private EditContext editContext;
        private List<ActualsReportRow> actualsReportRows = new List<ActualsReportRow>();
        private IDictionary<DateTime, double> actualsColumnSums = new Dictionary<DateTime, double>();
        private DateTime? actualsStartDate;
        private DateTime? actualsEndDate;
        private bool hideEmptyWeeks = false;
        private IEnumerable<SkillTag> availableTags;
        private string autoCompleteText;
        private bool actualsLoading = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // Initialise the component if not expecting manual initialisation
            if (!IsSplit)
            {
                await InitialiseComponentAsync();

                // Set up the buttons for the action bar
                Layout.SetButtons(
                [
                    new ActionButton
                    {
                        Icon = "refresh",
                        Text = "Update",
                        ButtonStyle = ButtonStyle.Light,
                        OnClick = UpdateSubTaskModelFromResourceDataGrid
                    },
                    new ActionButton
                    {
                        Text = "Update & Save",
                        OnClick = HandleSubmit,
                        Disabled = !EditAuthorised
                    },
                    new ActionButton
                    {
                        Icon = "close",
                        Text = "Discard",
                        ButtonStyle = ButtonStyle.Danger,
                        OnClick = DiscardChanges
                    }
                ]);
            }
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            Debug.WriteLine("** AddTask component rendering...");

            // If no project then navigate away if not being initialised manually
            if (!IsSplit && ProjectModel == null) Navigation.NavigateTo("nothinghere");
        }

        /// <summary>
        /// Initialises the component with the project and task models.
        /// </summary>
        /// <param name="referenceContext">Overwrite the current context with a new context of your choice (erase tracking information)</param>
        public async Task InitialiseComponentAsync(PPMToolContext referenceContext = null)
        {
            Debug.WriteLine("** Initialising AddTask component...");

            Loading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            // Overwrite the context
            if (referenceContext != null && referenceContext != Context)
            {
                Context.Dispose();
                Context = referenceContext;
            }

            // Get project model from DB and manually restore it in case it has been modified elsewhere
            ProjectModel = ProjectService.GetById(Context, ProjectId);
            ProjectService.RestoreModel(Context, ref projectModel);
            Debug.WriteLine("** ProjectModel loaded!");

            // Initialise the lists
            people = PersonService.GetAll(Context)
                .Where(x => x.EndDate == null || x.EndDate >= DateTime.Now)
                .OrderBy(x => x.Name)
                .ToList();
            taskTypes = Enum.GetValues<TaskType>().ToList();
            availableTags = SkillTagService.GetAll(Context);
            availableRates = Enum.GetValues<Rate>().ToList();
            availableSources = FundingSourceService.GetFundingSources(Context, ProjectId ?? 0).ToList();

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
                SubTaskService.RestoreModel(Context, ref referenceTask);

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
                TaskModel = new SubTask()
                {
                    OwningProject = ProjectModel
                };
            }

            // Initialise the defineByEndDate flag based on the fixed end date flag
            defineByEndDate = TaskModel.HasFixedEndDate;

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
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser || (ActiveUserRoleType == RoleType.Manager && ProjectModel.ProjectManager.PersonId == ActiveUser?.Person.PersonId);

            LogInformation(TaskModel.SubTaskId > 0 ? $"Editing task {TaskModel?.Name} on {ProjectModel?.GetFullName()} | Copy = {IsCopy} | Split = {IsSplit}" : $"Adding new task to {ProjectModel?.GetFullName()}");

            // Finished
            Loading = false;
            await InvokeAsync(StateHasChanged);

            // Run actuals report if not copying or splitting
            if (!IsCopy && !IsSplit)
            {
                await LoadActualsAsync();
            }
        }

        /// <summary>
        /// Method to generate the actuals report and populate the data grid
        /// </summary>
        /// <exception cref="Exception">Throw if there are multiple task/resource/week entries in the timesheet data</exception>
        private async Task<IList<ActualsReportRow>> LoadActualsAsync()
        {
            // Initialise
            var tempActuals = new List<ActualsReportRow>();

            // Update UI
            actualsLoading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            try
            {
                Debug.WriteLine("** Running actuals report...");

                // Get all the timesheet entries associated with the activity code for this project
                if (projectModel?.InnateActivity == null) return tempActuals;
                var timesheets = TimesheetService.GetAllForInnateCode(Context, projectModel.InnateActivity).Where(x => x.Status == TimesheetStatus.Approved);
                if (timesheets.Count() == 0) return tempActuals;

                // Find the earliest and latest timesheet weeks if no date set
                var startWeek = actualsStartDate ?? timesheets.Min(x => x.StartDate);
                var endWeek = actualsEndDate ?? timesheets.Max(x => x.StartDate);

                // Correct to start of week
                startWeek = startWeek.FirstDayOfWeek().Date;
                endWeek = endWeek.FirstDayOfWeek().Date;

                if (endWeek <= startWeek)
                {
                    endWeek = startWeek.AddDays(7);
                }

                // Revise timesheet selection based on date range selected
                timesheets = timesheets.Where(x => x.StartDate >= startWeek && x.StartDate <= endWeek);

                // Create a row for every unique resource - task combination
                foreach (var timesheet in timesheets)
                {
                    foreach (var entry in timesheet.TimesheetEntries)
                    {
                        // Ignore the tasks that do not match the activity for the project
                        if (entry.InnateCodeTask.InnateCode.InnateCodeId != projectModel.InnateActivity.InnateCodeId)
                        {
                            continue;
                        }

                        // See if we can find an existing row that matches the resource and task combination
                        var row = tempActuals
                            .FirstOrDefault(x => x.Resource.PersonId == timesheet.Owner.PersonId && x.Task.InnateCodeTaskId == entry.InnateCodeTask.InnateCodeTaskId);

                        // If not then create an empty object
                        if (row == null)
                        {
                            row = new ActualsReportRow
                            {
                                Resource = timesheet.Owner,
                                Task = entry.InnateCodeTask,
                                Hours = new Dictionary<DateTime, double>()
                            };
                            tempActuals.Add(row);
                        }

                        // Add the hours to the row
                        if (!row.Hours.ContainsKey(timesheet.StartDate))
                        {
                            entry.UpdateTotalHours();
                            row.Hours.Add(timesheet.StartDate, entry.TotalHours);
                            row.RowTotal += entry.TotalHours;
                        }
                        else
                        {
                            // This shouldn't happen as there should only be one entry for the week, resource, task combination
                            throw new Exception("Actuals report failed by finding duplicate week/resource/task combination in the timesheet database!");
                        }
                    }
                }

                // Fill in the blank weeks
                if (!hideEmptyWeeks)
                {
                    var currentWeek = startWeek;
                    Debug.WriteLine($"** Filling blanks between {startWeek.ToShortDateString()} and {endWeek.ToShortDateString()}");
                    while (currentWeek <= endWeek)
                    {
                        foreach (var row in tempActuals)
                        {
                            if (!row.Hours.ContainsKey(currentWeek))
                            {
                                row.Hours.Add(currentWeek, 0);
                            }
                        }
                        currentWeek = currentWeek.AddDays(7);
                    }
                }

                // Order and group the rows appropriately
                actualsReportRows = tempActuals
                    .OrderBy(x => x.Task.TaskName)
                    .ThenBy(x => x.Resource.Name)
                    .ToList();

            }
            catch (Exception ex)
            {
                LogError($"Actuals report failing!\n{ex}");
            }
            finally
            {
                actualsLoading = false;
                await InvokeAsync(StateHasChanged);
                Debug.WriteLine($"** ...finished updating actuals.");
            }

            return tempActuals;
        }

        /// <summary>
        /// Based on the currently visible data in the data grid, update the column sums with a new Dictionary
        /// </summary>
        /// <param name="data">The actuals data to use to inform the column sums. If null, use whatever data is visible in the data grid.</param>
        private void UpdateActualsColumnSums(IEnumerable<ActualsReportRow> data = null)
        {
            // Get only visible rows
            var actualsData = data ?? actualsGrid?.View;
            if (actualsData == null || actualsData.Count() == 0)
            {
                Debug.WriteLine($"** Cannot update the column sums as no data!");
                return;
            }

            Debug.WriteLine($"** Updating column sums...");

            // Setup the array using all weeks not just those visible
            var keys = actualsReportRows?.SelectMany(x => x.Hours.Keys).Distinct();
            IDictionary<DateTime, double> tempActualColumnSums = new Dictionary<DateTime, double>();

            // Loop over dates
            foreach (var week in keys)
            {
                // Reset
                tempActualColumnSums.Add(new KeyValuePair<DateTime, double>(week, 0));

                // Loop over each row and update
                foreach (var row in actualsData)
                {
                    if (row.Hours.ContainsKey(week))
                    {
                        tempActualColumnSums[week] += row.Hours[week];
                    }
                }
            }

            actualsColumnSums = tempActualColumnSums;

            Debug.WriteLine($"** ...finished updating column sums.");
        }

        /// <summary>
        /// Callback fired when the filter is applied or cleared
        /// </summary>
        private void ActualsReportFiltered()
        {
            UpdateActualsColumnSums();
        }

        /// <summary>
        /// Initialise the binding of the predecessor ID in the dropdown
        /// </summary>
        public void InitialisePredecessorBinding()
        {
            if (TaskModel.Predecessor != null)
            {
                Debug.WriteLine($"** Task {TaskModel.SubTaskId}: Setting selected predecessor ID to {TaskModel.Predecessor.SubTaskId}");
                selectedPredecessorId = TaskModel.Predecessor.SubTaskId;
                InvokeAsync(StateHasChanged);
            }
        }

        private string GetNiceString(Enum x)
        {
            return x.ToNiceString();
        }

        /// <summary>
        /// Handler for when the start date changes to ensure the end date is valid
        /// </summary>
        /// <param name="date"></param>
        private void OnStartDateChange(DateTime? date)
        {
            if (date.HasValue)
            {
                // If the start date is moved past the end date, update the end date to be the day after
                if (TaskModel.EndDate <= TaskModel.StartDate)
                {
                    // If there is a duration already then migth be convenient to use that to bump the end date?
                    TaskModel.EndDate = TaskModel.DurationDays > 0 ? TaskModel.StartDate.AddDays(TaskModel.DurationDays) : TaskModel.StartDate.AddDays(1);
                }
                UpdateUIState(TaskModel, new EventArgs());
            }
        }

        /// <summary>
        /// Handler for the events on the sub task to update various UI flags
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UpdateUIState(object sender, EventArgs e)
        {
            if (TaskModel.HasFixedStart)
            {
                selectedPredecessorId = null;
            }

            startDateDisabled = !TaskModel.HasFixedStart && selectedPredecessorId != null;
            workDisabled = TaskModel.TaskType == TaskType.FixedDuration;

            if (TaskModel.TaskType == TaskType.FixedWork)
            {
                specifyEndDateDisabled = true;
                defineByEndDate = false;
                endDateDisabled = true;
                durationDisabled = true;
            }
            else
            {
                specifyEndDateDisabled = false;

                // If define by end date is true then enable the end date picker and disable the duration picker
                endDateDisabled = !defineByEndDate;
                durationDisabled = defineByEndDate;
            }
        }

        /// <summary>
        /// Delete a subtask and clean up as part of the process
        /// </summary>
        private async void DeleteSubTask()
        {
            if (TaskId != null && TaskId > 0)
            {
                bool confirmed = await DialogService.Confirm($"You are about to delete task {TaskModel.Name} from project {ProjectModel?.GetFullName()}",
                    "Delete Task") ?? false;
                if (confirmed)
                {
                    LogWarning($"Task {TaskModel?.SubTaskId}: Deleting task {TaskModel?.Name} on {ProjectModel?.GetFullName()}");

                    // Call delete on the subtask service and let it remove the resources
                    SubTaskService.Delete(Context, TaskModel);

                    // Remove the sub-task from the project model
                    ProjectModel.SubTasks.Remove(TaskModel);

                    // Update the project summary values
                    var finrefs = FinancialReferenceService.GetAll(Context);
                    ProjectModel.UpdateProjectMetaData(false, finrefs);

                    // Update the project in the database
                    LogInformation($"Saving project {ProjectModel?.GetFullName()}...");
                    ProjectService.Update(Context, ProjectModel);

                    // Return to the project details page
                    Navigation.NavigateTo($"projects/projectdetails/{ProjectId}");
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

        /// <summary>
        /// Cancel the row edit and restore the model modifications
        /// </summary>
        /// <param name="resource"></param>
        protected override void CancelEdit(Resource resource)
        {
            LogInformation($"Task {TaskModel?.SubTaskId}: Cancel edit row for {resource.GetSensibleObjectName()}");

            // Reset the entity tracking
            Reset();
            SubTaskService.RestoreModel(Context, ref resource);
            dataGrid.CancelEditRow(resource);

            // Update UI elements
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        /// <summary>
        /// Create a new row in the datagrid
        /// </summary>
        /// <param name="resource"></param>
        protected override void OnCreateRow(Resource resource)
        {
            LogInformation($"Task {TaskModel?.SubTaskId}: Created new row for {resource.GetSensibleObjectName()}");

            // Add to the data grid
            dataGridEntities.Add(resource);

            // Reset the entity tracking
            entityToInsert = null;

            // Update UI elements
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        /// <summary>
        /// Remove the chosen resource from the available list
        /// </summary>
        /// <param name="entity"></param>
        protected override void OnUpdateRow(Resource entity)
        {
            // Reset the entity tracking
            Reset();

            // Update UI elements
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        /// <summary>
        /// Remove a resource row from the data grid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task DeleteRow(Resource entity)
        {
            await base.DeleteRow(entity);

            // Update UI elements
            UpdatePeopleDropdownSource(new LoadDataArgs());

            // Update task unmet demand
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        /// <summary>
        /// Save the row in the data grid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task SaveRow(Resource entity)
        {
            // Ensure the resource has the subtask set to allow following calculations
            if (entity.SubTask == null)
            {
                entity.SubTask = TaskModel;
            }

            // Update the billed FTE
            entity.UpdateBilledFTE(projectModel.CostModel);

            // Save the row to the DB
            await base.SaveRow(entity);

            // Update UI elemnts
            UpdatePeopleDropdownSource(new LoadDataArgs());

            // Update task unmet demand
            TaskModel.UpdateUnmetDemand(dataGridEntities);
        }

        /// <summary>
        /// Insert a new row in the data grid
        /// </summary>
        /// <returns></returns>
        protected override async Task InsertRow()
        {
            await base.InsertRow();

            // Update UI elements
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        /// <summary>
        /// Edit a row in the data grid
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override async Task EditRow(Resource entity)
        {
            await base.EditRow(entity);

            // Update UI elements
            UpdatePeopleDropdownSource(new LoadDataArgs());
        }

        /// <summary>
        /// Discard changes to the page
        /// </summary>
        private void DiscardChanges()
        {
            LogInformation($"Task {TaskModel?.SubTaskId}: Discarding task changes!");
            Navigation.NavigateTo($"projects/projectdetails/{ProjectModel.ProjectId}");
        }

        /// <summary>
        /// Updates the resources on the subtask model from the data grid and validates
        /// </summary>
        public void UpdateSubTaskModelFromResourceDataGrid()
        {
            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Validating the sub task model...");

            // Reset validation state
            error = null;
            IsValid = true;
            ClearErrorMessage();

            // Validate the edit form and present errors if necessary
            editContext?.Validate();
            var messages = editContext?.GetValidationMessages();
            if (messages?.Count() > 0)
            {
                error = messages.First();
                IsValid = false;
                SetErrorMessage(new StatusMessage(error, StatusMessage.MessageType.Error));
                return;
            }

            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Task validation OK. Updating sub task resources from data grid...");

            // Update the resources on the task model to match the data grid entities
            TaskModel.AssignedResources.Clear();
            foreach (var r in dataGridEntities)
            {
                // Copies do not have a task model attached so attach here
                if (r.SubTask == null)
                {
                    r.SubTask = TaskModel;
                }

                Debug.WriteLine($"** Active Resource: ResId: {r.ResourceId} | PersonId: {r.Person.PersonId} | FTE: {r.AssignmentFTE} | Rate: {r.DayRate}");
                TaskModel.AssignedResources.Add(r);
            }

            // Update predecessor task
            Debug.WriteLine($"** Task {TaskModel.SubTaskId}: Setting predecessor task with ID = {selectedPredecessorId}");
            TaskModel.Predecessor = ProjectModel.SubTasks.FirstOrDefault(s => s.SubTaskId == selectedPredecessorId);

            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Scheduling task...");

            // Before we schedule, ensure the duration uses the predecessor-driven start date if needed.
            if (defineByEndDate)
            {
                if (TaskModel.Predecessor != null && !TaskModel.HasFixedStart)
                {
                    TaskModel.StartDate = TaskModel.Predecessor.EndDate.Date.AddDays(TaskModel.Lag + 1);
                }

                TaskModel.RecalculateDurationFromDates();
            }
            else
            {
                TaskModel.RecalculateEndDateFromDuration();
            }

            // Schedule (updates planned work, duration etc.)
            error = TaskModel.Schedule();

            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Updating actual hours from resources...");

            // Update actual hours
            TaskModel.ActualWorkHours = 0;
            foreach (var res in TaskModel.AssignedResources)
            {
                TaskModel.ActualWorkHours += res.ActualWorkHours;
            }

            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Updating costs...");

            // Update planned and actual costs from the resources now scheduling has completed
            var projectDayRate = ProjectModel.DayRate;
            var finrefs = FinancialReferenceService.GetAll(Context);
            TaskModel.UpdateSubTaskCosts(ProjectModel, finrefs);

            // Set validity based on scheduler result
            IsValid = error == null;

            // Call schedule() on the subtask that this is a predecssor for
            var error2 = SubTaskService.ScheduleFollowerTasks(TaskModel, ProjectModel);
            if (error2 != null)
            {
                error = $"{error2.Item1}: {error2.Item2}";
                IsValid = false;
            }

            // Check that assigned resources are managers
            if (TaskModel.IsLeadershipTask)
            {
                var managerIds = UserService.GetAllManagerPersonId(Context);
                if (TaskModel.AssignedResources.Any(x => !managerIds.Contains(x.Person.PersonId)))
                {
                    error = "Only managers can be assigned to leadership tasks";
                    IsValid = false;
                }
            }

            if (!SubTaskService.IsUniqueTaskNameInProject(ProjectModel, TaskModel))
            {
                error = "Non-leadership task names must be unique within the project";
                IsValid = false;
            }

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

            // Set the action bar error
            if (!IsValid)
            {
                SetErrorMessage(new StatusMessage($"Task configuration is invalid: {error}", StatusMessage.MessageType.Error));
            }

            Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: ...Validation complete!");

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

                // If valid from the schedule and resource update then carry on and try to save
                if (IsValid)
                {
                    // Warn of the fact that they are setting a zero demand
                    var confirmed = true;
                    if (TaskModel.Demand == 0)
                    {
                        var message = "You are about to set the demand for this task to zero.";

                        if (TaskModel.AssignedResources.Count != 0)
                        {
                            message += " You also have resources assigned to this task despite its zero demand.";
                        }

                        message += " Are you sure you want to do this?";

                        confirmed = await DialogService.Confirm(message, "Zero Demand Task") ?? false;
                    }

                    // Set error if they do not want to continue
                    if (!confirmed)
                    {
                        if (TaskModel.AssignedResources.Count != 0)
                        {
                            IsValid = false;
                            error = "Task has zero demand but has resources assigned!";
                        }
                    }

                    // Fail if demand, original demand or assigned resources are assigned less than 3 DP
                    if (TaskModel.Demand != 0 && HasDigitsAfterThirdDecimalPlace(TaskModel.Demand))
                    {
                        IsValid = false;
                        error = "Demand has digits after the third decimal place which is not allowed!";
                    }
                    if (HasDigitsAfterThirdDecimalPlace(TaskModel.OriginalDemand))
                    {
                        IsValid = false;
                        error = "Original Demand has digits after the third decimal place which is not allowed!";
                    }
                    if (TaskModel.AssignedResources.Any(x => HasDigitsAfterThirdDecimalPlace(x.AssignmentFTE)))
                    {
                        IsValid = false;
                        error = "One or more resources have Assignment FTE with digits after the third decimal place which is not allowed!";
                    }

                    // Set the error message and exit early
                    if (!IsValid)
                    {
                        SetErrorMessage(new StatusMessage(error, StatusMessage.MessageType.Error));
                        return;
                    }

                    LogInformation($"Task {TaskModel?.SubTaskId}: Saving sub task...");

                    // Add reference to the project
                    TaskModel.OwningProject = ProjectModel;
                    Debug.WriteLine($"** Task {TaskModel?.SubTaskId}: Owning project ID = {taskModel.OwningProject?.ProjectId}");

                    // Add new new to task list for project if it is a new one
                    if (TaskModel.SubTaskId <= 0)
                    {
                        // Add the subtask to the database
                        SubTaskService.Add(Context, TaskModel);
                    }
                    else
                    {
                        // Update the sub task in the database
                        SubTaskService.Update(Context, TaskModel);
                    }

                    // Update the project summary values if not splitting as that is taken care of on the split task page
                    if (!IsSplit)
                    {
                        var finrefs = FinancialReferenceService.GetAll(Context);
                        ProjectModel.UpdateProjectMetaData(false, finrefs);

                        // Update the project in the database
                        LogInformation($"Task {TaskModel?.SubTaskId}: Saving project {ProjectModel?.GetFullName()}...");
                        ProjectService.Update(Context, ProjectModel);

                        // Return to the project details page if not triggered from a split task page
                        Navigation.NavigateTo($"projects/projectdetails/{ProjectId}");
                    }
                }
            }
            else
            {
                LogError($"Task {TaskModel?.SubTaskId}: Cannot save task as it has no project model!");
            }
        }

        /// <summary>
        /// Method to check whether there are any digits after the third decimal place
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        private bool HasDigitsAfterThirdDecimalPlace(double number)
        {
            double truncatedNumber = Math.Truncate(number * 1000) / 1000;
            return number != truncatedNumber;
        }


        /// <summary>
        /// Method to update the source for the resource dropdown to filter out based on search text
        /// </summary>
        /// <param name="args"></param>
        private void UpdatePeopleDropdownSource(LoadDataArgs args)
        {
            var temp = people.AsQueryable();
            if (taskModel.IsLeadershipTask)
            {
                var managerId = UserService.GetAllManagerPersonId(Context);
                temp = temp.Where(x => managerId.Contains(x.PersonId));
            }

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
            Navigation.NavigateTo($"projects/splittask/{projectModel.ProjectId}/{taskModel.SubTaskId}");
        }

        /// <summary>
        /// Return the EF context being used to track entities for this component
        /// </summary>
        /// <returns></returns>
        internal PPMToolContext GetContext()
        {
            return Context;
        }

        /// <summary>
        /// When the search box is changed
        /// </summary>
        /// <param name="args"></param>
        void OnChange(dynamic args)
        {
            var match = availableTags.FirstOrDefault(x => x.Name.Trim() == autoCompleteText.Trim());
            if (match != null && !TaskModel.SkillsRequired.Any(x => x.SkillTagId == match.SkillTagId))
            {
                TaskModel.SkillsRequired.Add(match);
                ClearSearch();
                StateHasChanged();
            }
        }

        /// <summary>
        /// Simply clear the search box
        /// </summary>
        private void ClearSearch()
        {
            autoCompleteText = string.Empty;
        }

        /// <summary>
        /// Remove the skill from the current model
        /// </summary>
        /// <param name="skill"></param>
        private void RemoveSkill(SkillTag skill)
        {
            TaskModel.SkillsRequired.Remove(skill);
        }
    }
}
