using System;
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

        protected bool EditAuthorised { get; set; }

        protected AuthenticationState AuthenticationState { get; private set; }

        protected bool loading;

        private string activeUser = "None";

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
            activeUser = AuthenticationState?.User.Identity.Name.Trim().ToLower();
        }

        protected void LogInformation(string message)
        {
            Logger?.LogInformation($"{activeUser}: {message}");
        }

        protected void LogWarning(string message)
        {
            Logger.LogWarning($"{activeUser}: {message}");
        }

        protected void LogError(string message)
        {
            Logger?.LogError(message);
        }

        protected void LogError(string message, Exception exception)
        {
            Logger?.LogError(exception, message);
        }
    }
}
