// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Shared
{
    /// <summary>
    /// Provides a base class for Blazor components that require access to user authentication state, user information,
    /// and application services.
    /// </summary>
    /// <remarks>BaseComponent centralises common functionality for components, including retrieving the
    /// current authenticated user, their role, and application settings. It also provides access to the database
    /// context and various injected services. Components that inherit from BaseComponent can use these members to
    /// simplify user and context management.</remarks>
    public partial class BaseComponent : ComponentBase
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected PPMToolContext Context { get; set; }

        protected string ActiveUserName { get; private set; } = "None";

        public User ActiveUser { get; private set; }

        protected RoleType ActiveUserRoleType { get; private set; }

        [Inject]
        protected UserService UserService { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        [Inject]
        protected ThemeService ThemeService { get; set; }

        [Inject]
        protected SettingsService SettingsService { get; set; }

        /// <summary>
        /// Initializes the component and sets up the user context based on the current authentication state.
        /// </summary>
        /// <remarks>This method retrieves the authenticated user's information and role, and initialises
        /// the database context if authentication is successful. It should be called as part of the component's
        /// initialisation lifecycle. If the user is not authenticated, user-related properties remain unset.</remarks>
        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (AuthenticationStateTask is not null)
            {
                var authState = AuthenticationStateTask.GetAwaiter().GetResult();
                var claimsPrincipal = authState?.User;

                if (claimsPrincipal?.Identity is not null && claimsPrincipal.Identity.IsAuthenticated)
                {
                    // Create the context on every page
                    if (Context == null) Context = ContextFactory.CreateDbContext();

                    // Get the user object
                    User user = UserService.GetByUsernameOrEmail(Context, claimsPrincipal.Identity.Name?.Clean());

                    // Get the active user
                    ActiveUser = user;

                    // Stash the user name
                    ActiveUserName = user?.CASUserName;

                    // Get active user role
                    ActiveUserRoleType = user?.RoleType ?? RoleType.None;
                }
            }
        }

        /// <summary>
        /// Base method to make it easy to get a setting from any component in the UI
        /// </summary>
        /// <param name="setting"></param>
        /// <returns></returns>
        protected string GetSetting(SettingType setting)
        {
            // Just pass through to the service
            return SettingsService.GetSetting(setting);
        }

        /// <summary>
        /// Retrieves the value of the specified setting, converted to the specified type. Returns a default value if
        /// the setting is not found or cannot be converted.
        /// </summary>
        /// <typeparam name="T">The type to which the setting value is converted and returned.</typeparam>
        /// <param name="setting">The setting to retrieve.</param>
        /// <param name="defaultValue">The value to return if the setting is not found or cannot be converted to the specified type.</param>
        /// <returns>The value of the specified setting converted to type T, or the provided default value if the setting is not
        /// found or conversion fails.</returns>
        protected T GetSetting<T>(SettingType setting, T defaultValue)
        {
            return SettingsService.GetSetting<T>(setting, defaultValue);
        }
    }
}
