using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;

namespace PPMTool.Shared
{
    public partial class BaseComponent : ComponentBase
    {
        [CascadingParameter]
        protected Task<AuthenticationState> AuthenticationStateTask { get; set; }

        protected AuthenticationState AuthenticationState { get; private set; }

        protected PPMToolContext Context { get; set; }

        protected string ActiveUserName { get; private set; } = "None";

        public Person ActiveUser { get; private set; }

        protected RoleType ActiveUserRoleType { get; private set; }

        [Inject]
        protected RolesService RolesService { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (AuthenticationStateTask is not null)
            {
                var authState = AuthenticationStateTask.GetAwaiter().GetResult();
                var user = authState?.User;

                if (user?.Identity is not null && user.Identity.IsAuthenticated)
                {
                    // Create the context on every page
                    Context = ContextFactory.CreateDbContext();

                    // Get authentication state
                    AuthenticationState = authState;

                    // Stash the user name
                    ActiveUserName = AuthenticationState?.User.Identity.Name.Trim().ToLower();

                    // Get the active user
                    var role = RolesService.GetByUsername(Context, ActiveUserName);
                    ActiveUser = role?.Person;

                    // Get active user role
                    ActiveUserRoleType = role?.RoleType ?? RoleType.None;
                }
            }
        }
    }
}
