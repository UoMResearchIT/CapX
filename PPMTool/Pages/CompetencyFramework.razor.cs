using System.Diagnostics;
using DotNetExtensions;
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

        /// <summary>
        /// Represents a group of competencies based on grade and the meta data required to render the component
        /// </summary>
        private class CompetencyGroup
        {
            /// <summary>
            /// Grade of competencies in the group
            /// </summary>
            public int Grade { get; }

            /// <summary>
            /// Name of the competency group
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// Icon used for the competency group
            /// </summary>
            public string Icon { get; }

            /// <summary>
            /// Whether this group is selected or not (expands the accordion)
            /// </summary>
            public bool Selected { get; set; }

            /// <summary>
            /// Total number of competencies in the group
            /// </summary>
            public int Total { get; private set; }

            /// <summary>
            /// Number of competencies in the group the selected person has met
            /// </summary>
            public int Met { get; private set; }

            /// <summary>
            /// Competencies in the group, grouped by category
            /// </summary>
            public IEnumerable<IGrouping<CompetencyCategory, Competency>> CompetenciesGroupedByCategory { get; }

            /// <summary>
            /// Selection state of the competency category group in the competency group
            /// </summary>
            public IDictionary<CompetencyCategory, bool> CompetencySelectionState { get; }

            /// <summary>
            /// Number of competencies in the category group met by the selected person
            /// </summary>
            public IDictionary<CompetencyCategory, int> CompetencyMetValues { get; }

            /// <summary>
            /// The assessments against the competencies in this group
            /// </summary>
            public IEnumerable<CompetencyAssessment> CompetencyAssessments { get; }

            /// <summary>
            /// Event fired when an accordion state is toggled
            /// </summary>
            public event EventHandler<AccordionToggledEventArgs> OnAccordionToggled;

            /// <summary>
            /// Event arguments passed by accordion toggle event
            /// </summary>
            public class AccordionToggledEventArgs
            {
                /// <summary>
                ///  Title of the accordion
                /// </summary>
                public string Title { get; }

                /// <summary>
                /// New selected state of accordion
                /// </summary>
                public bool State { get; }

                /// <summary>
                /// Constructor
                /// </summary>
                /// <param name="title"></param>
                /// <param name="state"></param>
                public AccordionToggledEventArgs(string title, bool state)
                {
                    Title = title;
                    State = state;
                }
            }

            /// <summary>
            /// Constructor
            /// </summary>
            /// <param name="grade"></param>
            /// <param name="description"></param>
            /// <param name="icon"></param>
            /// <param name="groupedCompetencies"></param>
            public CompetencyGroup(int grade, string description, string icon, IEnumerable<IGrouping<CompetencyCategory, Competency>> groupedCompetencies, IEnumerable<CompetencyAssessment> assessments)
            {
                Grade = grade;
                Description = description;
                Icon = icon;
                CompetenciesGroupedByCategory = groupedCompetencies;
                CompetencySelectionState = new Dictionary<CompetencyCategory, bool>();
                CompetencyMetValues = new Dictionary<CompetencyCategory, int>();
                Total = groupedCompetencies.SelectMany(x => x).Count();
                CompetencyAssessments = assessments;

                // Initialise the dictionary of selection states
                foreach (var category in CompetenciesGroupedByCategory.Select(x => x.Key))
                {
                    CompetencySelectionState.Add(category, false);
                    CompetencyMetValues.Add(category, 0);
                }
            }

            /// <summary>
            /// Will take a boolean flag for the state of an accordion and toggle it
            /// </summary>
            /// <param name="state"></param>
            public void ToggleAccordion(CompetencyCategory? category = null)
            {
                // Decide on which accordion to expand
                if (category == null)
                {
                    Selected = !Selected;
                }
                else
                {
                    CompetencyCategory key = category ?? default;
                    CompetencySelectionState[key] = !CompetencySelectionState[key];
                }

                // Fire event
                var title = $"Grade {Grade}";
                var state = Selected;
                if (category != null)
                {
                    var group = CompetenciesGroupedByCategory.FirstOrDefault(x => x.Key == category);
                    title += $"| {group?.Key.GetDescription()}";
                    var key = group?.Key ?? default;
                    if (group != null)
                    {
                        state = CompetencySelectionState[key];
                    }
                }
                OnAccordionToggled?.Invoke(this, new AccordionToggledEventArgs(title, state));
            }

            /// <summary>
            /// Updates the met count for this competency group
            /// </summary>
            /// <param name="selectedPerson"></param>
            public void UpdateMet(Person selectedPerson)
            {
                Met = 0;
                if (selectedPerson != null)
                {
                    foreach (var group in CompetenciesGroupedByCategory)
                    {
                        // Get one "fully met" assessment per competency for the given person and count them
                        CompetencyMetValues[group.Key] = group
                            .SelectMany(x => x.Assessments)
                            .Where(x => x.Status == AssessmentStatus.FullyMet && x.Person.PersonId == selectedPerson.PersonId)
                            .DistinctBy(x => x.AssociatedCompetency.CompetencyId)
                            .Count();
                        Met += CompetencyMetValues[group.Key];
                    }
                }
            }
        }

        private Person selectedPerson = null;
        public Person SelectedPerson
        {
            get => selectedPerson;
            set
            {
                if (selectedPerson != value)
                {
                    selectedPerson = value;
                    UpdateMet();
                }
            }
        }

        private IEnumerable<Person> availablePeople;
        private IEnumerable<Competency> competencies;
        private bool userIsSuperuser;
        private int activeUserId;
        private string competencySearchTerms;
        private IEnumerable<CompetencyGroup> competencyGroups = new List<CompetencyGroup>();
        private bool showUnMetOnly;

        /// <summary>
        /// Method to update the met count of each available competency group
        /// </summary>
        private void UpdateMet()
        {
            foreach (var group in competencyGroups)
            {
                group.UpdateMet(selectedPerson);
            }
        }

        /// <summary>
        /// Generates a background task for loading the competency data
        /// </summary>
        /// <returns></returns>
        private Task GetTask()
        {
            InvokeAsync(() =>
            {
                Loading = true;
                StateHasChanged();
            });

            return Task.Run(() =>
            {
                Debug.WriteLine($"** Running competency load task for {selectedPerson}...");

                // Get starting lists from the DB
                availablePeople = PersonService.GetAll(Context).OrderBy(x => x.Name);
                if (!userIsSuperuser)
                {
                    // Self plus direct reports who are current
                    availablePeople = availablePeople
                        .Where(x => x.PersonId == activeUserId || (x.LineManager?.PersonId == activeUserId && x.IsCurrentStaff()))
                        .OrderBy(x => x.Name);
                }
                competencies = CompetencyService.GetAllActive(Context);

                // Filter the competencies by those with only unmet or no assessments
                if (showUnMetOnly)
                {
                    // Get assessments grouped by competency ID
                    var latestAssessments = competencies
                            .SelectMany(x => x.Assessments)
                            .Where(x => x.Person.PersonId == selectedPerson.PersonId)
                            .OrderByDescending(x => x.DateCreated)
                            .GroupBy(x => x.AssociatedCompetency.CompetencyId);

                    // Get a list of competency IDs for those where the latest assessment is fully met
                    var exceptionList = new List<int>();
                    foreach (var group in latestAssessments)
                    {
                        var assessment = group.First();
                        if (assessment.Status == AssessmentStatus.FullyMet)
                        {
                            exceptionList.Add(group.Key);
                        }
                    }

                    // Remove from the competencies all those within the exception list
                    competencies = competencies.Where(x => !exceptionList.Contains(x.CompetencyId));
                }

                // Setup the accordion data
                var groups = new List<CompetencyGroup>();
                var newGroup = new CompetencyGroup(
                    5,
                    "Foundation Level (Grade 5)",
                    "counter_1",
                    competencies
                        .Where(x => x.Grade == 5)
                        .GroupBy(x => x.Category)
                        .OrderBy(x => x.Key),
                    competencies
                        .Where(x => x.Grade == 5)
                        .SelectMany(x => x.Assessments)
                        .Where(x => x.Person.PersonId == selectedPerson.PersonId)
                );
                newGroup.OnAccordionToggled += OnAccordionToggled;
                groups.Add(newGroup);

                newGroup = new CompetencyGroup(
                    6,
                    "Advanced Level (Grade 6)",
                    "counter_2",
                    competencies
                        .Where(x => x.Grade == 6)
                        .GroupBy(x => x.Category)
                        .OrderBy(x => x.Key),
                    competencies
                        .Where(x => x.Grade == 6)
                        .SelectMany(x => x.Assessments)
                        .Where(x => x.Person.PersonId == selectedPerson.PersonId)
                );
                newGroup.OnAccordionToggled += OnAccordionToggled;
                groups.Add(newGroup);

                newGroup = new CompetencyGroup(
                    7,
                    "Leadership Level (Grade 7)",
                    "counter_3",
                    competencies
                        .Where(x => x.Grade == 7)
                        .GroupBy(x => x.Category)
                        .OrderBy(x => x.Key),
                    competencies
                        .Where(x => x.Grade == 7)
                        .SelectMany(x => x.Assessments)
                        .Where(x => x.Person.PersonId == selectedPerson.PersonId)
                );
                newGroup.OnAccordionToggled += OnAccordionToggled;
                groups.Add(newGroup);

                competencyGroups = groups;
                UpdateMet();

            }).ContinueWith(t =>
            {
                Debug.WriteLine($"** ...Competency load task complete: {t.Status}");

                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Handles accordion toggle events and refreshes the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccordionToggled(object sender, CompetencyGroup.AccordionToggledEventArgs e)
        {
            Debug.WriteLine($"** Accordion {e.Title} state changed to {e.State}");
            StateHasChanged();
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Check user permissions
            userIsSuperuser = ActiveUser?.RoleType == RoleType.Superuser;
            activeUserId = ActiveUser?.Person?.PersonId ?? 0;

            // Get the active user by default
            SelectedPerson = ActiveUser?.Person;

            // Kick off a DB task to get the data
            EnqueueLoadData(GetTask);

            LogInformation("Viewing competencies framework");
        }

        /// <summary>
        /// Go to add a competency
        /// </summary>
        private void AddCompetency()
        {
            Navigation.NavigateTo("competencies/addcompetency/-1");
        }

        /// <summary>
        /// Go to edit a competency
        /// </summary>
        /// <param name="competency"></param>
        private void EditCompetency(Competency competency)
        {
            Navigation.NavigateTo($"competencies/addcompetency/{competency?.CompetencyId}");
        }

        /// <summary>
        /// Adds an assessment
        /// </summary>
        /// <param name="assessment"></param>
        private void AddAssessment(CompetencyAssessment assessment)
        {
            LogInformation($"Adding assessment \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            if (ValidateAssessment(assessment, out var message))
            {
                CompetencyService.AddAssessment(Context, assessment);
            }
            else
            {
                ShowValidationError(message);
            }
            StateHasChanged();
        }

        /// <summary>
        /// Updates an assessment
        /// </summary>
        /// <param name="assessment"></param>
        private void UpdateAssessment(CompetencyAssessment assessment)
        {
            LogInformation($"Updating assessment to \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            if (ValidateAssessment(assessment, out var message))
            {
                CompetencyService.UpdateAssessment(Context, assessment);
            }
            else
            {
                ShowValidationError(message);
            }
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

        /// <summary>
        /// Callback for when a person is selected from the dropdown
        /// </summary>
        private void PersonSelected()
        {
            EnqueueLoadData(GetTask);
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
                    foreach (var group in competencyGroups)
                    {
                        // Collapse the top level
                        group.Selected = false;

                        // Collapse all the lower levels
                        foreach (var category in group.CompetencySelectionState.Keys)
                        {
                            group.CompetencySelectionState[category] = false;
                        }
                    }

                    // Find competencies with matching string
                    var term = competencySearchTerms.Trim().ToLower();
                    var matching = competencies.Where(x => x.GetHierarchyId().Contains(term) || x.Description.ToLower().Contains(term) || x.Objective.ToLower().Contains(term));

                    // Expand the accordions for those matching
                    foreach (var grade in matching.Select(x => x.Grade).Distinct())
                    {
                        // Set state of top level accordion
                        var group = competencyGroups.First(x => x.Grade == grade);
                        group.Selected = true;

                        // Set state of lower lever accordions
                        foreach (var category in matching.Where(x => x.Grade == grade).Select(x => x.Category).Distinct())
                        {
                            var cat = group.CompetencySelectionState.First(x => x.Key == category).Key;
                            group.CompetencySelectionState[cat] = true;
                        }
                    }
                    ;
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
