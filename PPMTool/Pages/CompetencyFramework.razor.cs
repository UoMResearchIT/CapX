using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
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
        private RolesService RolesService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private CompetencyService CompetencyService { get; set; }

        [Inject]
        private IJSRuntime JSRuntime { get; set; }

        private IEnumerable<Person> people;
        private IEnumerable<Competency> competencies;
        private bool userIsSuperuser;
        private int activeUserId;
        private Person selectedPerson = null;
        private byte[] file;
        private string fileName;
        private long? fileSize;
        private string competencySearchTerms;

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // Check user permissions
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            userIsSuperuser = role?.RoleType == Enums.RoleType.Superuser;
            activeUserId = role?.Person.PersonId ?? 0;

            // Get the active user by default
            selectedPerson = role?.Person;

            // Get starting lists from the DB
            people = PersonService.GetAll(Context).Where(x => x.IsCurrentStaff()).OrderBy(x => x.Name);
            competencies = CompetencyService.GetAll(Context);

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
            LogInformation($"Adding assessment \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            CompetencyService.AddAssessment(Context, assessment);
            StateHasChanged();
        }

        private void UpdateAssessment(CompetencyAssessment assessment)
        {
            LogInformation($"Updating assessment to \"{assessment.Evidence}\" | Status = {assessment.Status} for {selectedPerson?.Name} for competency {assessment.AssociatedCompetency?.CompetencyId}");
            CompetencyService.UpdateAssessment(Context, assessment);
            StateHasChanged();
        }

        private void PersonSelected()
        {
            StateHasChanged();
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
                    var localPerson = PersonService.GetById(threadContext, selectedPerson.PersonId);

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
                        CompetencyService.AddAssessment(threadContext, new CompetencyAssessment
                        {
                            AssociatedCompetency = matchingCompetency,
                            CompetencyDescription = matchingCompetency.Description,
                            CompetencyObjective = matchingCompetency.Objective,
                            CompetencyRevision = matchingCompetency.Revision,
                            Person = localPerson,
                            Status = status,
                            Evidence = string.IsNullOrWhiteSpace(valuesRest[4]) ? "No evidence supplied" : valuesRest[4].Trim()
                        });
                    }

                    Debug.WriteLine($"** Finished reading lines.");
                }
                catch (Exception ex)
                {
                    // Present an error notification to the user
                    InvokeAsync(() => ShowNotification(new NotificationMessage
                    {
                        Severity = NotificationSeverity.Error,
                        Summary = "Upload Issue",
                        Detail = $"{ex.Message}",
                        Duration = 10000,
                        Style = "position: fixed; top: 100%; left: 50%; transform: translate(-50%, -100%); width: 100%"
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
                    // TODO: Collapse all the accordions

                    // Find competencies with matching string
                    var term = competencySearchTerms.Trim().ToLower();
                    var matching = competencies.Where(x => x.GetHierarchyId().Contains(term) || x.Description.ToLower().Contains(term) || x.Objective.ToLower().Contains(term));

                    // TODO: Expand the accordions for those matching

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
