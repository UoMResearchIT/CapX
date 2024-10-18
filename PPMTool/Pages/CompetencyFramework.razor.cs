using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class CompetencyFramework : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        private IEnumerable<Person> people;
        private IEnumerable<Competency> competencies;


        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Get starting lists from the DB
            people = PersonService.GetAll(context);
            competencies = CompetencyService.GetAll(context);

        }
    }
}
