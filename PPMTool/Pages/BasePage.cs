using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize]
    public abstract class BasePage : ComponentBase
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
        protected RolesService RolesService { get; set; }

        [Inject]
        protected ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        [Inject]
        protected TooltipService TooltipService { get; set; }

        [Inject]
        protected NotificationService NotificationService { get; set; }

        [CascadingParameter]
        protected Task<AuthenticationState> AuthenticationStateTask { get; set; }

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

        protected AuthenticationState AuthenticationState { get; private set; }

        protected string ActiveUserName { get; private set; } = "None";

        protected PPMToolContext Context { get; set; }

        protected StatusMessage ErrorMessage { get; set; }

        protected string Title { get; set; }

        protected Person ActiveUser { get; private set; }

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

            // Create the context on every page
            Context = ContextFactory.CreateDbContext();

            // Get authentication state
            AuthenticationState = AuthenticationStateTask.Result;

            // Editing only permitted by managers and superusers
            EditAuthorised = (AuthenticationState?.User.IsInRole("Superuser") ?? false) || (AuthenticationState?.User.IsInRole("Manager") ?? false);

            // Stash the user name
            ActiveUserName = AuthenticationState?.User.Identity.Name.Trim().ToLower();

            // Get the active user
            ActiveUser = RolesService.GetByUsername(Context, ActiveUserName)?.Person;
        }

        /// <summary>
        /// Check whether the current user is the line manager of the person or a superuser
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        protected bool IsSuperuserOrLineManagerOfThisPerson(Person person)
        {
            var lm = (person?.LineManager.PersonId ?? 0) == (ActiveUser?.PersonId ?? -1);
            var su = AuthenticationState?.User.IsInRole("Superuser") ?? false;
            return lm || su;
        }

        public void LogInformation(string message)
        {
            Logger?.LogInformation($"{ActiveUserName}: {message}");
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning($"{ActiveUserName}: {message}");
        }

        public void LogError(string message)
        {
            Logger?.LogError(message);
        }

        public void LogError(string message, Exception exception)
        {
            Logger?.LogError(exception, message);
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
