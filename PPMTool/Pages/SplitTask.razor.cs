using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser,Developer")]
    public partial class SplitTask : BasePage
    {
        [Parameter]
        public int? SubTaskId { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        [Inject]
        public RolesService RolesService { get; set; }

        private AddTask originalAddTaskComponent;
        private AddTask newAddTaskComponent;
        private bool splitOnDate = true;
        private ActualsLogic selectedActualsLogic;
        private DateTime? splitDate;
        private double? splitValue;
        private List<StatusMessage> statusMessages = new List<StatusMessage>();
        private double origProportion = 0;
        private DateTime originalStartDate;
        private DateTime originalEndDate;

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);
            if (firstRender)
            {
                LogInformation($"Splitting task {originalAddTaskComponent?.TaskModel.Name} on {originalAddTaskComponent?.ProjectModel.GetFullName()}");
                originalStartDate = originalAddTaskComponent?.TaskModel.StartDate ?? DateTime.Today;
                originalEndDate = originalAddTaskComponent?.TaskModel.EndDate ?? DateTime.Today;

                // Only allow the project manager to save the split or a superuser
                var user = AuthenticationState?.User;
                var role = RolesService.GetByUsername(context, ActiveUserName);
                EditAuthorised = (user?.IsInRole("Superuser") ?? false) || ((user?.IsInRole("Manager") ?? false) && originalAddTaskComponent?.ProjectModel.ProjectManager.PersonId == role?.Person.PersonId);

                StateHasChanged();
            }
        }

        private void SplitTasks()
        {
            Debug.WriteLine($"** Running split logic...");

            // Clear the error messages
            statusMessages.Clear();

            // Reinitialise the components from the DB
            originalAddTaskComponent.InitialiseComponent();
            newAddTaskComponent.InitialiseComponent();

            // Apply the logic to split the task and actuals
            ApplySplitLogic();

            // Update and schedule the sub tasks
            UpdateSubTasks();

            // Update the actuals
            UpdateActuals();

            // Call update subtasks again to update the actuals cost
            UpdateSubTasks();

            // Check for fixed work warnings
            CheckForFixedWorkWarnings();

            // Tell Blazor to redraw the page
            StateHasChanged();

            Debug.WriteLine($"** {statusMessages.Count} status message(s).");
        }

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
                    statusMessages.Add(new StatusMessage($"The original and new task must both have a non-zero duration! Remember the dates are inclusive. " +
                        $"Based on your choice of split, the two tasks would have durations of {proposedDurationOrigTask} days and {proposedDurationNewTask} days respectively.",
                        StatusMessage.MessageType.Error, () => true));
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
                    statusMessages.Add(new StatusMessage($"The original and new task must both have a non-zero duration! " +
                        $"Based on your choice of split, the two tasks would have durations of {proposedDurationOrigTask} days and {proposedDurationNewTask} days respectively.",
                        StatusMessage.MessageType.Error, () => true));
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

        private void UpdateSubTasks()
        {
            // Call update subtasks on both panes to validate
            originalAddTaskComponent.UpdateSubTaskModelFromResourceDataGrid();
            newAddTaskComponent.UpdateSubTaskModelFromResourceDataGrid();
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding splitting task {originalAddTaskComponent?.TaskModel.Name} on {originalAddTaskComponent.ProjectModel.GetFullName()}!");
            Navigation.NavigateTo($"projectdetails/{originalAddTaskComponent?.ProjectId}");
        }

        private void UpdateAndSave()
        {
            // Try to submit both tasks (sub tasks are updated as part of this submission attempt)
            originalAddTaskComponent.HandleSubmit();
            newAddTaskComponent.HandleSubmit();

            // Navigate away if successful submit
            if (originalAddTaskComponent.IsValid && newAddTaskComponent.IsValid)
            {
                Navigation.NavigateTo($"projectdetails/{originalAddTaskComponent?.ProjectId}");
            }
            else
            {
                statusMessages.Add(new StatusMessage("Please correct the errors on tasks before saving!", StatusMessage.MessageType.Error, () => true));
            }
        }
    }
}
