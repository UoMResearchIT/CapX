using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
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

        private IEnumerable<Person> people;
        private IEnumerable<Competency> competencies;
        private bool userIsSuperuser;
        private int activeUserId;
        private Person selectedPerson = null;
        private byte[] file;
        private string fileName;
        private long? fileSize;

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
            people = PersonService.GetAll(context).Where(x => x.IsCurrentStaff()).OrderBy(x => x.Name);
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
            StateHasChanged();
        }

        private void UpdateAssessment(CompetencyAssessment assessment)
        {
            CompetencyService.UpdateAssessment(context, assessment);
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

            if (value != null) LogInformation($"File Uploaded - adding competency assessments...");

            Task.Run(() =>
            {
                try
                {
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
                    foreach (var line in lines)
                    {
                        // Split line
                        var values = Clean(line).Split("\t");

                        // TODO: If the value is of the pattern 1.1 then store as this is the first two digits of the legacy ID

                        // TODO: If the value is of the pattern 1. then append as this completed the legacy ID

                        // TODO: If the value is of the pattern a. then append as this completes the legacy ID

                        // TODO: If there is an "x" or "X" then this represents a selection and can infer a status

                        // TODO: Cross check line against competencies to see if LegacyId matches

                        // TODO: Check the existing assessments to see if this represents a change from the latest

                        // TODO: Add assessment to DB


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
            return line.Replace("&nbsp;", " ").Replace("\"", "");
        }
    }
}
