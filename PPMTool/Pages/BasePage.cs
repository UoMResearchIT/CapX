// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Blazored.SessionStorage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using PPMTool.Shared;
using Radzen;
using static PPMTool.Shared.MainLayout;

namespace PPMTool.Pages
{
    [Authorize]
    public abstract class BasePage : BaseComponent
    {
        /// <summary>
        /// Default notification message with format and duration pre-set
        /// </summary>
        public class CapXNotificationMessage : NotificationMessage
        {
            public CapXNotificationMessage()
            {
                Style = "position: fixed; top: 100%; left: 50%; transform: translate(-50%, -120%); width: 100%";
                Duration = 4000;
                Severity = NotificationSeverity.Error;
            }
        }

        [Inject]
        protected ISessionStorageService SessionStorage { get; set; }

        [Inject]
        protected ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Inject]
        private TooltipService TooltipService { get; set; }

        [Inject]
        private NotificationService NotificationService { get; set; }

        [Inject]
        protected FeatureService FeatureService { get; set; }

        // Datagrid variables for results paging
        public int PageCount;
        public readonly int[] PageSizeOptions = new[] { 5, 10, 25, 50, 100 };

        private bool loading = true;
        [CascadingParameter]
        public bool Loading
        {
            get => loading;
            set
            {
                if (loading != value)
                {
                    loading = value;
                }
            }
        }

        public async Task OnPageSizeChanged(int value)
        {
            await SessionStorage.SetItemAsync("PageCount", value);
        }


        [CascadingParameter]
        public MainLayout Layout { get; set; }

        protected bool EditAuthorised { get; set; }

        protected StatusMessage ErrorMessage { get; private set; }

        /// <summary>
        /// A queuing mechanism for background data loads on pages so they don't run at the same time
        /// </summary>
        protected TaskQueue TaskQueue { get; private set; } = new TaskQueue();

        /// <summary>
        /// Put a load data request into the queue
        /// </summary>
        /// <param name="taskGenerator">Function to generate a Task to put in the queue</param>
        protected void EnqueueLoadData(Func<Task> taskGenerator)
        {
            _ = TaskQueue.Enqueue(taskGenerator);
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();
            GetPageCountSettingAsync();
            Layout?.Reset();

            // Not sure why this happens but worth noting
            if (Layout == null)
            {
                LogWarning("Layout is null!");
            }

            // Editing only permitted by managers and superusers by default
            EditAuthorised = ActiveUserRoleType == RoleType.Manager || ActiveUserRoleType == RoleType.Superuser;
            
        }

        private async Task GetPageCountSettingAsync()
        {
            PageCount = await SessionStorage.GetItemAsync<int?>("PageCount") ?? 15;
        }

        /// <summary>
        /// Sets the page error message -- if using action bar will set it there instead of using the page property
        /// </summary>
        /// <param name="message"></param>
        protected void SetErrorMessage(StatusMessage message)
        {
            if (Layout?.ShowActionBar ?? false)
            {
                Layout?.SetErrorMessage(message);
            }
            else
            {
                ErrorMessage = message;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Clear the error messsage -- if using the action bar will clear it there instead of using the page property
        /// </summary>
        protected void ClearErrorMessage()
        {
            if (Layout?.ShowActionBar ?? false)
            {
                Layout?.ClearErrorMessage();
            }
            else
            {
                ErrorMessage = null;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Setup a default save / discard action bar
        /// </summary>
        /// <param name="submit"></param>
        /// <param name="discard"></param>
        protected void SetDefaultActionBar(Action submit, Action discard)
        {
            Layout?.SetButtons(
            [
                new ActionButton
                {
                    OnClick = submit,
                    Disabled = !EditAuthorised
                },
                new ActionButton
                {
                    Icon = "close",
                    Text = "Discard",
                    ButtonStyle = ButtonStyle.Danger,
                    OnClick = discard
                }
            ]);
        }

        /// <summary>
        /// Check whether the current user is the line manager of the person or a superuser
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        protected bool IsSuperuserOrLineManagerOfThisPerson(Person person)
        {
            var lm = (person?.LineManager?.PersonId ?? 0) == (ActiveUser?.Person?.PersonId ?? -1);
            var su = ActiveUserRoleType == RoleType.Superuser;
            return lm || su;
        }

        /// <summary>
        /// Whether the active user is the person about which a page is concerned, the LM of the person or a superuser
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        protected bool IsSuperuserOrLineManagerOrPerson(Person person)
        {
            var p = (person?.PersonId ?? 0) == (ActiveUser?.Person?.PersonId ?? -1);
            var sulm = IsSuperuserOrLineManagerOfThisPerson(person);
            return p || sulm;
        }

        /// <summary>
        /// Logs the error to the Sentry platform
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sentryLevel"></param>
        /// <param name="exception"></param>
        private void LogToSentry(string message, SentryLevel sentryLevel = SentryLevel.Info, Exception exception = null)
        {
            if (exception != null)
            {
                SentrySdk.CaptureException(exception);
            }
            else
            {
                SentrySdk.CaptureMessage(message, sentryLevel);
            }
        }

        /// <summary>
        /// Log information to the logging sinks
        /// </summary>
        /// <param name="message"></param>
        public void LogInformation(string message)
        {
            Logger?.LogInformation($"{ActiveUserName}: {message}");
        }

        /// <summary>
        /// Log the warning to the logging sinks
        /// </summary>
        /// <param name="message"></param>
        public void LogWarning(string message)
        {
            Logger.LogWarning($"{ActiveUserName}: {message}");
            //LogToSentry(message, SentryLevel.Warning);
        }

        /// <summary>
        /// Log the error to the logging sinks
        /// </summary>
        /// <param name="message"></param>
        public void LogError(string message)
        {
            Logger?.LogError(message);
            //LogToSentry(message, SentryLevel.Error);
        }

        /// <summary>
        /// Log the error to the logging sinks
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        public void LogError(string message, Exception exception)
        {
            Logger?.LogError(exception, message);
            //LogToSentry(message, SentryLevel.Error, exception);
        }

        public void ShowTooltip(ElementReference elementReference, string message, int delay = 250)
        {
            var options = new TooltipOptions()
            {
                Delay = delay
            };
            TooltipService.Open(elementReference, message, options);
        }

        protected void ShowNotification(CapXNotificationMessage message)
        {
            NotificationService.Notify(message);
        }
    }
}
