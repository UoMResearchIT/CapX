using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PPMTool.Data.Context;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public abstract class BasePage : ComponentBase
    {
        [Inject]
        private ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

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

        protected string ActiveUser { get; private set; } = "None";

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
            ActiveUser = AuthenticationState?.User.Identity.Name.Trim().ToLower();
        }

        public void LogInformation(string message)
        {
            Logger?.LogInformation($"{ActiveUser}: {message}");
        }

        public void LogWarning(string message)
        {
            Logger.LogWarning($"{ActiveUser}: {message}");
        }

        public void LogError(string message)
        {
            Logger?.LogError(message);
        }

        public void LogError(string message, Exception exception)
        {
            Logger?.LogError(exception, message);
        }
    }
}
