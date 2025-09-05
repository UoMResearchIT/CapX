using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Shared
{
    public partial class LoginView : BaseComponent, IDisposable
    {
        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private ILogger Logger { get; set; }

        private string displayName;
        private IEnumerable<User> users;
        private IEnumerable<string> filteredUsers;
        private bool showDropDown = false;
        private string selectedUser;
        private User loginAs;
        private string loginLink = string.Empty;
        private bool notLoggedIn = false;
        private PPMToolContext context;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Subscribe to the navigation manager's location changed event to force a rerender of the login view
            Navigation.LocationChanged += HandleLocationChanged;

            // Show dropdown
#if LOCAL
            showDropDown = true;
#endif
            if (AuthenticationStateTask is not null)
            {
                var authState = AuthenticationStateTask.GetAwaiter().GetResult();
                var user = authState?.User;

                if (user?.Identity is null || !user.Identity.IsAuthenticated)
                {
                    if (context == null)
                    {
                        context = ContextFactory.CreateDbContext();
                    }
                    users = UserService.GetAll(context).OrderByDescending(x => x.RoleType).ThenBy(x => x.Name);
                    filteredUsers = users.Select(x => UserToString(x));
                    selectedUser = filteredUsers.FirstOrDefault();
                    OnChange();
                    notLoggedIn = true;
                }
            }

            displayName = ActiveUser?.Name;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender)
            {

                int count = Regex.Matches(loginLink, "returnUrl").Count;
                if (notLoggedIn && count == 1)
                {
#if !LOCAL
                    Logger.LogInformation($"User not logged in -- auto-redirecting to {loginLink}...");
                    Navigation.NavigateTo(loginLink, true);
#endif
                }
            }
        }

        /// <summary>
        /// Fired when a person is pick from the dropdown
        /// </summary>
        private void OnChange()
        {
            loginAs = users.FirstOrDefault(x => UserToString(x) == selectedUser);
            SetLoginLink();
        }

        /// <summary>
        /// Method to set the login link
        /// </summary>
        private void SetLoginLink()
        {

            loginLink = $"/Account/Login?returnUrl={Navigation?.Uri}";
#if LOCAL
            loginLink += $"&username={loginAs.CASUserName}";
#endif
            Debug.WriteLine($"** Setting login link as {loginLink}");
        }

        /// <summary>
        /// Convert a user to a string in the dropdown
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        private string UserToString(User user)
        {
            return $"[{user.RoleType.ToString().ToUpper()}] {user.Name} ({user.CASUserName})";
        }

        public void Dispose()
        {
            // Unsubscribe
            Navigation.LocationChanged -= HandleLocationChanged;

            // Dispose the context
            context?.Dispose();
        }

        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Force a rerender of the login view
            StateHasChanged();
        }
    }
}
