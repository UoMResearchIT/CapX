using System.Diagnostics;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Services;
using Radzen;
using Xceed.Words.NET;

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
            /// <param name="assessments"></param>
            public CompetencyGroup(
                int grade,
                string description,
                string icon,
                IEnumerable<IGrouping<CompetencyCategory, Competency>> groupedCompetencies,
                IEnumerable<CompetencyAssessment> assessments)
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
            /// <param name="category"></param>
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
                            .Where(x => x.Status == AssessmentStatus.FullyMet && x.PersonId == selectedPerson.PersonId)
                            .DistinctBy(x => x.CompetencyId)
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
        private bool showAllStaff = true;
        private bool exportRunning;
        private bool downloadFrameworkRunning;

        private class CompetencyAssessmentExportLine
        {
            /// <summary>
            /// Three digit ID so we can identify grade, category, number
            /// </summary>
            public string Id { get; }

            /// <summary>
            /// The category description
            /// </summary>
            public string Category { get; }

            /// <summary>
            /// Description of the competency
            /// </summary>
            public string Description { get; }

            /// <summary>
            /// DateTime of the latest assessment
            /// </summary>
            public DateTime LatestAssessmentDate { get; }

            /// <summary>
            /// Status of the latest assessment
            /// </summary>
            public string AssessmentStatus { get; }

            /// <summary>
            /// Evidence associated with the latest assessment
            /// </summary>
            public string Evidence { get; }

            /// <summary>
            /// Constructor assigns the properties from the competency and the latest assessment
            /// </summary>
            /// <param name="competency"></param>
            /// <param name="latestAssessment"></param>
            public CompetencyAssessmentExportLine(Competency competency, CompetencyAssessment latestAssessment)
            {
                Id = competency.GetHierarchyId();
                Category = competency.Category.GetDescription();
                Description = HtmlHelper.ConvertToPlainText(competency.Description);
                LatestAssessmentDate = latestAssessment == null ? new DateTime() : DateTime.Parse(latestAssessment.DateCreated);
                AssessmentStatus = latestAssessment == null ? Enums.AssessmentStatus.Unmet.ToNiceString() : latestAssessment.Status.ToNiceString();
                Evidence = latestAssessment == null ? "Never assessed!" : HtmlHelper.ConvertToPlainText(latestAssessment.Evidence);
            }
        }

        /// <summary>
        /// Method to export the competency framework to a Word doc to make it easier to update as a group
        /// </summary>
        /// <returns></returns>
        private async Task ExportFrameworkAsync()
        {
            LogInformation($"Exporting competency framework to Word...");

            downloadFrameworkRunning = true;

            await Task.Run(async () =>
            {
                try
                {
                    // Create file path
                    var filename = $"CompetencyFramework_{DateTime.Now.ToString("yyyyMMddHHmmss")}.docx";
                    var path = FileHelper.GetLocalApplicationFilePath(filename);

                    // Create Word Doc
                    var doc = DocX.Create(path);
                    foreach (var group in competencyGroups)
                    {
                        // Write Heading 1
                        var text = doc.InsertParagraph(group.Description);
                        text.StyleId = "Heading1";

                        foreach (var category in group.CompetenciesGroupedByCategory)
                        {
                            // Write Heading 2
                            text = doc.InsertParagraph(category.Key.GetDescription());
                            text.StyleId = "Heading2";

                            foreach (var competency in category.Where(x => x.IsActive))
                            {
                                // Write Heading 3
                                text = doc.InsertParagraph(competency.GetHierarchyId());
                                text.StyleId = "Heading3";

                                // Write Competency Description
                                HtmlHelper.InsertHtmlLikeTextWithLinks(doc, competency.GetSensibleObjectName(), "Normal");

                                // Write Competency Objective
                                doc.InsertParagraph("");
                                text = doc.InsertParagraph("Objective");
                                text.StyleId = "Normal";
                                text.Bold();
                                HtmlHelper.InsertHtmlLikeTextWithLinks(doc, competency.Objective, "Normal");
                            }
                        }
                    }

                    // Save the document
                    doc.Save();

                    await InvokeAsync(async () =>
                    {
                        // Get file stream
                        using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                        // Invoke JS on the client to download the file
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    });
                }
                catch (Exception e)
                {
                    LogError($"Exporting framework failed: {e}");
                }

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    LogInformation($"Framework export task finished {t.Status}");
                    downloadFrameworkRunning = false;
                    StateHasChanged();
                });
            });
        }

        /// <summary>
        /// Method to export an Excel file with the information in it from the currently displayed journey
        /// </summary>
        private async Task ExportDataAsync()
        {
            LogInformation($"Exporting development journey for {SelectedPerson?.Name}...");

            exportRunning = true;

            await Task.Run(async () =>
            {
                try
                {
                    // Create blank list of data
                    var assessments = new List<CompetencyAssessmentExportLine>();

                    // Get the assessment info
                    if (SelectedPerson == null)
                    {
                        LogWarning("Tried to export data without selecting a person!");
                        return;
                    }

                    // Go through the groups and extract the info
                    foreach (var group in competencyGroups)
                    {
                        foreach (var category in group.CompetenciesGroupedByCategory)
                        {
                            foreach (var competency in category.Where(x => x.IsActive))
                            {
                                var latestAssessment = competency.Assessments
                                    .Where(x => x.PersonId == SelectedPerson.PersonId)
                                    .OrderByDescending(x => DateTime.Parse(x.DateCreated))
                                    .FirstOrDefault();
                                assessments.Add(new CompetencyAssessmentExportLine(competency, latestAssessment));
                            }
                        }
                    }

                    // Create file path
                    var filename = $"DevelopmentJourney_{SelectedPerson?.ShortName}_{DateTime.Now.ToString("yyyyMMddHHmmss")}.xlsx";
                    var path = FileHelper.GetLocalApplicationFilePath(filename);

                    // Create workbook and worksheet
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Data");

                        // Write header row
                        var props = typeof(CompetencyAssessmentExportLine).GetProperties();
                        var propNames = props.Select(x => x.Name).ToList();
                        for (int i = 0; i < propNames.Count; i++)
                        {
                            var cell = worksheet.Cell(1, i + 1);
                            cell.Value = propNames[i];
                            cell.Style.Font.Bold = true;
                        }

                        // Write data rows
                        for (int row = 0; row < assessments.Count; row++)
                        {
                            var record = assessments[row];
                            for (int col = 0; col < propNames.Count; col++)
                            {
                                var property = record.GetType().GetProperty(propNames[col]);
                                var rawValue = property?.GetValue(record);
                                var cell = worksheet.Cell(row + 2, col + 1);

                                // Format and assign
                                if (propNames[col] == "LatestAssessmentDate")
                                {
                                    if (rawValue is DateTime dt)
                                    {
                                        cell.Value = dt;
                                        cell.Style.DateFormat.Format = "dd/MM/yyyy";
                                    }
                                    else
                                    {
                                        cell.Value = rawValue?.ToString() ?? string.Empty;
                                    }
                                }
                                else
                                {
                                    if (rawValue is int)
                                    {
                                        cell.Value = (int)rawValue;
                                    }
                                    else if (rawValue is double)
                                    {
                                        cell.Value = (double)rawValue;
                                    }
                                    else
                                    {
                                        cell.Value = rawValue?.ToString() ?? string.Empty;
                                    }
                                }
                            }
                        }

                        // Save the workbook
                        workbook.SaveAs(path);

                        Debug.WriteLine($"** Exported {assessments.Count} rows to {path}");
                    }

                    await InvokeAsync(async () =>
                    {
                        // Get file stream
                        using var streamRef = new DotNetStreamReference(stream: File.Open(path, FileMode.Open));

                        // Invoke JS on the client to download the file
                        await JSRuntime.InvokeVoidAsync("downloadFileFromStream", filename, streamRef);
                    });
                }
                catch (Exception e)
                {
                    LogError($"Exporting development journey failed: {e}");
                }

            }).ContinueWith(t =>
            {
                InvokeAsync(() =>
                {
                    LogInformation($"Export task finished {t.Status}");
                    exportRunning = false;
                    StateHasChanged();
                });
            });
        }

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
        /// Method to create a background task for loading the available people dropdown
        /// </summary>
        /// <returns></returns>
        private async Task GetAvailablePeopleAsync()
        {
            Debug.WriteLine($"** Getting people for {ActiveUser?.Name}...");

            // Get starting lists from the DB
            availablePeople = await PersonService.GetAllShallowAsync(Context);
            if (!showAllStaff)
            {
                availablePeople = availablePeople.Where(x => x.IsCurrentStaff());
            }
            if (!userIsSuperuser)
            {
                // Self plus direct reports
                availablePeople = availablePeople
                    .Where(x => x.PersonId == activeUserId || x.LineManager?.PersonId == activeUserId)
                    .OrderBy(x => x.Name);
            }
        }

        /// <summary>
        /// Generates a background task for loading the competency data
        /// </summary>
        /// <returns></returns>
        private async Task LoadDataAsync()
        {
            Loading = true;
            StateHasChanged();

            Debug.WriteLine($"** Running competency load task for {selectedPerson?.Name}...");

            // Populate the drop down
            await GetAvailablePeopleAsync();

            // Get all active competencies from DB
            competencies = await CompetencyService.GetAllActiveAsync(Context);

            // Run a background task to do the processing
            await Task.Run(() =>
            {
                // Filter the competencies by those with only unmet or no assessments
                if (showUnMetOnly)
                {
                    // Get assessments grouped by competency ID
                    var latestAssessments = competencies
                            .SelectMany(x => x.Assessments)
                            .Where(x => x.PersonId == selectedPerson.PersonId)
                            .OrderByDescending(x => DateTime.Parse(x.DateCreated))
                            .GroupBy(x => x.CompetencyId);

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
                        .Where(x => x.PersonId == selectedPerson?.PersonId)
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
                        .Where(x => x.PersonId == selectedPerson?.PersonId)
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
                        .Where(x => x.PersonId == selectedPerson?.PersonId)
                );
                newGroup.OnAccordionToggled += OnAccordionToggled;
                groups.Add(newGroup);

                competencyGroups = groups;
                UpdateMet();
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
        /// Handles accordion toggle events and refreshes the view
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAccordionToggled(object sender, CompetencyGroup.AccordionToggledEventArgs e)
        {
            Debug.WriteLine($"** Accordion {e.Title} state changed to {e.State}");
            StateHasChanged();
        }

        /// <summary>
        /// Run when the component is first created
        /// </summary>
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            // Check user permissions
            userIsSuperuser = ActiveUser?.RoleType == RoleType.Superuser;
            activeUserId = ActiveUser?.Person?.PersonId ?? 0;

            // Get the active user by default
            SelectedPerson = ActiveUser?.Person;

            // Kick off a DB task to get the data
            await LoadDataAsync();

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
            LogInformation($"Adding assessment \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.CompetencyId}");
            if (ValidateAssessment(assessment, out var message))
            {
                CompetencyService.AddAssessment(Context, assessment);
                UpdateMet();
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
            LogInformation($"Updating assessment to Evidence: \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.CompetencyId}");
            if (ValidateAssessment(assessment, out var message))
            {
                CompetencyService.UpdateAssessment(Context, assessment);
                UpdateMet();
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
        /// <param name="message"></param>
        /// <returns></returns>
        private bool ValidateAssessment(CompetencyAssessment assessment, out string message)
        {
            // Need to have evidence but only if assessment status is partially met or met
            if ((string.IsNullOrWhiteSpace(assessment.Evidence) || string.IsNullOrWhiteSpace(HtmlHelper.ConvertToPlainText(assessment.Evidence))) && assessment.Status != AssessmentStatus.Unmet)
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
        private async Task PersonSelectedAsync()
        {
            await LoadDataAsync();
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
                    var term = competencySearchTerms.Clean();
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
