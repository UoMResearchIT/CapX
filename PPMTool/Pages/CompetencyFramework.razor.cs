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
        private RolesService RolesService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        private IEnumerable<Person> people;
        private IEnumerable<Competency> competencies;
        private bool userIsSuperuser;
        private int activeUserId;
        private Person selectedPerson = null;


        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Check user permissions
            var role = RolesService.GetByUsername(context, ActiveUserName);
            userIsSuperuser = role?.RoleType == Enums.RoleType.Superuser;
            activeUserId = role?.Person.PersonId ?? 0;

            // Get the active user by default
            selectedPerson = role?.Person;

            // Get starting lists from the DB
            people = PersonService.GetAll(context);
            competencies = CompetencyService.GetAll(context);

            LogInformation("Viewing competencies framework");
        }

        private void AddCompetency()
        {
            Navigation.NavigateTo("addcompetency/-1");
        }

        private void EditCompetency(Competency competency)
        {
            Navigation.NavigateTo($"addcompetency/{competency?.CompetencyId}");
        }

        private void AddAssessment(CompetencyAssessment assessment)
        {
            CompetencyService.AddAssessment(context, assessment);
        }

        private void UpdateAssessment(CompetencyAssessment assessment)
        {
            CompetencyService.UpdateAssessment(context, assessment);
        }
    }
}
