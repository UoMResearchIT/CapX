using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    public abstract class BasePageWithLineManagerCheck : BasePage
    {
        [Inject]
        protected RolesService RolesService { get; set; }

        protected Person ActiveUser { get; private set; }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get the active user
            ActiveUser = RolesService.GetByUsername(Context, ActiveUserName)?.Person;
        }

        /// <summary>
        /// Check whether the current user is the line manager of the person or a superuser
        /// </summary>
        /// <param name="person"></param>
        /// <returns></returns>
        protected bool IsSuperuserOrLineManagerOfThisPerson(Person person)
        {
            var lm = (person?.LineManager.PersonId ?? 0) == (ActiveUser?.PersonId ?? -1);
            var su = AuthenticationState?.User.IsInRole("Superuser") ?? false;
            return lm || su;
        }
    }
}
