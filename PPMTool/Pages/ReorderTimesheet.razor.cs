using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    public partial class ReorderTimesheet : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        private Timesheet timesheet;
        private Role activeUserRole;
        private ObservableCollection<TimesheetTemplateItem> templateData;
        private IList<TimesheetTemplateItem> selectedTemplateItem;
        private TimesheetTemplateItem draggedItem;
        private string finalOrder;

        protected override async Task OnParametersSetAsync()
        {
            await base.OnParametersSetAsync();

            Loading = true;

            try
            {
                await Task.Run(() =>
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
                    Loading = false;
                });
            }
            catch (TaskCanceledException)
            {
                // We intend it to be cancelled so this is fine to ignore
            }
        }

        void RowRender(RowRenderEventArgs<TimesheetTemplateItem> args)
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
                    Summary = "Change saved",
                    Detail = "Your timesheet template ordering has been updated."
                });
            }));


        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
        }

        /// <summary>
        /// Should this user be allowed to view the timesheet. Only superusers, the owner or the line manager.
        /// </summary>
        /// <returns></returns>
        private bool CanEditTheTaskOrder()
        {
            return (timesheet?.IsOwner(ActiveUser) ?? false);
        }

        /// <summary>
        /// Navigate to timesheet
        /// </summary>
        /// <param name="timesheet"></param>
        public void GoToTimesheet(Timesheet timesheet)
        {
            Navigation.NavigateTo($"timesheets/addtimesheet/{timesheet.TimesheetId}");
        }

        /// <summary>
        /// Navigate back to the main timesheet dashboard
        /// </summary>
        /// <param name="timesheet"></param>
        public void SaveAndExit()
        {
            Navigation.NavigateTo($"timesheets");
        }
    }
}
