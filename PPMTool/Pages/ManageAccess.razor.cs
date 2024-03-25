using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic.Core;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
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
    }
}