// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;
using PPMTool.Services;
using Radzen;
using static PPMTool.Data.StatusMessage;

namespace PPMTool.Pages
{
    [Authorize(Roles = "Manager,Superuser")]
    public partial class AddProject : BasePage
    {
        [Inject]
        private ProjectService ProjectService { get; set; }

        [Inject]
        private SubTaskService SubTaskService { get; set; }

        [Inject]
        private PersonService PersonService { get; set; }

        [Inject]
        private InnateCodeService InnateCodeService { get; set; }

        [Inject]
        private IConfiguration Configuration { get; set; }

        [Inject]
        private DialogService DialogService { get; set; }

        [Inject]
        private FinancialReferenceService FinancialReferenceService { get; set; }

        [Inject]
        private PaymentService PaymentService { get; set; }

        [Inject]
        private FundingSourceService FundingSourceService { get; set; } = null!;

        [Inject]
        private FacultyService FacultyService { get; set; }

        [Inject]
        private SchoolService SchoolService { get; set; }

        [Parameter]
        public int ProjectId { get; set; }

        private Project projectModel = new Project();
        private bool gotoDetails = false;
        private bool discardChanges = true;
        private bool showOrgUnitsRequiredWarning = false;
        private IEnumerable<InnateCode> innateActivities = new List<InnateCode>();
        private IQueryable<InnateCode> innateActivityQuery;
        private IEnumerable<Person> projectManagers = new List<Person>();
        private IEnumerable<Faculty> faculties = new List<Faculty>();
        private IEnumerable<School> schools = new List<School>();
        private IEnumerable<ProjectStatus> statuses = new List<ProjectStatus>();
        private IEnumerable<CostModel> costModels = new List<CostModel>();
        private ValidationMessageStore messageStore;
        private EditContext editContext;
        private double fundsReceived;
        private Faculty chosenFaculty;
        private IEnumerable<FundingSource> availableFundingSources = new List<FundingSource>();

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (!firstRender) return;

            if (ProjectId > 0)
            {
                projectModel = ProjectService.GetById(Context, ProjectId);

                // Populate faculty
                chosenFaculty = projectModel.School?.Faculty;

                // Get funds received
                fundsReceived = PaymentService.GetFundsReceived(Context, projectModel?.ProjectId ?? 0);

                // If editing a project, only allow the project manager to edit it or a superuser
                EditAuthorised = ActiveUserRoleType == RoleType.Superuser || projectModel.ProjectManager.PersonId == ActiveUser?.Person?.PersonId;

                // Populate school list
                schools = SchoolService.GetSchoolsForFaculty(Context, projectModel.School.Faculty.FacultyId);

                // Populate funding source list
                availableFundingSources = FundingSourceService.GetFundingSources(Context, ProjectId);
            }
            else
            {
                projectModel.DayRate = GetSetting(SettingType.DayRateDefault, 0f);

                // Auto generate the RTP number based on the highest in the DB
                projectModel.RTP = ProjectService.GetAll(Context).Select(x => x.RTP).DefaultIfEmpty(0).Max() + 1;

                // Set the active user as the PM by default
                projectModel.ProjectManager = ActiveUser?.Person;

                // Specific check for when Finance feature has not been enabled and a new
                // project is being added, as Faculties/Schools are required
                if (!FeatureService.IsFeatureEnabled(FeatureType.ProjectFinance))
                {
                    // If we have any active Schools then it means we have active Faculties too
                    showOrgUnitsRequiredWarning = !SchoolService.GetAllActive(Context).Any();
                }

                // Set the selected school to null or the dropdown placeholder won't work
                projectModel.School = null;
            }

            // Add default buttons with handlers
            SetDefaultActionBar(
                () => { gotoDetails = true; discardChanges = false; HandleSubmit(); },
                () => { gotoDetails = ProjectId > 0; discardChanges = true; HandleSubmit(); }
            );

            // Initially load data
            innateActivityQuery = InnateCodeService.GetAll(Context).AsQueryable();
            innateActivities = innateActivityQuery.ToList();
            faculties = FacultyService.GetAllActive(Context).ToList();
            statuses = Enum.GetValues<ProjectStatus>().ToList();
            var people = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();
            var users = UserService.GetAll(Context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            projectManagers = people.Where(x => users.Any(y => y.Person.PersonId == x.PersonId)).ToList();
            costModels = DisplayOrderHelper.GetOrderListOfCostModels();

            // Create edit context and message store
            editContext = new EditContext(projectModel);
            messageStore = new(editContext);

            Loading = false;
            StateHasChanged();
            LogInformation(projectModel.ProjectId > 0 ? $"Editing project {projectModel?.GetSensibleObjectName()}" : $"Adding new project");
        }

