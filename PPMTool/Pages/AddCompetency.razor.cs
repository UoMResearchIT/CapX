using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser")]
    public partial class AddCompetency : BasePage
    {
        [Parameter]
        public int CompetencyId { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        private Competency competency;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (CompetencyId > 0)
            {
                competency = CompetencyService.GetById(context, CompetencyId);
            }
            else
            {
                competency = new Competency();
            }

            LogInformation($"Adding / Editing competency {competency?.GetSensibleObjectName()}");
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding competency changes!");
            Navigation.NavigateTo($"competencies");
        }

        private void HandleValidSubmit()
        {
            if (competency != null)
            {
                // Write to the database
                LogInformation($"Saving competency {competency?.GetSensibleObjectName()}.");

                // Try to add or update
                int result = -1;
                if (competency?.CompetencyId != 0)
                {
                    result = CompetencyService.Update(context, competency);
                }
                else
                {
                    result = CompetencyService.Add(context, competency);
                }

                // Navigate back
                Navigation.NavigateTo($"competencies");
            }
        }
    }
}
