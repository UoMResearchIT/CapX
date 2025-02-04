using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Superuser,Manager,Developer")]
    public partial class CompetencyFramework : BasePage
    {
        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private IEnumerable<Person> availablePeople;
        private IEnumerable<Competency> competencies;
        private bool userIsSuperuser;
        private int activeUserId;
        private Person selectedPerson = null;
        private string competencySearchTerms;

        private Dictionary<int, bool> gradeAccordionSelected;
        private IEnumerable<IGrouping<CompetencyCategory, Competency>> groupedGrade5Competencies;
        private Dictionary<CompetencyCategory, bool> grade5CategoriesSelected = new();
        private IEnumerable<IGrouping<CompetencyCategory, Competency>> groupedGrade6Competencies;
        private Dictionary<CompetencyCategory, bool> grade6CategoriesSelected = new();
        private IEnumerable<IGrouping<CompetencyCategory, Competency>> groupedGrade7Competencies;
        private Dictionary<CompetencyCategory, bool> grade7CategoriesSelected = new();

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Check user permissions
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            userIsSuperuser = role?.RoleType == RoleType.Superuser;
            activeUserId = ActiveUser?.PersonId ?? 0;

            // Get the active user by default
            selectedPerson = ActiveUser;

            // Get starting lists from the DB
            availablePeople = PersonService.GetAll(Context).OrderBy(x => x.Name);
            if (!userIsSuperuser)
            {
                // Self plus direct reports who are current
                availablePeople = availablePeople
                    .Where(x => x.PersonId == activeUserId || (x.LineManager?.PersonId == activeUserId && x.IsCurrentStaff()))
                    .OrderBy(x => x.Name);
            }
            competencies = CompetencyService.GetAll(Context);

            // Prepare the bindings for the expansion setting of the accordions
            groupedGrade5Competencies = competencies.Where(x => x.Grade == 5).GroupBy(x => x.Category).OrderBy(x => x.Key);
            foreach (var category in groupedGrade5Competencies.Select(x => x.Key))
            {
                grade5CategoriesSelected.Add(category, false);
            }
            groupedGrade6Competencies = competencies.Where(x => x.Grade == 6).GroupBy(x => x.Category).OrderBy(x => x.Key);
            foreach (var category in groupedGrade6Competencies.Select(x => x.Key))
            {
                grade6CategoriesSelected.Add(category, false);
            }
            groupedGrade7Competencies = competencies.Where(x => x.Grade == 7).GroupBy(x => x.Category).OrderBy(x => x.Key);
            foreach (var category in groupedGrade7Competencies.Select(x => x.Key))
            {
                grade7CategoriesSelected.Add(category, false);
            }
            gradeAccordionSelected = new Dictionary<int, bool>
            {
                { 5, false },
                { 6, false },
                { 7, false }
            };

            LogInformation("Viewing competencies framework");
        }

        private void AddCompetency()
        {
            Navigation.NavigateTo("competencies/addcompetency/-1");
        }

        private void EditCompetency(Competency competency)
        {
            Navigation.NavigateTo($"competencies/addcompetency/{competency?.CompetencyId}");
        }

        private void AddAssessment(CompetencyAssessment assessment)
        {
            LogInformation($"Adding assessment \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            if (ValidateAssessment(assessment, out var message)) CompetencyService.AddAssessment(Context, assessment);
            else ShowValidationError(message);
            StateHasChanged();
        }

        private void UpdateAssessment(CompetencyAssessment assessment)
        {
            LogInformation($"Updating assessment to \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            if (ValidateAssessment(assessment, out var message)) CompetencyService.UpdateAssessment(Context, assessment);
            else ShowValidationError(message);
            StateHasChanged();
        }

        /// <summary>
        /// General method to show a validation error notification
        /// </summary>
        /// <param name="message"></param>
        private void ShowValidationError(string message)
        {
            ShowNotification(new CapXNotificationMessage
            {
                Summary = "Validation Error",
                Detail = message
            });
        }

        /// <summary>
        /// Check the assessment model is correct before adding or updating
        /// </summary>
        /// <param name="assessment"></param>
        /// <returns></returns>
        private bool ValidateAssessment(CompetencyAssessment assessment, out string message)
        {
            if (string.IsNullOrWhiteSpace(assessment.Evidence) || string.IsNullOrWhiteSpace(HtmlHelper.ConvertToPlainText(assessment.Evidence)))
            {
                message = "Evidence is required!";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(assessment.CompetencyDescription) || string.IsNullOrWhiteSpace(HtmlHelper.ConvertToPlainText(assessment.CompetencyDescription)))
            {
                message = "Competency description is required!";
                return false;
            }
            else if (string.IsNullOrWhiteSpace(assessment.CompetencyObjective) || string.IsNullOrWhiteSpace(HtmlHelper.ConvertToPlainText(assessment.CompetencyObjective)))
            {
                message = "Competency objective is required!";
                return false;
            }
            message = string.Empty;
            return true;
        }

        private void PersonSelected()
        {
            StateHasChanged();
        }

        /// <summary>
        /// Method to strip out the expected (non-compliant) input characters and replace with something standard
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string Clean(string line)
        {
            return line.Replace("\r", "").Replace("\"", "");
        }

        /// <summary>
        /// Filter the visible competencies based on the search terms
        /// </summary>
        private void FilterCompetencies()
        {
            LogInformation($"Searching for competencies with: {competencySearchTerms}");

            // Clear existing highlighting
            InvokeAsync(async () =>
            {
                await JSRuntime.InvokeVoidAsync("clearHighlightInCompetencies");
            }).ContinueWith(async t =>
            {
                if (!string.IsNullOrWhiteSpace(competencySearchTerms))
                {
                    // Collapse all the accordions
                    foreach (var grade in gradeAccordionSelected.Keys)
                    {
                        gradeAccordionSelected[grade] = false;
                    }
                    foreach (var category in grade5CategoriesSelected.Keys.Distinct())
                    {
                        grade5CategoriesSelected[category] = false;
                    }
                    foreach (var category in grade6CategoriesSelected.Keys.Distinct())
                    {
                        grade6CategoriesSelected[category] = false;
                    }
                    foreach (var category in grade7CategoriesSelected.Keys.Distinct())
                    {
                        grade7CategoriesSelected[category] = false;
                    }

                    // Find competencies with matching string
                    var term = competencySearchTerms.Trim().ToLower();
                    var matching = competencies.Where(x => x.GetHierarchyId().Contains(term) || x.Description.ToLower().Contains(term) || x.Objective.ToLower().Contains(term));

                    // Expand the accordions for those matching
                    foreach (var grade in matching.Select(x => x.Grade).Distinct())
                    {
                        gradeAccordionSelected[grade] = true;
                    };
                    foreach (var category in matching.Where(x => x.Grade == 5).Select(x => x.Category).Distinct())
                    {
                        grade5CategoriesSelected[category] = true;
                    }
                    foreach (var category in matching.Where(x => x.Grade == 6).Select(x => x.Category).Distinct())
                    {
                        grade6CategoriesSelected[category] = true;
                    }
                    foreach (var category in matching.Where(x => x.Grade == 7).Select(x => x.Category).Distinct())
                    {
                        grade7CategoriesSelected[category] = true;
                    }
                    await InvokeAsync(StateHasChanged);

                    // Highlight matching text on the page with a JS call
                    await JSRuntime.InvokeVoidAsync("highlightInCompetencies", competencySearchTerms.Trim());
                }
            });
        }

        /// <summary>
        /// Clear the competency search box and re-filter
        /// </summary>
        private void ClearSearch()
        {
            competencySearchTerms = string.Empty;
            FilterCompetencies();
        }
    }
}
