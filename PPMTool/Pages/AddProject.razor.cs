// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Configuration;
using PPMTool.Data;
using PPMTool.Data.Entities;
using PPMTool.Enums;
using PPMTool.Services;
using Radzen;

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

        protected override void OnInitialized()
        {
            base.OnInitialized();

            if (ProjectId > 0)
            {
                projectModel = ProjectService.GetById(Context, ProjectId);

                // If editing a project, only allow the project manager to edit it or a superuser
                var role = RolesService.GetByUsername(Context, ActiveUserName);
                EditAuthorised = ActiveUserRoleType == RoleType.Superuser || projectModel.ProjectManager.PersonId == ActiveUser?.PersonId;

                // Populate school list
                schools = DropdownHelper.GetSchoolsForFaculty(projectModel.Faculty);
            }
            else
            {
                projectModel.DayRate = double.Parse(Configuration["DefaultDayRate"]);

                // Auto generate the RTP number based on the highest in the DB
                projectModel.RTP = ProjectService.GetAll(Context).Select(x => x.RTP).DefaultIfEmpty(0).Max() + 1;

                // Set the active user as the PM by default
                projectModel.ProjectManager = RolesService.GetByUsername(Context, ActiveUserName)?.Person;
            }

            // Initially load data
            innateActivityQuery = InnateCodeService.GetAll(Context).AsQueryable();
            innateActivities = innateActivityQuery.ToList();
            faculties = Enum.GetValues<Faculty>().ToList();
            statuses = Enum.GetValues<ProjectStatus>().ToList();
            var people = PersonService.GetAll(Context).OrderBy(x => x.Name).ToList();
            var roles = RolesService.GetAll(Context)
                .Where(x =>
                    (x.RoleType == RoleType.Manager || x.RoleType == RoleType.Superuser)
                    && x.Person != null
                );
            projectManagers = people.Where(x => roles.Any(y => y.Person.PersonId == x.PersonId)).ToList();

            // Create edit context and message store
            editContext = new EditContext(projectModel);
            messageStore = new(editContext);

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

        private void OnProjectManagerChosen(object value)
        {
            Person pm = value as Person;

            // If the PM is not null and is not the current user then warn of loss of access if not superuser
            var role = RolesService.GetByUsername(Context, ActiveUserName);
            if (pm != null && pm.PersonId != role?.Person?.PersonId && role.RoleType != RoleType.Superuser)
            {
                DialogService.Alert("By changing the project manager of this project to someone other than you, you will lose edit access to the project on saving.", "Warning!", new AlertOptions() { OkButtonText = "OK" });
            }
        }

        private void HandleSubmit()
        {
            // Form valid?
            messageStore.Clear();
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
                            foreach (SubTask t in projectModel.SubTasks)
                            {
                                t.AssignedResources.Clear();
                            }
                        }

                        // If the project is marked as cancelled or finished then remove the followers
                        if (projectModel.ProjectStatus.IsFinishedOrCancelled())
                        {

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
                        if (!CheckResultOfAddOrUpdate(res)) return;
                    }
                    else
                    {
                        LogInformation("Adding new project...");
                        var res = ProjectService.Add(Context, projectModel);
                        if (!CheckResultOfAddOrUpdate(res)) return;

                        // Make sure that super users automatically follow the project
                        var superusers = RolesService.GetAll(Context).Where(x => x.RoleType == RoleType.Superuser).Select(x => x.Person);
                        foreach (var s in superusers)
                        {
                            if (s == null) throw new InvalidOperationException("Superuser role found without a person attached to it!");

                            if (projectModel.ProjectManager != s && !projectModel.Followers.Contains(s))
                            {
                                projectModel.Followers.Add(s);
                            }
                        }
                        ProjectService.Update(Context, projectModel);
                    }
                }

                NavigatePostSubmit();
            }

            // Form invalid
            else
            {
                if (discardChanges)
                {
                    LogInformation($"Discarding project changes!");
                    NavigatePostSubmit();
                }
            }
        }

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

        private bool CheckProjectManagerSet()
        {
            if (projectModel.ProjectManager == null)
            {
                messageStore.Add(() => projectModel.ProjectManager, "Project must have a project manager set!");
                return false;
            }
            return true;
        }

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
