using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class ViewAbsences : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        private IEnumerable<Absence> currentAbsences;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Order by name by default
            var loadedPeople = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();

            // Current absences
            currentAbsences = loadedPeople
                .Where(x => x.IsCurrentlyAbsent())
                .Select(x => x.Absences.FirstOrDefault(x => x.IsCurrentAbsence()));
        }

        private void EditAbsence(Person person)
        {
            Navigation.NavigateTo($"/addabsence/{person.PersonId}");
        }
    }
}
