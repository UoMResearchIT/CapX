// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Data.Helpers;
using PPMTool.Enums;
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

        [Parameter]
        public int ProjectId { get; set; }

        private Project projectModel = new Project();
        private bool gotoDetails = false;
        private bool discardChanges = true;
        private IEnumerable<InnateCode> innateActivities = new List<InnateCode>();
        private IQueryable<InnateCode> innateActivityQuery;
        private IEnumerable<Person> projectManagers = new List<Person>();
        private IEnumerable<Faculty> faculties = new List<Faculty>();
        private IEnumerable<School> schools = new List<School>();
        private IEnumerable<ProjectStatus> statuses = new List<ProjectStatus>();
        private ValidationMessageStore messageStore;
        private EditContext editContext;
        private double fundsReceived;
        private IEnumerable<FundingSource> availableFundingSources = new List<FundingSource>();

        protected override void OnAfterRender(bool firstRender)
        {
            base.OnAfterRender(firstRender);

            if (!firstRender) return;

            if (ProjectId > 0)
            {
                projectModel = ProjectService.GetById(Context, ProjectId);

                // Get funds received
                fundsReceived = PaymentService.GetFundsReceived(Context, projectModel?.ProjectId ?? 0);

                // If editing a project, only allow the project manager to edit it or a superuser
                EditAuthorised = ActiveUserRoleType == RoleType.Superuser || projectModel.ProjectManager.PersonId == ActiveUser?.Person?.PersonId;

                // Populate school list
                schools = DropdownHelper.GetSchoolsForFaculty(projectModel.Faculty);

                // Populate funding source list
                availableFundingSources = FundingSourceService.GetFundingSources(Context, ProjectId);
            }
            else
            {
                projectModel.DayRate = GlobalDefaults.DayRateDefault;

                // Auto generate the RTP number based on the highest in the DB
                projectModel.RTP = ProjectService.GetAll(Context).Select(x => x.RTP).DefaultIfEmpty(0).Max() + 1;

                // Set the active user as the PM by default
                projectModel.ProjectManager = ActiveUser?.Person;
            }

            // Add default buttons with handlers
            SetDefaultActionBar(
                () => { gotoDetails = true; discardChanges = false; HandleSubmit(); },
                () => { gotoDetails = ProjectId > 0; discardChanges = true; HandleSubmit(); }
            );

            // Initially load data
            innateActivityQuery = InnateCodeService.GetAll(Context).AsQueryable();
            innateActivities = innateActivityQuery.ToList();
            faculties = Enum.GetValues<Faculty>().ToList();
            statuses = Enum.GetValues<ProjectStatus>().ToList();
            var people = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();
            var users = UserService.GetAll(Context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            projectManagers = people.Where(x => users.Any(y => y.Person.PersonId == x.PersonId)).ToList();

            // Create edit context and message store
            editContext = new EditContext(projectModel);
            messageStore = new(editContext);

            Loading = false;
            StateHasChanged();
            LogInformation(projectModel.ProjectId > 0 ? $"Editing project {projectModel?.GetFullName()}" : $"Adding new project");
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
            Faculty? faculty = value as Faculty?;
            if (faculty != null)
            {
                schools = DropdownHelper.GetSchoolsForFaculty(faculty ?? Faculty.Internal);
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

        private void HandleSubmit()
        {
            // Form valid?
            ClearErrorMessage();
            if (editContext.Validate())
            {
                if (!discardChanges)
                {
                    // Further validation
                    if (!CheckProjectManagerSet()) return;

                    // Update the project summary values
                    var finrefs = FinancialReferenceService.GetAll(Context);
                    projectModel.UpdateProjectMetaData(true, finrefs);

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

                        // Set the actuals last updated if changed status to active from anything other than paused
                        var oldStatus = ProjectService.GetOldStatus(Context, projectModel);
                        if (oldStatus != projectModel.ProjectStatus)
                        {
                            LogInformation($"Project status change: {oldStatus} -> {projectModel.ProjectStatus}");
                        }
                        if (oldStatus != ProjectStatus.Paused && projectModel.ProjectStatus == ProjectStatus.Active)
                        {
                            projectModel.ActualsLastUpdated = DateTime.Now.ToString("R");
                        }

                        LogInformation($"Saving project {projectModel?.GetFullName()}...");
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

            // Set error messages based on the message store
            var messages = editContext.GetValidationMessages();
            if (messages.Any())
            {
                SetErrorMessage(new StatusMessage(messages.First(), MessageType.Error));
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
                LogWarning($"Duplicate project found with {(res == -1 ? $"name {projectModel?.Name}" : $"RTP-{projectModel?.RTP}")}!");
                if (res == -1)
                {
                    messageStore.Add(() => projectModel.Name, "Duplicate project name found!");
                }
                else
                {
                    messageStore.Add(() => projectModel.RTP, "Duplicate RTP number found!");
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
                bool confirmed = await DialogService.Confirm($"You are about to delete project {projectModel.GetFullName()}. " +
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

                    LogInformation($"Deleting project {projectModel.GetFullName()}, ID {projectModel.ProjectId}");

                    // Delete the project from the database
                    ProjectService.Delete(Context, projectModel);

                    // Navigate back to the projects list
                    Navigation.NavigateTo("projects");
                }
            }
        }
    }
}
