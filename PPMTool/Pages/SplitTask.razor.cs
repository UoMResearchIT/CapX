// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class SplitTask : BasePage
    {
        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Parameter]
        public int? SubTaskId { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        private AddTask originalAddTaskComponent;
        private AddTask newAddTaskComponent;
        private SubTask originalTask;
        private Project owningProject;
        private bool splitOnDate = true;
        private ActualsLogic selectedActualsLogic;
        private DateTime? splitDate;
        private double? splitValue;
        private List<StatusMessage> statusMessages = new List<StatusMessage>();
        private double origProportion = 0;
        private DateTime originalStartDate;
        private DateTime originalEndDate;
        private bool showTaskComponents;
        private bool showTaskInvalidError;
        private bool splitPending = false;
        private bool disableButtons = false;

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {
                Task.Run(LoadDataAsync);
            }

            Debug.WriteLine($"** SplitTask Page Rendered! Split pending = {Loading} | OriginalTaskComponentId = {originalAddTaskComponent?.TaskModel?.SubTaskId} | NewTaskComponentId = {newAddTaskComponent?.TaskModel?.SubTaskId}");
        }

        /// <summary>
        /// Loads the initial page data from the parameters
        /// </summary>
        /// <returns></returns>
        private async Task LoadDataAsync()
        {
            Debug.WriteLine("** Loading data...");
            Loading = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            // Initialise the original task and project for the meta data
            originalTask = SubTaskService.GetShallowById(Context, SubTaskId);
            owningProject = ProjectService.GetById(Context, ProjectId);
            originalStartDate = originalTask?.StartDate ?? DateTime.Today;
            originalEndDate = originalTask?.EndDate ?? DateTime.Today;
            LogInformation($"Splitting task {originalTask?.Name} on {owningProject?.GetSensibleObjectName()}");

            // Only allow the project manager to save the split or a superuser
            EditAuthorised = ActiveUserRoleType == RoleType.Superuser || owningProject?.ProjectManager.PersonId == ActiveUser?.Person?.PersonId;

            // Add status message
            statusMessages.Add(new StatusMessage("Set your parameters and click Split Task to configure the two halves of the tasks automatically!", StatusMessage.MessageType.Warning, () => !showTaskComponents));

            // Finish loading
            Loading = false;
            await InvokeAsync(StateHasChanged);
            Debug.WriteLine("** ...Finished loading data.");
        }

        /// <summary>
        /// Button callback when options chosen and user wants to initialise the components
        /// </summary>
        private void InitialiseTaskComponentsClicked()
        {
            // Show the hidden tasks to get the references to bind
            showTaskComponents = true;
            StateHasChanged();
            Task.Run(SplitTasksAsync);
        }

        /// <summary>
        /// Determines whether the user has provided the input required to split the task
        /// </summary>
        /// <returns></returns>
        private bool UserInputProvided()
        {
            return (splitOnDate && splitDate != null) || (!splitOnDate && splitValue != null);
        }

        /// <summary>
        /// Splits the tasks by initialising the components and following through the logic
        /// </summary>
        private async Task SplitTasksAsync()
        {
            if (splitPending)
            {
                return;
            }

            // Wait for the binding of the components
            while (originalAddTaskComponent == null || newAddTaskComponent == null)
            {
                await Task.Delay(100);
                Debug.WriteLine("** Waiting for components to bind...");
            }

            // Set the loading spinners on the components
            splitPending = true;
            await InvokeAsync(StateHasChanged);
            await Task.Yield();

            Debug.WriteLine($"** Running split logic...");

            try
            {
                // Clear the error messages
                statusMessages.Clear();
                showTaskInvalidError = false;

                // Initialise the components from the DB (share the page context with the two add task components)
                await originalAddTaskComponent.InitialiseComponentAsync(Context);
                await newAddTaskComponent.InitialiseComponentAsync(originalAddTaskComponent.GetContext());

                // Apply the logic to split the task and actuals
                ApplySplitLogic();

                // Update and schedule the sub tasks (needs to be on UI thread for edit context validation)
                await InvokeAsync(UpdateSubTasks);

                // Update the actuals
                UpdateActuals();

                // Call update subtasks again to update the actuals cost
                await InvokeAsync(UpdateSubTasks);

                // Check for fixed work warnings
                CheckForFixedWorkWarnings();

                // Set the original task as the predecessor of the new task
                newAddTaskComponent.TaskModel.HasFixedStart = false;
                Debug.WriteLine($"** Setting original task as predecessor to new task...");
                newAddTaskComponent.TaskModel.Predecessor = originalAddTaskComponent.TaskModel;
                newAddTaskComponent.InitialisePredecessorBinding();

                // Find the tasks for which the original task was the predecessor and update them to have the new task as its predecessor
                Debug.WriteLine($"** Successors on original task = {originalAddTaskComponent.TaskModel.Successors.Count}");
                foreach (var task in originalAddTaskComponent.TaskModel.Successors)
                {
                    task.Predecessor = newAddTaskComponent.TaskModel;
                }

                splitPending = false;
                showTaskComponents = true;
                await InvokeAsync(StateHasChanged);
                Debug.WriteLine($"** Split complete. {statusMessages.Count} status message(s).");
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Exception when splitting tasks! {e}");
            }
        }

        /// <summary>
        /// Method to check whether there are fixed work warnings required and adds a status message
        /// </summary>
        private void CheckForFixedWorkWarnings()
        {
            // Check for error on fixed work scheduling
            if (splitDate != null && originalAddTaskComponent.TaskModel.TaskType == TaskType.FixedWork &&
                (originalAddTaskComponent.TaskModel.EndDate != splitDate.Value.AddDays(-1) ||
                originalAddTaskComponent.TaskModel.EndDate == newAddTaskComponent.TaskModel.StartDate))
            {
                statusMessages.Add(new StatusMessage("Due to fixed work scheduling, the splitter has been unable to split the task automatically at the date you have specified and will need manual adjustment.", StatusMessage.MessageType.Warning, () => true));
                return;
            }

            if (originalAddTaskComponent.TaskModel.TaskType == TaskType.FixedWork && newAddTaskComponent.TaskModel.EndDate != originalEndDate)
            {
                statusMessages.Add(new StatusMessage("Due to fixed work scheduling, the splitter has been unable to split the task automatically so that the new task ends on the same day as the unsplit task and will need manual adjustment.", StatusMessage.MessageType.Warning, () => true));
                return;
            }
        }

        /// <summary>
        /// Updates the components to reflect the split processing
        /// </summary>
        private void ApplySplitLogic()
        {
            // Store some original values before modification
            var originalWork = originalAddTaskComponent.TaskModel.PlannedWorkHours;
            var originalDuration = originalAddTaskComponent.TaskModel.DurationDays;

            Debug.WriteLine($"** Original Config: {originalAddTaskComponent.TaskModel.PlannedWorkHours} hours work | {originalAddTaskComponent.TaskModel.DurationDays} days duration");

            double proposedDurationOrigTask = 0;
            double proposedDurationNewTask = 0;

            // Configure the split date and value as required and validate choices
            if (splitOnDate)
            {
                // Check we have a split date specified
                if (splitDate == null)
                {
                    statusMessages.Add(new StatusMessage("Please select a date to split the task on and try to split again!", StatusMessage.MessageType.Error, () => true));
                    return;
                }

                // Split date must be the day after the start of the original task for it to have a non-zero duration
                // Split date must be at least the same day as the end of the new task for it to have a non-zero duration
                proposedDurationOrigTask = (splitDate - originalAddTaskComponent.TaskModel.StartDate).Value.TotalDays;
                proposedDurationNewTask = (newAddTaskComponent.TaskModel.EndDate - splitDate).Value.TotalDays + 1;
                if (proposedDurationOrigTask < 1 || proposedDurationNewTask < 1)
                {
                    AddBadDurationStatusMessage(proposedDurationOrigTask, proposedDurationNewTask);
                    return;
                }
            }
            else
            {
                // Check we have a split value
                if (splitValue == null)
                {
                    statusMessages.Add(new StatusMessage("Please specify the number of days to split the task by and try to split again!", StatusMessage.MessageType.Error, () => true));
                    return;
                }

                // Duration of both tasks in days must be greater than zero
                proposedDurationOrigTask = splitValue ?? 0;
                proposedDurationNewTask = originalAddTaskComponent.TaskModel.DurationDays - splitValue ?? 0;
                if (proposedDurationOrigTask < 1 || proposedDurationNewTask < 1)
                {
                    AddBadDurationStatusMessage(proposedDurationOrigTask, proposedDurationNewTask);
                    return;
                }

                // Set split date
                splitDate = originalAddTaskComponent.TaskModel.StartDate.AddDays(proposedDurationOrigTask);
            }

            Debug.WriteLine($"** Proposal: Orig Duration = {proposedDurationOrigTask} | New Duration = {proposedDurationNewTask} | Split Date = {splitDate?.ToString("dd/MM/yyyy")}");

            // Get proportion of split based on duration
            origProportion = proposedDurationOrigTask / originalDuration;

            // Reset new task settings to match original
            newAddTaskComponent.TaskModel.TaskType = originalAddTaskComponent.TaskModel.TaskType;
            newAddTaskComponent.TaskModel.HasFixedEndDate = originalAddTaskComponent.TaskModel.HasFixedEndDate;

            // Set the start date of the new task based on the split
            newAddTaskComponent.TaskModel.StartDate = splitDate ?? DateTime.Today;

            // Fixed work tasks drive their own duration or end date so the number of hours of work needs to be estimated
            if (originalAddTaskComponent.TaskModel.TaskType == TaskType.FixedWork)
            {
                originalAddTaskComponent.TaskModel.PlannedWorkHours = Math.Round(10 * originalWork * origProportion) / 10;
                newAddTaskComponent.TaskModel.PlannedWorkHours = Math.Round(10 * (originalWork - originalAddTaskComponent.TaskModel.PlannedWorkHours)) / 10;
            }

            // Fixed duration task but with dates specified so need to specify end dates
            else if (originalAddTaskComponent.TaskModel.HasFixedEndDate)
            {
                originalAddTaskComponent.TaskModel.EndDate = splitDate.Value.AddDays(-1);
                newAddTaskComponent.TaskModel.EndDate = originalEndDate;
            }

            // Fixed duration task with duration specified so need to specify days
            else
            {
                originalAddTaskComponent.TaskModel.DurationDays = (int)Math.Round(originalDuration * origProportion);
                newAddTaskComponent.TaskModel.DurationDays = originalDuration - originalAddTaskComponent.TaskModel.DurationDays;
            }
        }

        /// <summary>
        /// Add a status message for bad duration settings
        /// </summary>
        /// <param name="origDuration"></param>
        /// <param name="newDuration"></param>
        private void AddBadDurationStatusMessage(double origDuration, double newDuration)
        {
            statusMessages.Add(new StatusMessage($"The original and new task must both have a non-zero duration! Remember the dates are inclusive. " +
                        $"Based on your choice of split, the two tasks would have durations of {origDuration} days and {newDuration} days respectively.",
                        StatusMessage.MessageType.Error, () => true));
        }

        /// <summary>
        /// Updates the components with appropritae actuals
        /// </summary>
        private void UpdateActuals()
        {
            // Update the actuals
            var originalActuals = originalAddTaskComponent.TaskModel.ActualWorkHours;
            if (selectedActualsLogic == ActualsLogic.Overflow)
            {
                if (originalActuals > originalAddTaskComponent.TaskModel.PlannedWorkHours)
                {
                    // Must run after the sub task has been updated as it relies on the adjusted planned work
                    originalAddTaskComponent.TaskModel.ActualWorkHours = originalAddTaskComponent.TaskModel.PlannedWorkHours;
                    newAddTaskComponent.TaskModel.ActualWorkHours = originalActuals - originalAddTaskComponent.TaskModel.PlannedWorkHours;
                }
                else
                {
                    newAddTaskComponent.TaskModel.ActualWorkHours = 0;
                    originalAddTaskComponent.TaskModel.ActualWorkHours = originalActuals;
                }
            }
            else if (selectedActualsLogic == ActualsLogic.Divide)
            {
                originalAddTaskComponent.TaskModel.ActualWorkHours = Math.Round(10 * originalActuals * origProportion) / 10;
                newAddTaskComponent.TaskModel.ActualWorkHours = Math.Round(10 * (originalActuals - originalAddTaskComponent.TaskModel.ActualWorkHours)) / 10;
            }
            else if (selectedActualsLogic == ActualsLogic.Leave)
            {
                newAddTaskComponent.TaskModel.ActualWorkHours = 0;
                originalAddTaskComponent.TaskModel.ActualWorkHours = originalActuals;
            }
        }

        /// <summary>
        /// Calls the update subtasks methods on the components
        /// </summary>
        private async Task UpdateSubTasks()
        {
            disableButtons = true;
            StateHasChanged();
            await Task.Yield();

            // Call update subtasks on both panes to validate
            originalAddTaskComponent.UpdateSubTaskModelFromResourceDataGrid();
            newAddTaskComponent.UpdateSubTaskModelFromResourceDataGrid();

            disableButtons = false;
            StateHasChanged();
        }

        /// <summary>
        /// Discards changes and leaves the page
        /// </summary>
        private void DiscardChanges()
        {
            LogInformation($"Discarding splitting task {originalAddTaskComponent?.TaskModel.Name} on {originalAddTaskComponent.ProjectModel.GetSensibleObjectName()}!");
            Navigation.NavigateTo($"projects/projectdetails/{originalAddTaskComponent?.ProjectId}");
        }

        /// <summary>
        /// Validates the components and saves to DB
        /// </summary>
        private async Task UpdateAndSave()
        {
            showTaskInvalidError = false;

            // Validate the tasks first before trying to save anything as both have to pass
            await UpdateSubTasks();

            disableButtons = true;
            StateHasChanged();
            await Task.Yield();

            if (originalAddTaskComponent.IsValid && newAddTaskComponent.IsValid)
            {
                // Try to submit both tasks (sub tasks are updated as part of this submission attempt)
                originalAddTaskComponent.HandleSubmit();
                newAddTaskComponent.HandleSubmit();

                // Get updated project from the DB
                owningProject = ProjectService.GetById(Context, owningProject.ProjectId);
                Debug.WriteLine($"** {owningProject?.SubTasks.Count} subtasks found! IDs: {string.Join("|", owningProject?.SubTasks.Select(x => x.SubTaskId))}");

                // Update the project summary values
                var finrefs = FinancialReferenceService.GetAllOrDefault(Context);
                var bauTopSlicePercentage = GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
                owningProject.UpdateProjectMetaData(false, finrefs, bauTopSlicePercentage);

                // Update the project in the database
                LogInformation($"Saving project {owningProject.GetSensibleObjectName()}...");
                ProjectService.Update(Context, owningProject);

                // Navigate back
                Navigation.NavigateTo($"projects/projectdetails/{originalAddTaskComponent?.ProjectId}");
            }
            else
            {
                showTaskInvalidError = true;
            }

            disableButtons = false;
            StateHasChanged();
        }
    }
}
