using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize]
    public abstract class BasePage : ComponentBase
    {
        [Inject]
        private ILogger Logger { get; set; }

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
                    Debug.WriteLine($"** Loading: {loading}");
                }
            }
        }

        protected bool EditAuthorised { get; set; }

        protected AuthenticationState AuthenticationState { get; private set; }

        protected string ActiveUserName { get; private set; } = "None";

        protected PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Create the context on every page
            context = ContextFactory.CreateDbContext();

            // Get authentication state
            AuthenticationState = AuthenticationStateTask.Result;

            // Editing only permitted by managers and superusers
            EditAuthorised = (AuthenticationState?.User.IsInRole("Superuser") ?? false) || (AuthenticationState?.User.IsInRole("Manager") ?? false);

            // Stash the user name
            ActiveUserName = AuthenticationState?.User.Identity.Name.Trim().ToLower();
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

        protected void ShowNotification(NotificationMessage message)
        {
            NotificationService.Notify(message);
        }
    }
}