        /// <summary>
        /// Should be fired when the dropdown control initialises and when the filter condition changes.
        /// </summary>
        /// <param name="args"></param>
        void LoadInnateDropdownData(LoadDataArgs args)
        {
            var temp = innateActivityQuery;
            if (!string.IsNullOrEmpty(args.Filter))
            {
                Debug.WriteLine($"** Filtering Innate Code on {args.Filter}");
                temp = temp.Where(act => act.ActivityName.ToLower().Contains(args.Filter.ToLower()) || act.ActivityCode.ToLower().Contains(args.Filter.ToLower()));
                Debug.WriteLine($"** Innate code filter list contains {temp.Count()} items");
            }

            innateActivities = temp.ToList();

            InvokeAsync(StateHasChanged);
        }

        /// <summary>
        /// Helper method to call the nice string method of the enum
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string GetNiceString(Enum x)
        {
            return x.ToNiceString();
        }

        /// <summary>
        /// Loads the dropdown data for the schools based on the chosen faculty
        /// </summary>
        /// <param name="value"></param>
        private void OnFacultyChosen(object value)
        {
            Faculty faculty = value as Faculty;
            if (faculty != null)
            {
                schools = SchoolService.GetSchoolsForFaculty(Context, faculty.FacultyId);

                // Reset the school so the placeholder shows again
                projectModel.School = null;
            }
        }

        /// <summary>
        /// Callback after a PM is chosen in the dropdown
        /// </summary>
        /// <param name="value"></param>
        private void OnProjectManagerChosen(object value)
        {
            Person pm = value as Person;

            // If the PM is not null and is not the current user then warn of loss of access if not superuser
            if (pm != null && pm.PersonId != ActiveUser?.Person?.PersonId && ActiveUser.RoleType != RoleType.Superuser)
            {
                DialogService.Alert("By changing the project manager of this project to someone other than you, you will lose edit access to the project on saving.", "Warning!", new AlertOptions() { OkButtonText = "OK" });
            }
        }

        /// <summary>
        /// Fired when the save button is clicked
        /// </summary>
        private void HandleSubmit()
        {
            // Clear the action bar messages
            ClearErrorMessage();

            // Clear any manual messages
            messageStore.Clear();
            editContext.NotifyValidationStateChanged();

            // Validate the form again
            if (editContext.Validate())
            {
                if (!discardChanges)
                {
                    // Further validation not picked up by model annotations
                    if (!CheckProjectManagerSet() || !CheckSchoolAndFacultySet())
                    {
                        UpdateErrorOnActionBarFromContextMessageStore();
                        return;
                    }

                    // Update the project summary values
                    var finrefs = FinancialReferenceService.GetAllOrDefault(Context);
                    var bauTopSlicePercentage = GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
                    projectModel.UpdateProjectMetaData(true, finrefs, bauTopSlicePercentage);

                    if (ProjectId > 0)
                    {
                        // Check to see if the project is marked as cancelled as then we need to remove resources.
                        // Leave resources on completed projects so we have a historical record.
                        if (projectModel.ProjectStatus.IsCancelled())
                        {
                            Logger.LogInformation("Removing resources as cancelled!");
                            foreach (SubTask t in projectModel.SubTasks)
                            {
                                t.AssignedResources.Clear();
                            }
                        }

                        // If the project is marked as cancelled or finished then remove the followers
                        if (projectModel.ProjectStatus.IsFinishedOrCancelled())
                        {
                            Logger.LogInformation("Removing followers as finished or cancelled!");
                            projectModel.Followers.Clear();
                        }

                        // Check if the status is changing
                        var oldStatus = ProjectService.GetOldStatus(Context, projectModel);
                        if (oldStatus != projectModel.ProjectStatus)
                        {
                            LogInformation($"Project status change: {oldStatus} -> {projectModel.ProjectStatus}");

                            // Set the actuals last updated if changed status to active from anything other than paused
                            if (oldStatus != ProjectStatus.Paused && projectModel.ProjectStatus == ProjectStatus.Active)
                            {
                                projectModel.ActualsLastUpdated = DateTime.Now.ToString("R");
                            }
                        }

                        LogInformation($"Saving project {projectModel?.GetSensibleObjectName()}...");
                        var res = ProjectService.Update(Context, projectModel);
                        CheckResultOfAddOrUpdate(res);
                    }
                    else
                    {
                        LogInformation("Adding new project...");
                        var res = ProjectService.Add(Context, projectModel);
                        CheckResultOfAddOrUpdate(res);
                    }
                }

                // Only navigate away if no validation failures at DB add/update
                if (!editContext.GetValidationMessages().Any())
                {
                    NavigatePostSubmit();
                }
            }

            // Form invalid
            else
            {
                if (discardChanges)
                {
                    LogInformation($"Discarding project changes!");
                    NavigatePostSubmit();
                    return;
                }
            }

            // Update with errors
            UpdateErrorOnActionBarFromContextMessageStore();
        }

