using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public abstract class BasePage : ComponentBase
    {
        [Inject]
        private ILogger Logger { get; set; }

        [Inject]
        protected NavigationManager Navigation { get; set; }

        [CascadingParameter]
        protected Task<AuthenticationState> authenticationStateTask { get; set; }

        protected bool EditAuthorised { get; set; }

        protected AuthenticationState AuthenticationState { get; private set; }

        protected bool loading;

        private string activeUser = "None";

        protected override void OnInitialized()
        {
            base.OnInitialized();

            AuthenticationState = authenticationStateTask.Result;

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
