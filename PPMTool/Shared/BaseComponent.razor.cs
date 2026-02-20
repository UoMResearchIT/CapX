// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Shared
{
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
                    User user = UserService.GetByUsernameOrEmail(Context, claimsPrincipal.Identity.Name?.Trim().ToLower());

                    // Get the active user
                    ActiveUser = user;

                    // Stash the user name
                    ActiveUserName = user?.CASUserName;

                    // Get active user role
                    ActiveUserRoleType = user?.RoleType ?? RoleType.None;
                }
            }
        }
    }
}
