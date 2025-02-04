using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private byte[] file;
        private string fileName;
        private long? fileSize;
        private string competencySearchTerms;
        private IEnumerable<CompetencyGroup> competencyGroups = new List<CompetencyGroup>();

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

                // Setup the accordion data
                var groups = new List<CompetencyGroup>();
                groups.Add(new CompetencyGroup(
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
                ));
                groups.Add(new CompetencyGroup(
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
                ));
                groups.Add(new CompetencyGroup(
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
                ));
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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Check user permissions
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            userIsSuperuser = role?.RoleType == RoleType.Superuser;
            activeUserId = ActiveUser?.PersonId ?? 0;

            // Get the active user by default
            SelectedPerson = ActiveUser;

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
            if (ValidateAssessment(assessment, out var message)) CompetencyService.AddAssessment(Context, assessment);
            else ShowValidationError(message);
            StateHasChanged();
        }

        /// <summary>
        /// Updates an assessment
        /// </summary>
        /// <param name="assessment"></param>
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
            EnqueueLoadData(GetTask);
        }

        private void OnError(UploadErrorEventArgs args, string name)
        {
            LogError($"File Upload Failed: {args.Message}");
        }

        private void OnFileChanged(byte[] value, string name)
        {
            // Start the spinner
            Loading = true;

            if (value != null) LogInformation($"File Uploaded - adding competency assessments for {selectedPerson?.Name} from the file...");

            Task.Run(() =>
            {
                try
                {
                    // Create a context to be accesed on this thread
                    var threadContext = ContextFactory.CreateDbContext();
                    var localCompetencies = CompetencyService.GetAll(threadContext);
                    var localPerson = PersonService.GetById(threadContext, SelectedPerson.PersonId);

                    // Bail or read from stream
                    if (value == null)
                    {
                        return;
                    }

                    // Convert text -- arrives as a base64 image bizarrely!
                    var fileText = System.Text.Encoding.Default.GetString(value);
                    string[] dbInfo = fileText.Split("base64,");
                    var base64Contents = dbInfo[1].ToString();
                    byte[] contentsAsBytes = Convert.FromBase64String(base64Contents);
                    fileText = System.Text.Encoding.Default.GetString(contentsAsBytes);

                    // Split into lines
                    var lines = fileText.Split("\n");

                    // Read one line at a time
                    string legacyId1 = null;
                    string legacyId2 = null;
                    string legacyId3 = null;
                    foreach (var line in lines)
                    {
                        // Split line initially after first | since we replaced all NBSP with | characters
                        var values = Clean(line).Split("|");

                        // Continue if line is shorter than expected then reset the ID tracker
                        if (values.Length < 2)
                        {
                            legacyId1 = null;
                            legacyId2 = null;
                            legacyId3 = null;
                            continue;
                        }

                        // If the value is of the pattern 1.1 then restart as this is the first two digits of the legacy ID
                        var test = values[0] + "|";
                        if (Regex.IsMatch(test, @"\d+\.\d+\|"))
                        {
                            legacyId2 = null;
                            legacyId3 = null;
                            legacyId1 = values[0].Trim();
                        }
                        // If the value is of the pattern 1. then append as this completed the legacy ID for top level items
                        else if (Regex.IsMatch(test, @"\d+\.\|"))
                        {
                            legacyId3 = null;
                            legacyId2 = values[0].Replace(".", "").Trim();
                        }
                        // If the value is of the pattern a. then append as this completes the legacy ID for sub items
                        else if (Regex.IsMatch(test, @"[a-z]\.\|"))
                        {
                            legacyId3 = values[0].Replace(".", "").Trim();
                        }

                        // If no legacy ID then move to next line
                        if (legacyId1 == null || legacyId2 == null) continue;

                        // Look at the rest of the line
                        var valuesRest = values[values.Length - 1].Split("\t");

                        // Check number of values
                        if (valuesRest.Length != 5) continue;

                        // If there is an "x" or "X" then this represents a selection and can infer a status
                        AssessmentStatus status = default;
                        if (valuesRest[1].Trim().ToLower() == "x")
                        {
                            status = AssessmentStatus.Unmet;
                        }
                        else if (valuesRest[2].Trim().ToLower() == "x")
                        {
                            status = AssessmentStatus.PartiallyMet;
                        }
                        else if (valuesRest[3].Trim().ToLower() == "x")
                        {
                            status = AssessmentStatus.FullyMet;
                        }
                        else
                        {
                            continue;
                        }

                        // Cross check line against competencies to see if LegacyId matches
                        var legacyId = string.Join(".", new string[] { legacyId1, legacyId2, legacyId3 });
                        if (legacyId.EndsWith("."))
                        {
                            // Strip trailing dot if necessary
                            legacyId = legacyId.Substring(0, legacyId.Length - 1);
                        }
                        var matchingCompetency = localCompetencies.FirstOrDefault(x => x.LegacyId.Trim().ToLower() == legacyId.Trim().ToLower());
                        if (matchingCompetency == null)
                        {
                            LogWarning($"Valid competency but no matching competency in the DB. LegacyId = {legacyId}");
                            continue;
                        }

                        // Check the existing assessments to see if this represents a change from the latest
                        var latestAssessment = matchingCompetency.Assessments.Where(x => x.Person.PersonId == localPerson.PersonId).OrderBy(x => x.DateCreated).LastOrDefault();
                        if (latestAssessment != null)
                        {
                            if (status == latestAssessment.Status)
                            {
                                LogWarning($"Assessment not imported as not a change based on the latest assessment for the competency with LegacyId = {legacyId} | Status = {status}");
                                continue;
                            }
                        }

                        // Add assessment to DB
                        LogInformation($"Adding assessment against competency LegacyId {legacyId} for {localPerson.Name} to the DB");
                        var assessment = new CompetencyAssessment
                        {
                            AssociatedCompetency = matchingCompetency,
                            CompetencyDescription = matchingCompetency.Description,
                            CompetencyObjective = matchingCompetency.Objective,
                            CompetencyRevision = matchingCompetency.Revision,
                            Person = localPerson,
                            Status = status,
                            Evidence = string.IsNullOrWhiteSpace(valuesRest[4]) ? "No evidence supplied" : valuesRest[4].Trim()
                        };

                        if (ValidateAssessment(assessment, out var message))
                        {
                            CompetencyService.AddAssessment(threadContext, assessment);
                        }
                        else
                        {
                            LogError($"Assessment validation failed {message}!");
                        }
                    }

                    Debug.WriteLine($"** Finished reading lines.");
                }
                catch (Exception ex)
                {
                    // Present an error notification to the user
                    InvokeAsync(() => ShowNotification(new CapXNotificationMessage
                    {
                        Summary = "Upload Issue",
                        Detail = $"{ex.Message}",
                        Duration = 10000
                    }));
                    LogError($"{ex.Message}");

                }
            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    Loading = false;
                    StateHasChanged();
                });
            });
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
                    };
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
