using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class ManageAccess : DataGridPage<Role>
    {
        [Inject]
        public RolesService RolesService { get; set; }

        [Inject]
        public PersonService PersonService { get; set; }

        [Inject]
        public IJSRuntime JsRuntime { get; set; }

        private List<Person> people;
        private List<RoleType> roles;

        protected override void OnInitialized()
        {
            base.OnInitialized();
            dataGridEntityService = RolesService;
            dataGridEntities = RolesService.GetAll(context).OrderBy(x => x.Person?.Name).ToList();

            // Populate the people and role types for the dropdowns
            roles = Enum.GetValues(typeof(RoleType)).ToDynamicList<RoleType>();
            people = PersonService.GetAll(context).OrderBy(x => x.Name).ToList();

            LogInformation($"Viewing access grid");
        }

        protected override async Task DeleteRow(Role entity)
        {
            if (await JsRuntime.InvokeAsync<bool>("confirm", $"You are about to delete innate code {entity.GetSensibleObjectName()}. Are you sure?"))
            {
                await base.DeleteRow(entity);
                RolesService.Delete(context, entity);
                LogInformation($"Deleted access record for {entity.GetSensibleObjectName()}");
            }
        }
    }
}