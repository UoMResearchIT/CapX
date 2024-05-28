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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            LogInformation($"Splitting task {originalAddTaskComponent?.TaskModel.Name} on {originalAddTaskComponent?.ProjectModel.GetFullName()}");
        }

        private void UpdateSubTasks()
        {
            // TODO: Use the config logic to update the two subtasks -- use the SubTaskService to restore the original model

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
        }
    }
}
