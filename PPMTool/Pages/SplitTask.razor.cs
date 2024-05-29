using System;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public partial class SplitTask : BasePage
    {
        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private ProjectService ProjectService { get; set; }

        [Parameter]
        public int? SubTaskId { get; set; }

        [Parameter]
        public int? ProjectId { get; set; }

        private AddTask originalAddTaskComponent;
        private AddTask newAddTaskComponent;
        private bool splitOnDate;
        private ActualsLogic selectedActualsLogic;
        private DateTime? splitDate;
        private double? splitValue;
        private StatusMessage errorMessage;
        double origProportion = 0;
        double newProportion = 0;

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
                SelectedSplitLogicChanged();
            }
        }

        private void SelectedSplitLogicChanged()
        {
            RestoreModels();
            ApplySplitLogic();
            ApplyActualsLogic();
        }

        private void SelectedSplitDateChanged(DateTime? value)
        {
            SelectedSplitLogicChanged();
        }

        private void ApplySplitLogic()
        {
            // Store some original values before modification
            var originalEndDate = originalAddTaskComponent.TaskModel.EndDate;
            var originalWork = originalAddTaskComponent.TaskModel.PlannedWorkHours;
            var originalDuration = originalAddTaskComponent.TaskModel.DurationDays;
            double proposedDurationOrigTask = 0;
            double proposedDurationNewTask = 0;
            var durationUnaltered = (originalAddTaskComponent.TaskModel.EndDate - originalAddTaskComponent.TaskModel.StartDate).TotalDays + 1;

            // Configure the split date adn value as required and validate choices
            if (splitOnDate)
            {
                // Check we have a split date specified
                if (splitDate == null)
                {
                    errorMessage = new StatusMessage("Please select a date to split the task on!", StatusMessage.MessageType.Error);
                    return;
                }

                // Split date must be the day after the start of the original task for it to have a non-zero duration
                // Split date must be at least the same day as the end of the new task for it to have a non-zero duration
                proposedDurationOrigTask = (splitDate - originalAddTaskComponent.TaskModel.StartDate).Value.TotalDays;
                proposedDurationNewTask = (newAddTaskComponent.TaskModel.EndDate - splitDate).Value.TotalDays + 1;
                if (proposedDurationOrigTask < 1 || proposedDurationNewTask < 1)
                {
                    errorMessage = new StatusMessage("The original and new task must both have a non-zero duration! Remember the dates are inclusive.", StatusMessage.MessageType.Error);
                    return;
                }
            }
            else
            {
                // Check we have a split value
                if (splitValue == null)
                {
                    errorMessage = new StatusMessage("Please specify the number of days to split the task by!", StatusMessage.MessageType.Error);
                    return;
                }

                // Duration of both tasks in days must be greater than zero
                proposedDurationOrigTask = splitValue ?? 0;
                proposedDurationNewTask = originalAddTaskComponent.TaskModel.DurationDays - splitValue ?? 0;
                if (proposedDurationOrigTask < 1 || proposedDurationNewTask < 1)
                {
                    errorMessage = new StatusMessage("The original and new task must both have a non-zero duration!", StatusMessage.MessageType.Error);
                    return;
                }

                // Set split date
                splitDate = originalAddTaskComponent.TaskModel.StartDate.AddDays(proposedDurationOrigTask - 1);

            }

            // Get proportion of split based on duration
            origProportion = proposedDurationOrigTask / durationUnaltered;
            newProportion = proposedDurationNewTask / durationUnaltered;

            // Adjust the start and end dates of the tasks
            newAddTaskComponent.TaskModel.StartDate = splitDate ?? DateTime.Today;
            originalAddTaskComponent.TaskModel.EndDate = splitDate.Value.AddDays(-1);

            // Reset new task settings to match original
            newAddTaskComponent.TaskModel.TaskType = originalAddTaskComponent.TaskModel.TaskType;
            newAddTaskComponent.TaskModel.HasFixedEndDate = originalAddTaskComponent.TaskModel.HasFixedEndDate;

            // Divide up the work if fixed work based on proportion
            if (originalAddTaskComponent.TaskModel.TaskType == TaskType.FixedWork)
            {
                originalAddTaskComponent.TaskModel.PlannedWorkHours = Math.Round(10 * originalWork * origProportion) / 10;
                newAddTaskComponent.TaskModel.PlannedWorkHours = originalWork - originalAddTaskComponent.TaskModel.PlannedWorkHours;
            }

            // Specify duration if fixed duration
            else if (!originalAddTaskComponent.TaskModel.HasFixedEndDate)
            {
                originalAddTaskComponent.TaskModel.DurationDays = (int)Math.Round(originalDuration * origProportion);
                newAddTaskComponent.TaskModel.DurationDays = originalDuration - originalAddTaskComponent.TaskModel.DurationDays;
            }
        }

        private void ApplyActualsLogic()
        {
            var originalActuals = originalAddTaskComponent.TaskModel.ActualWorkHours;
            if (selectedActualsLogic == ActualsLogic.Overflow)
            {
                if (originalActuals > originalAddTaskComponent.TaskModel.PlannedWorkHours)
                {
                    newAddTaskComponent.TaskModel.ActualWorkHours = originalActuals - originalAddTaskComponent.TaskModel.PlannedWorkHours;
                    originalAddTaskComponent.TaskModel.ActualWorkHours = originalAddTaskComponent.TaskModel.PlannedWorkHours;
                }
                else
                {
                    newAddTaskComponent.TaskModel.ActualWorkHours = 0;
                    originalAddTaskComponent.TaskModel.ActualWorkHours = originalActuals;
                }
            }
            else if (selectedActualsLogic == ActualsLogic.Divide)
            {
                newAddTaskComponent.TaskModel.ActualWorkHours = Math.Round(10 * originalActuals * origProportion) / 10;
                originalAddTaskComponent.TaskModel.ActualWorkHours = originalActuals - newAddTaskComponent.TaskModel.ActualWorkHours;
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
            originalAddTaskComponent.UpdateSubTask();
            newAddTaskComponent.UpdateSubTask();
        }

        private void RestoreModels()
        {
            originalAddTaskComponent.InitialiseTaskModel();
            newAddTaskComponent.InitialiseTaskModel();
            errorMessage = null;
            StateHasChanged();
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding splitting task {originalAddTaskComponent?.TaskModel.Name} on {originalAddTaskComponent.ProjectModel.GetFullName()}!");
            Navigation.NavigateTo($"projectdetails/{originalAddTaskComponent?.ProjectId}");
        }

        private void UpdateAndSave()
        {
            // TODO: Run the edit context validation on both panes and if both are valid, save the changes and navigate away

            UpdateSubTasks();

            Navigation.NavigateTo($"projectdetails/{originalAddTaskComponent?.ProjectId}");
        }
    }
}
