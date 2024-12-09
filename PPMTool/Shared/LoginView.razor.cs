using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Shared
{
    public partial class LoginView : ComponentBase, IDisposable
    {
        [Inject]
        private NavigationManager Navigation { get; set; }

        [Inject]
        private RolesService RoleService { get; set; }

        [Inject]
        private IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        [CascadingParameter]
        private Task<AuthenticationState> AuthenticationState { get; set; }

        private string displayName;
        private IEnumerable<Role> roles;
        private IEnumerable<string> filteredRoles;
        private bool showDropDown = false;
        private string selectedRole;
        private Role loginAs;
        private string loginLink;

        protected override void OnInitialized()
        {
            // Subscribe to the navigation manager's location changed event to force a rerender of the login view
            Navigation.LocationChanged += HandleLocationChanged;

            // Show dropdown
#if LOCAL
            showDropDown = true;
#endif

            if (AuthenticationState is not null)
            {
                var authState = AuthenticationState.GetAwaiter().GetResult();
                var user = authState?.User;

                if (user?.Identity is not null && user.Identity.IsAuthenticated)
                {
                    // Lookup the person
                    var roles = RoleService.GetAll(ContextFactory.CreateDbContext());
                    var role = roles.FirstOrDefault(x => x.GetStandardisedUserName() == user.Identity.Name.Trim().ToLower());
                    displayName = role?.GetName() ?? user.Identity.Name;
                }
                else
                {
                    roles = RoleService.GetAll(ContextFactory.CreateDbContext()).OrderByDescending(x => x.RoleType).ThenBy(x => x.Name);
                    filteredRoles = roles.Select(x => RoleToString(x));
                    selectedRole = filteredRoles.FirstOrDefault();
                    OnChange();
                }
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
            loginLink += $"&role={loginAs.CASUserName}";
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
