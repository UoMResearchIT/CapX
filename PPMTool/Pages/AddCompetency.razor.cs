// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
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

        [Inject]
        private HtmlContentSanitizer HtmlContentSanitizer { get; set; }

        private Competency competency;

        /// <summary>
        /// Intermediate property to bind the editor to so we can sanitise before updating the actual model
        /// </summary>
        private string CompetencyDescriptionValue
        {
            get => competency?.Description ?? string.Empty;
            set
            {
                if (competency == null) return;
                competency.Description = HtmlContentSanitizer.Sanitize(value);
            }
        }

        /// <summary>
        /// Intermediate property to bind the editor to so we can sanitise before updating the actual model
        /// </summary>
        private string CompetencyObjectiveValue
        {
            get => competency?.Objective ?? string.Empty;
            set
            {
                if (competency == null) return;
                competency.Objective = HtmlContentSanitizer.Sanitize(value);
            }
        }
        private CompetencyCategory? originalCategory = null;
        private int? originalNumber = null;
        private int? originalGrade = null;
        private IEnumerable<Competency> competencies;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            competencies = await CompetencyService.GetAllAsync(Context);

            if (CompetencyId > 0)
            {
                competency = CompetencyService.GetById(Context, CompetencyId);
                originalCategory = competency?.Category;
                originalNumber = competency?.Number;
                originalGrade = competency?.Grade;
            }
            else
            {
                competency = new Competency();
            }

            SetDefaultActionBar(HandleValidSubmit, DiscardChanges);

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
                ClearErrorMessage();

                // Validate
                if (competency.Grade < 5 || competency.Grade > 7)
                {
                    SetErrorMessage(new StatusMessage("Competency framework only supports grades 5-7 at the moment!", StatusMessage.MessageType.Error));
                    return;
                }
                if (string.IsNullOrWhiteSpace(competency.Description) || string.IsNullOrWhiteSpace(competency.Objective))
                {
                    SetErrorMessage(new StatusMessage("Every competency needs a description and an objective!", StatusMessage.MessageType.Error));
                    return;
                }

                int result = -1;
                if (competency?.CompetencyId != 0)
                {
                    // Increment the revision and set the revision date
                    competency.Revision++;
                    competency.RevisionDate = DateTime.Now.ToString("R");
                    result = CompetencyService.Update(Context, competency);
                }
                else
                {
                    result = CompetencyService.Add(Context, competency);
                }

                if (result == -1)
                {
                    SetErrorMessage(new StatusMessage("Competency with the same Legacy ID exists already!", StatusMessage.MessageType.Error));
                    return;
                }

                // Navigate back
                Navigation.NavigateTo($"competencies");
            }
        }

        /// <summary>
        /// Method invoked when the competency category or grade is changed to auto-increment the number if necessary
        /// </summary>
        private void CategoryOrGradeChanged()
        {
            if (competency.Category == originalCategory && competency.Grade == originalGrade)
            {
                competency.Number = originalNumber ?? 0;
            }
            else
            {
                var allCompetenciesInThisCategoryAndGrade = competencies
                    .Where(x => x.Grade == competency.Grade && x.Category == competency.Category && x.CompetencyId != competency.CompetencyId)
                    .OrderBy(x => x.Number);
                var lastNumber = allCompetenciesInThisCategoryAndGrade.LastOrDefault()?.Number ?? 0;
                competency.Number = lastNumber + 1;
            }
        }
    }
}
