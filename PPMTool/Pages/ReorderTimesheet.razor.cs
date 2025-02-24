using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ReorderTimesheet : DataGridPage<TimesheetTemplateItem>
    {
        [Parameter]
        public int TimesheetId { get; set; }

        [Parameter]
        public Action FormClosed { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        private ObservableCollection<TimesheetTemplateItem> templateData;
        private IList<TimesheetTemplateItem> selectedTemplateItem;
        private TimesheetTemplateItem draggedItem;
        private string finalOrder;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Loading = true;
            StateHasChanged();
            EnqueueLoadData(GetTask);
        }

        /// <summary>
        /// Getter for background load task
        /// </summary>
        /// <returns></returns>
        private Task GetTask()
        {
            return Task.Run(() =>
            {
                // Handle if the user is not found
                if (ActiveUser == null)
                {
                    LogError($"No person found for {ActiveUserName}!");
                    return;
                }

                // Get the user's timesheet template details to work with
                templateData = new ObservableCollection<TimesheetTemplateItem>();

                if (ActiveUser.TimesheetTemplateData != null)
                {
                    finalOrder = ActiveUser.TimesheetTemplateData;
                    var split = finalOrder.Split("|");
                    IEnumerable<InnateCodeTask> tasks = InnateCodeService.GetAllTasks(Context);

                    foreach (string id in split)
                    {
                        InnateCodeTask task = tasks.First(x => x.InnateCodeTaskId == int.Parse(id));
                        if (task != null)
                        {
                            TimesheetTemplateItem item = new TimesheetTemplateItem();
                            item.TimesheetTemplateItemId = task.InnateCodeTaskId;
                            item.InnateCode = task.InnateCode;
                            item.InnateCodeTask = task;

                            templateData.Add(item);
                        }
                    }
                }

                selectedTemplateItem = new List<TimesheetTemplateItem>() { templateData.FirstOrDefault() };

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Method to close the dialog popup. Uses a callback to reload the calling page.
        /// </summary>
        /// <param name="status"></param>
        protected virtual void CloseForm(bool status)
        {
            DialogService.Close(status);
            FormClosed?.Invoke();
        }

        /// <summary>
        /// Drag-and-drop functionality in the RadzenDataGrid component.
        /// Details at https://blazor.radzen.com/datagrid-rowreorder?theme=material3
        /// </summary>
        /// <param name="args"></param>
        public void RowRender(RowRenderEventArgs<TimesheetTemplateItem> args)
        {
            args.Attributes.Add("title", "Drag row to reorder");
            args.Attributes.Add("style", "cursor:grab");
            args.Attributes.Add("draggable", "true");
            args.Attributes.Add("ondragover", "event.preventDefault();event.target.closest('.rz-data-row').classList.add('my-class')");
            args.Attributes.Add("ondragleave", "event.target.closest('.rz-data-row').classList.remove('my-class')");
            args.Attributes.Add("ondragstart", EventCallback.Factory.Create<DragEventArgs>(this, () => draggedItem = args.Data));
            args.Attributes.Add("ondrop", EventCallback.Factory.Create<DragEventArgs>(this, () =>
            {
                var draggedIndex = templateData.IndexOf(draggedItem);
                var droppedIndex = templateData.IndexOf(args.Data);
                templateData.Remove(draggedItem);
                templateData.Insert(draggedIndex <= droppedIndex ? droppedIndex++ : droppedIndex, draggedItem);

                // Update the order to store back into the db for the user
                finalOrder = "";
                foreach (var item in templateData)
                {
                    finalOrder += finalOrder.Length > 0 ? $"|{item.InnateCodeTask.InnateCodeTaskId}" : $"{item.InnateCodeTask.InnateCodeTaskId}";
                }

                // Save the result and update the user
                ActiveUser.TimesheetTemplateData = finalOrder;
                PersonService.Update(Context, ActiveUser);

                // Show notification for save action
                ShowNotification(new CapXNotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Change Saved",
                    Detail = "Your timesheet template ordering has been updated."
                });
            }));
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>
        /// Navigate to timesheet
        /// </summary>
        /// <param name="timesheet"></param>
        public void GoToTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"timesheets/addtimesheet/{TimesheetId}");
        }
    }
}
