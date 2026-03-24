using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageAccess : DataGridPage<User>
    {
        [Inject]
        public PersonService PersonService { get; set; }

        private List<Person> people;
        private List<RoleType> roles;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = UserService;
            dataGridEntities = UserService.GetAll(Context).OrderBy(x => x.GetName()).ToList();

            // Populate the people and role types for the dropdowns
            roles = Enum.GetValues<RoleType>().ToList();
            people = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();

            LogInformation($"Viewing access grid");
        }

        protected override async Task DeleteRow(User entity)
        {
            if (await DialogService.Confirm($"You are about to delete access record {entity.GetSensibleObjectName()}.", "Delete Access") ?? false)
            {
                await base.DeleteRow(entity);
                UserService.Delete(Context, entity);
                LogInformation($"Deleted access record for {entity.GetSensibleObjectName()}");
            }
        }

        protected override async Task SaveRow(User entity)
        {
            // Validate
            if (string.IsNullOrWhiteSpace(entity.CASUserName))
            {
                SetErrorMessage(new StatusMessage("You must supply a user name", StatusMessage.MessageType.Error));
                return;
            }
            if (string.IsNullOrWhiteSpace(entity.Name))
            {
                SetErrorMessage(new StatusMessage("You must give the user a name", StatusMessage.MessageType.Error));
                return;
            }

            await base.SaveRow(entity);
        }
    }
}