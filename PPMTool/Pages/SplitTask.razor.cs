using System;
using Microsoft.AspNetCore.Components;
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
        private int selectedSplitLogic = 0;
        private int selectedActualsLogic = 0;
        private DateTime? splitDate;
        private double? splitValue;

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
            }
        }

        private void SelectedSplitLogicChanged(int value)
        {
            // TODO: Respond to the selected split logic changing by updating the relevant parts of the tasks
        }

        private void SelectedActualsLogicChanged(int value)
        {
            // TODO: Respond to the selected split logic changing by updating the relevant parts of the tasks
        }

        private void UpdateSubTasks()
        {
            // TODO: Call update subtasks on both panes to validate
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