        /// <summary>
        /// Method to set the error message on the action bar from the edit context
        /// </summary>
        private void UpdateErrorOnActionBarFromContextMessageStore()
        {
            // Set error messages based on the message store
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
            }
            else
            {
                ClearErrorMessage();
            }
        }

        /// <summary>
        /// Checks the results of a DB add or update and adds message to message store
        /// </summary>
        /// <param name="res"></param>
        /// <returns></returns>
        private bool CheckResultOfAddOrUpdate(int res)
        {
            if (res < 0)
            {
                // Duplicate found so show error message
                LogWarning($"Duplicate project found with {(res == -1 ? $"name {projectModel?.Name}" : $"{GetSetting(SettingType.ProjectAbbreviation)}-{projectModel?.RTP}")}!");
                if (res == -1)
                {
                    messageStore.Add(() => projectModel.Name, "Duplicate project name found!");
                }
                else
                {
                    messageStore.Add(() => projectModel.RTP, $"Duplicate {GetSetting(SettingType.ProjectAbbreviation)} number found!");
                }
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the PM is set
        /// </summary>
        /// <returns></returns>
        private bool CheckProjectManagerSet()
        {
            if (projectModel.ProjectManager == null)
            {
                messageStore.Add(() => projectModel.ProjectManager, "Project must have a project manager set!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check the faculty and school are set
        /// </summary>
        /// <returns></returns>
        private bool CheckSchoolAndFacultySet()
        {
            // Not faculty or school objects or placeholder objects
            if (projectModel.School == null || projectModel.School?.Faculty == null || projectModel.School?.SchoolId == 0 || projectModel.School?.Faculty?.FacultyId == 0)
            {
                messageStore.Add(() => projectModel.School, $"Project must have a {GetSetting(SettingType.OrgUnitUpper)} and {GetSetting(SettingType.OrgUnitLower)} set!");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Perform navigation to the appropriate page depending on where the user cam from
        /// </summary>
        private void NavigatePostSubmit()
        {
            if (gotoDetails)
            {
                Navigation.NavigateTo($"projects/projectdetails/{projectModel.ProjectId}");
            }
            else
            {
                Navigation.NavigateTo("projects");
            }
        }

        /// <summary>
        /// Delete the project from the DB after confirmation dialog
        /// </summary>
        private async void DeleteProject()
        {
            if (ProjectId > 0)
            {
                // Prompt
                bool confirmed = await DialogService.Confirm($"You are about to delete project {ProjectService.GetFullName(projectModel)}. " +
                    $"If this project was cancelled or didn't get funded then do not delete it but change its status instead so we can keep a record of unfunded projects.",
                    "Delete Project") ?? false;
                if (confirmed)
                {
                    // Delete all the subtasks for the project
                    var numToDelete = projectModel.SubTasks.Count;
                    for (int i = 0; i < numToDelete; ++i)
                    {
                        if (projectModel.SubTasks.Count > 0)
                        {
                            LogInformation($"Deleting subtask ID {projectModel.SubTasks.First()?.SubTaskId}");

                            SubTaskService.Delete(Context, projectModel.SubTasks.First());
                        }
                    }

                    LogInformation($"Deleting project {projectModel.GetSensibleObjectName()}, ID {projectModel.ProjectId}");

                    // Delete the project from the database
                    ProjectService.Delete(Context, projectModel);

                    // Navigate back to the projects list
                    Navigation.NavigateTo("projects");
                }
            }
        }
    }
}
