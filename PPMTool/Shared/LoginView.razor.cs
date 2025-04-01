using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Shared
{
    public partial class LoginView : BaseComponent, IDisposable
    {
        [Inject]
        private NavigationManager Navigation { get; set; }

        private string displayName;
        private IEnumerable<Role> roles;
        private IEnumerable<string> filteredRoles;
        private bool showDropDown = false;
        private string selectedRole;
        private Role loginAs;
        private string loginLink;
        private bool redirectRequired = false;

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
                    roles = RolesService.GetAll(ContextFactory.CreateDbContext()).OrderByDescending(x => x.RoleType).ThenBy(x => x.Name);
                    filteredRoles = roles.Select(x => RoleToString(x));
                    selectedRole = filteredRoles.FirstOrDefault();
                    OnChange();

                    redirectRequired = true;
                }
            }

            displayName = ActiveUser?.Name;
        }

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (firstRender && redirectRequired)
            {
#if !LOCAL
                Debug.WriteLine("** Sending user to login!");
                Navigation.NavigateTo(loginLink, true);
#endif
            }
        }

        /// <summary>
        /// Fired when a person is pick from the dropdown
        /// </summary>
        private void OnChange()
        {
            loginAs = roles.FirstOrDefault(x => RoleToString(x) == selectedRole);
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
            Debug.WriteLine($"** Login using {loginLink}");
        }

        /// <summary>
        /// Convert a role to a string in the dropdown
        /// </summary>
        /// <param name="role"></param>
        /// <returns></returns>
        private string RoleToString(Role role)
        {
            return $"[{role.RoleType.ToString().ToUpper()}] {role.Name} ({role.CASUserName})";
        }

        public void Dispose()
        {
            // Unsubscribe
            Navigation.LocationChanged -= HandleLocationChanged;
        }

        private void HandleLocationChanged(object sender, LocationChangedEventArgs e)
        {
            // Force a rerender of the login view
            StateHasChanged();
        }
    }
}
