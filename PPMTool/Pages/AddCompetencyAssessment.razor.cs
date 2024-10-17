using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Services;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class AddCompetencyAssessment : BasePage
    {
        [Parameter]
        public int CompetencyId { get; set; }

        [Parameter]
        public int AssessmentId { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        private Competency competency;
        private CompetencyAssessment assessment;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (CompetencyId > 0)
            {
                competency = CompetencyService.GetById(context, CompetencyId);
            }

            if (AssessmentId > 0)
            {
                assessment = competency.Assessments.FirstOrDefault(x => x.CompetencyAssessmentId == AssessmentId);
            }

            if (assessment == null)
            {
                assessment = new CompetencyAssessment();
            }

            LogInformation($"Adding / Editing assessment {assessment?.GetSensibleObjectName()}.");
        }

        private void DiscardChanges()
        {
            LogInformation($"Discarding assessment changes!");
            Navigation.NavigateTo($"competencies");
        }

        private void HandleValidSubmit()
        {
            if (competency != null)
            {
                // Write to the database
                LogInformation($"Saving assessment {assessment?.GetSensibleObjectName()}.");

                // TODO: Add or update assessment

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
