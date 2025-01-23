using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Shared;
using Radzen;
using Sentry;

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
        protected ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        private bool loading;
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

        protected bool EditAuthorised { get; set; }

        protected StatusMessage ErrorMessage { get; set; }

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

            // Editing only permitted by managers and superusers by default
            EditAuthorised = ActiveUserRoleType == RoleType.Manager || ActiveUserRoleType == RoleType.Superuser;
        }

        /// <summary>
        /// Check whether the current user is the line manager of the person or a superuser
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        protected bool IsSuperuserOrLineManagerOfThisPerson(Person person)
        {
            var lm = (person?.LineManager.PersonId ?? 0) == (ActiveUser?.PersonId ?? -1);
            var su = ActiveUserRoleType == RoleType.Superuser;
            return lm || su;
        }

        /// <summary>
        /// Logs the error to the Sentry platform
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sentryLevel"></param>
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

        public void ShowTooltip(ElementReference elementReference, string message, int delay = 500)
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
