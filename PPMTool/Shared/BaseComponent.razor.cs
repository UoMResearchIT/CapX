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

        protected PPMToolContext Context { get; set; }

        protected string ActiveUserName { get; private set; } = "None";

        public Person ActiveUser { get; private set; }

        protected RoleType ActiveUserRoleType { get; private set; }

        [Inject]
        protected UserService UserService { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

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
                    Context = ContextFactory.CreateDbContext();

                    // Stash the user name
                    ActiveUserName = authState?.User.Identity.Name.Trim().ToLower();

                    // Get the active user
                    var user = UserService.GetByUsername(Context, ActiveUserName);
                    ActiveUser = user?.Person;

                    // Get active user role
                    ActiveUserRoleType = user?.RoleType ?? RoleType.None;
                }
            }
        }
    }
}
