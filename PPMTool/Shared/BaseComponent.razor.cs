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
        protected RolesService RolesService { get; set; }

        [Inject]
        protected IDbContextFactory<PPMToolContext> ContextFactory { get; set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Create context for component
            Context = ContextFactory.CreateDbContext();

            // Set the user info in the RoleService
            RolesService.SetUserInfo(Context, AuthenticationStateTask);

            // Set the values locally
            ActiveUser = RolesService.ActiveUserRole?.Person;
            ActiveUserRoleType = RolesService.ActiveUserRole?.RoleType ?? RoleType.None;
            ActiveUserName = RolesService.ActiveUserName;
        }
    }
}
