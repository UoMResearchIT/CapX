// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for API write operations that validate and mutate projects and project notes.
    /// </summary>
    public class ProjectWriteService
    {
        public const string FallbackAuthorUsername = "migration-import";
        private const string rtpRequired = "RTP is required and must be greater than zero";

        private readonly ProjectService projectService;
        private readonly SubTaskService subTaskService;
        private readonly NoteService noteService;
        private readonly FinancialReferenceService financialReferenceService;
        private readonly SettingsService settingsService;
        private readonly TaskWriteService taskWriteService;
        private readonly InnateCodeService innateCodeService;
        private readonly SchoolService schoolService;
        private readonly UserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectWriteService"/> class.
        /// </summary>
        /// <param name="projectService">Service used to manage projects.</param>
        /// <param name="subTaskService">Service used to manage project tasks.</param>
        /// <param name="noteService">Service used to manage notes.</param>
        /// <param name="financialReferenceService">Service used to load financial references.</param>
        /// <param name="settingsService">Service used to load calculation settings.</param>
        /// <param name="taskWriteService">Service whose assignment validation create-time resourcing shares.</param>
        /// <param name="innateCodeService">Service used to create timesheet activity codes under the UI's own rules.</param>
        /// <param name="schoolService">Service used to find schools.</param>
        /// <param name="userService">Service used to find users.</param>
        public ProjectWriteService(
            ProjectService projectService,
            SubTaskService subTaskService,
            NoteService noteService,
            FinancialReferenceService financialReferenceService,
            SettingsService settingsService,
            TaskWriteService taskWriteService,
            InnateCodeService innateCodeService,
            SchoolService schoolService,
            UserService userService)
        {
            this.projectService = projectService;
            this.subTaskService = subTaskService;
            this.noteService = noteService;
            this.financialReferenceService = financialReferenceService;
            this.settingsService = settingsService;
            this.taskWriteService = taskWriteService;
            this.innateCodeService = innateCodeService;
            this.schoolService = schoolService;
            this.userService = userService;
        }

        /// <summary>
        /// Validates a project creation request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming project creation request.</param>
        /// <param name="caller">Authenticated caller creating the project.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> Validate(PPMToolContext context, ImportProjectRequestDTO request, User caller)
        {
            var errors = new List<string>();

            if (caller.Person == null)
                errors.Add($"Caller '{caller.CASUserName}' has no linked Person -- required to set as the new Project's RequestOwner");

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            else if (projectService.DuplicateDetected(context, new Project { Name = request.Name }))
                errors.Add($"A Project named '{request.Name}' already exists");

            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (context.Projects.Any(p => p.RTP == request.RTP))
                errors.Add($"A Project with RTP {request.RTP} already exists");

            if (request.TimesheetActivityCode != null)
                errors.AddRange(ValidateTimesheetActivityCode(context, request.TimesheetActivityCode, request.Name, project: null));

            if (string.IsNullOrWhiteSpace(request.PI)) errors.Add("PI is required");
            if (string.IsNullOrWhiteSpace(request.RequestDocLink)) errors.Add("RequestDocLink is required");
            if (!GeneralHelpers.TryParseCostModel(request.CostModel, out var costModel))
                errors.Add($"CostModel '{request.CostModel}' is not a valid value");
            else if (costModel == CostModel.DayRate && request.DayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");
            if (!GeneralHelpers.TryParseDefined<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");
            if (request.ManagementEndDate.Date < request.ManagementStartDate.Date)
                errors.Add("ManagementEndDate must be on or after ManagementStartDate");

            if (string.IsNullOrWhiteSpace(request.SchoolCode) && request.SchoolId == null)
                errors.Add("SchoolCode or SchoolId is required");
            else
                ResolveSchool(context, request.SchoolId, request.SchoolCode, errors);

            if (!string.IsNullOrWhiteSpace(request.ProjectManagerUsername) && userService.GetByUsernameWithWorkloadModels(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = userService.GetByUsernameWithWorkloadModels(context, r.Username)?.Person;
                if (string.IsNullOrWhiteSpace(r.Username))
                    errors.Add("Each Resourcing entry needs a Username");
                else if (person == null)
                    errors.Add($"Resourcing username '{r.Username}' not found, or has no linked Person");
                assignments.Add((person, r.Username, r.AssignmentFTE));
            }
            errors.AddRange(taskWriteService.ValidateAssignments(context, assignments, Duty.ProjectWork, request.ManagementStartDate));

            if ((request.Comments?.Count ?? 0) > 0 && userService.GetByUsernameWithWorkloadModels(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it before adding comments");

            return errors;
        }

        /// <summary>
        /// Creates a project, related innate activity/task scaffolding, optional resourcing, and optional notes.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming project creation request.</param>
        /// <param name="caller">Authenticated caller creating the project.</param>
        /// <returns>Identifier payload for the created project and related record counts.</returns>
        public ImportProjectResponseDTO Create(PPMToolContext context, ImportProjectRequestDTO request, User caller)
        {
            var school = ResolveSchool(context, request.SchoolId, request.SchoolCode, new List<string>())!;
            var projectManager = string.IsNullOrWhiteSpace(request.ProjectManagerUsername)
                ? null
                : userService.GetByUsernameWithWorkloadModels(context, request.ProjectManagerUsername)?.Person;
            GeneralHelpers.TryParseCostModel(request.CostModel, out var costModel);

            var project = new Project
            {
                Name = request.Name,
                RTP = request.RTP,
                PI = request.PI,
                School = school,
                ProjectManager = projectManager,
                RequestOwner = caller.Person!,
                Budget = request.Budget,
                CostModel = costModel,
                DayRate = costModel == CostModel.DayRate ? request.DayRate : 0,
                ProjectStatus = Enum.Parse<ProjectStatus>(request.ProjectStatus),
                Description = request.Description,
                RequestDocLink = request.RequestDocLink,
                ScrumProjectLink = string.IsNullOrWhiteSpace(request.ScrumProjectLink) ? null : request.ScrumProjectLink,
            };
            var projectId = projectService.Add(context, project);
            if (projectId < 0)
                throw new InvalidOperationException($"ProjectService.Add returned {projectId} (duplicate name/RTP) despite passing Validate() -- possible race condition");

            if (request.TimesheetActivityCode != null)
            {
                project.InnateActivity = ResolveTimesheetActivityCode(context, request.TimesheetActivityCode, project.Name, currentCode: null, project.ProjectId);
                context.SaveChangesWithRetry();
            }

            var managementFte = settingsService.GetSetting(SettingType.TechnicalLeadershipDefaultFTE, 0.05f);
            var managementTask = new SubTask
            {
                Name = "Leadership",
                TaskDuty = Duty.ProjectAndServiceMgmt,
                TaskType = TaskType.FixedDuration,
                HasFixedStart = true,
                HasFixedEndDate = true,
                Demand = managementFte,
                OriginalDemand = managementFte,
                OwningProject = project,
                StartDate = GeneralHelpers.AsUnspecifiedKind(request.ManagementStartDate),
                EndDate = GeneralHelpers.AsUnspecifiedKind(request.ManagementEndDate),
            };
            managementTask.Schedule();
            subTaskService.Add(context, managementTask);

            var resourcesCreated = 0;
            var resourcing = request.Resourcing ?? Array.Empty<ImportResourcingDTO>();
            if (resourcing.Count > 0)
            {
                var totalFte = resourcing.Sum(r => r.AssignmentFTE);
                var delivery = new SubTask
                {
                    Name = "Delivery",
                    TaskDuty = Duty.ProjectWork,
                    TaskType = TaskType.FixedDuration,
                    HasFixedStart = true,
                    HasFixedEndDate = true,
                    Demand = totalFte,
                    OriginalDemand = totalFte,
                    OwningProject = project,
                    StartDate = GeneralHelpers.AsUnspecifiedKind(request.ManagementStartDate),
                    EndDate = GeneralHelpers.AsUnspecifiedKind(request.ManagementEndDate),
                };
                delivery.Schedule();
                subTaskService.Add(context, delivery);

                foreach (var r in resourcing)
                {
                    var person = userService.GetByUsernameWithWorkloadModels(context, r.Username)!.Person!;
                    var resource = new Resource
                    {
                        Person = person,
                        SubTask = delivery,
                        AssignmentFTE = r.AssignmentFTE,
                        IsProvisional = true,
                    };
                    context.Resources.Add(resource);
                    resourcesCreated++;
                }
                subTaskService.Reschedule(context, delivery);
                context.SaveChangesWithRetry();
            }

            var notesCreated = 0;
            var fallbackAuthor = userService.GetByUsernameWithWorkloadModels(context, FallbackAuthorUsername);
            foreach (var c in request.Comments ?? Array.Empty<ImportCommentDTO>())
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : userService.GetByUsernameWithWorkloadModels(context, c.AuthorUsername);
                author ??= fallbackAuthor!;

                var content = author.CASUserName == FallbackAuthorUsername
                    ? $"<p><em>Originally posted by {c.AuthorDisplayName} on Planner, {c.CreatedDate:yyyy-MM-dd}:</em></p>{c.ContentHtml}"
                    : c.ContentHtml;

                var createdDate = GeneralHelpers.AsUnspecifiedKind(c.CreatedDate);
                noteService.Add(context, new Note
                {
                    Project = project,
                    Author = author,
                    HtmlContent = content,
                    CreatedDate = createdDate,
                    EditedDate = createdDate,
                });
                notesCreated++;
            }

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new ImportProjectResponseDTO(project.ProjectId, resourcesCreated, notesCreated);
        }

        /// <summary>
        /// Validates a project update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming project update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateProjectUpdate(PPMToolContext context, UpdateProjectRequestDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
            {
                errors.Add(rtpRequired);
                return errors;
            }
            var project = projectService.GetByRTP(context, request.RTP);
            if (project == null)
            {
                errors.Add($"RTP {request.RTP} does not match any Project");
                return errors;
            }

            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    errors.Add("Name cannot be blank");
                else if (projectService.DuplicateDetected(context, new Project { ProjectId = project.ProjectId, Name = request.Name }))
                    errors.Add($"A different Project named '{request.Name}' already exists");
            }

            CostModel? costModel = null;
            if (request.CostModel != null)
            {
                if (!GeneralHelpers.TryParseCostModel(request.CostModel, out var parsed))
                    errors.Add($"CostModel '{request.CostModel}' is not a valid value");
                else
                    costModel = parsed;
            }
            var resolvedCostModel = costModel ?? project.CostModel;
            var resolvedDayRate = request.DayRate ?? project.DayRate;
            if (resolvedCostModel == CostModel.DayRate && resolvedDayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");

            if (request.ProjectStatus != null && !GeneralHelpers.TryParseDefined<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");

            if (request.SchoolCode != null || request.SchoolId != null)
                ResolveSchool(context, request.SchoolId, request.SchoolCode, errors);

            if (!string.IsNullOrEmpty(request.ProjectManagerUsername) && userService.GetByUsernameWithWorkloadModels(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            if (request.TimesheetActivityCode != null)
                errors.AddRange(ValidateTimesheetActivityCode(context, request.TimesheetActivityCode, request.Name ?? project.Name, project));

            return errors;
        }

        /// <summary>
        /// Updates an existing project.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming project update request.</param>
        /// <returns>Identifier payload for the updated project.</returns>
        public UpdateProjectResponseDTO UpdateProject(PPMToolContext context, UpdateProjectRequestDTO request)
        {
            var project = projectService.GetByRTP(context, request.RTP)!;
            var oldStatus = project.ProjectStatus;

            if (request.Name != null) project.Name = request.Name;
            if (request.PI != null) project.PI = request.PI;
            if (request.SchoolCode != null || request.SchoolId != null)
                project.School = ResolveSchool(context, request.SchoolId, request.SchoolCode, new List<string>())!;
            if (request.ProjectManagerUsername != null)
                project.ProjectManager = request.ProjectManagerUsername == ""
                    ? null
                    : userService.GetByUsernameWithWorkloadModels(context, request.ProjectManagerUsername)!.Person!;
            if (request.Budget.HasValue) project.Budget = request.Budget.Value;
            if (request.CostModel != null && GeneralHelpers.TryParseCostModel(request.CostModel, out var costModel)) project.CostModel = costModel;
            if (request.DayRate.HasValue) project.DayRate = request.DayRate.Value;
            if (project.CostModel != CostModel.DayRate) project.DayRate = 0;
            if (request.ProjectStatus != null) project.ProjectStatus = Enum.Parse<ProjectStatus>(request.ProjectStatus);
            if (request.Description != null) project.Description = request.Description;
            if (request.RequestDocLink != null) project.RequestDocLink = request.RequestDocLink;
            if (request.ScrumProjectLink != null) project.ScrumProjectLink = request.ScrumProjectLink == "" ? null : request.ScrumProjectLink;
            if (request.TimesheetActivityCode != null)
                project.InnateActivity = ResolveTimesheetActivityCode(context, request.TimesheetActivityCode, project.Name, project.InnateActivity, project.ProjectId);
            ApplyStatusSideEffects(project, oldStatus);

            var result = projectService.Update(context, project);
            if (result < 0)
                throw new InvalidOperationException($"ProjectService.Update returned {result} (duplicate) despite passing ValidateProjectUpdate() -- possible race condition");

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new UpdateProjectResponseDTO(project.ProjectId);
        }

        /// <summary>
        /// Validates a notes/add request for an existing project.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming notes/add request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateNotesAdd(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (projectService.GetByRTP(context, request.RTP) == null)
                errors.Add($"RTP {request.RTP} does not match any Project");

            var comments = request.Comments ?? Array.Empty<ImportCommentDTO>();
            if (comments.Count == 0)
                errors.Add("Comments must contain at least one entry");

            if (comments.Count > 0 && userService.GetByUsernameWithWorkloadModels(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it (POST /api/users/add) before adding comments");

            return errors;
        }

        /// <summary>
        /// Adds notes to an existing project.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming notes/add request.</param>
        /// <returns>Identifier payload for the project and number of created notes.</returns>
        public ImportNotesResponseDTO AddNotes(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var project = projectService.GetByRTP(context, request.RTP)!;
            var fallbackAuthor = userService.GetByUsernameWithWorkloadModels(context, FallbackAuthorUsername);

            var notesCreated = 0;
            foreach (var c in request.Comments)
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : userService.GetByUsernameWithWorkloadModels(context, c.AuthorUsername);
                author ??= fallbackAuthor!;

                var content = author.CASUserName == FallbackAuthorUsername
                    ? $"<p><em>Originally posted by {c.AuthorDisplayName} on Planner, {c.CreatedDate:yyyy-MM-dd}:</em></p>{c.ContentHtml}"
                    : c.ContentHtml;

                var createdDate = GeneralHelpers.AsUnspecifiedKind(c.CreatedDate);
                noteService.Add(context, new Note
                {
                    Project = project,
                    Author = author,
                    HtmlContent = content,
                    CreatedDate = createdDate,
                    EditedDate = createdDate,
                });
                notesCreated++;
            }
            context.SaveChangesWithRetry();

            return new ImportNotesResponseDTO(project.ProjectId, notesCreated);
        }

        /// <summary>
        /// Validates a notes-by-RTP lookup request.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateNotesGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (projectService.GetByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// Gets notes for a project identified by RTP.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Notes for the project.</returns>
        public List<NoteDTO> GetNotesForRTP(PPMToolContext context, int rtp)
        {
            var project = projectService.GetByRTP(context, rtp)!;

            var notes = context.Notes
                .Where(n => n.Project.ProjectId == project.ProjectId)
                .Include(n => n.Author).ThenInclude(a => a!.Person)
                .OrderBy(n => n.CreatedDate)
                .ToList();

            return notes.Select(n => new NoteDTO(
                n.NoteId, rtp, n.Author.CASUserName, n.Author.GetName(),
                n.HtmlContent, n.CreatedDate, n.EditedDate
            )).ToList();
        }

        /// <summary>
        /// Validates a note update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming note update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateNoteUpdate(PPMToolContext context, UpdateNoteRequestDTO request)
        {
            var errors = new List<string>();
            if (!context.Notes.Any(n => n.NoteId == request.NoteId))
                errors.Add($"NoteId {request.NoteId} does not exist");
            if (string.IsNullOrWhiteSpace(request.HtmlContent))
                errors.Add("HtmlContent is required");
            return errors;
        }

        /// <summary>
        /// Updates an existing note.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming note update request.</param>
        /// <param name="caller">Authenticated caller performing the update.</param>
        /// <returns>Identifier payload for the updated note.</returns>
        public UpdateNoteResponseDTO UpdateNote(PPMToolContext context, UpdateNoteRequestDTO request, User caller)
        {
            var note = context.Notes.First(n => n.NoteId == request.NoteId);
            note.HtmlContent = request.HtmlContent;
            note.Editor = caller;
            // Local time, as the UI stamps an edited Note (ProjectDetails.razor.cs).
            note.EditedDate = GeneralHelpers.AsUnspecifiedKind(DateTime.Now);
            noteService.Update(context, note);
            context.SaveChangesWithRetry();

            return new UpdateNoteResponseDTO(note.NoteId);
        }

        /// <summary>
        /// Finds the active School a request names by <paramref name="schoolId"/>, by
        /// <paramref name="schoolCode"/>, or by both, which must then agree.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="schoolId">School id from the request, if any.</param>
        /// <param name="schoolCode">School code from the request, if any.</param>
        /// <param name="errors">Receives an error for each problem found.</param>
        /// <returns>The School, or <c>null</c> when an error was added.</returns>
        private School? ResolveSchool(PPMToolContext context, int? schoolId, string? schoolCode, List<string> errors)
        {
            School? byId = null, byCode = null;
            if (schoolId != null && (byId = schoolService.GetById(context, schoolId.Value, activeOnly: true)) == null)
                errors.Add($"SchoolId {schoolId} does not match any active School");
            if (!string.IsNullOrWhiteSpace(schoolCode) && (byCode = schoolService.GetByCode(context, schoolCode, activeOnly: true)) == null)
                errors.Add($"SchoolCode '{schoolCode}' does not match any active School");
            if (byId != null && byCode != null && byId.SchoolId != byCode.SchoolId)
            {
                errors.Add($"SchoolId {schoolId} and SchoolCode '{schoolCode}' name different Schools");
                return null;
            }
            return byId ?? byCode;
        }

        /// <summary>
        /// Applies what saving a project in <see cref="PPMTool.Pages.AddProject"/> does around its status, so a
        /// status changed through the API has the same consequences as one changed in the UI:
        /// leaving New Request stops the request-duration clock; a cancelled project loses its
        /// assignments; a finished or cancelled one loses its followers; and becoming Active
        /// (other than from Paused) restarts the actuals clock.
        /// </summary>
        /// <param name="project">The project, with its new status already set.</param>
        /// <param name="oldStatus">The status it had before this update.</param>
        private static void ApplyStatusSideEffects(Project project, ProjectStatus oldStatus)
        {
            var newStatus = project.ProjectStatus;
            if (oldStatus != newStatus && oldStatus == ProjectStatus.NewRequest)
                project.RequestCompletedDate = GeneralHelpers.AsUnspecifiedKind(DateTime.Today);

            // Resources are kept on finished projects as a historical record, as in the UI.
            if (newStatus.IsCancelled())
            {
                foreach (var task in project.SubTasks)
                {
                    task.AssignedResources.Clear();
                    task.UpdateUnmetDemand(task.AssignedResources);
                }
            }

            if (newStatus.IsFinishedOrCancelled())
                project.Followers.Clear();

            if (oldStatus != newStatus && oldStatus != ProjectStatus.Paused && newStatus == ProjectStatus.Active)
                project.ActualsLastUpdated = DateTime.Now.ToString("R");
        }

        /// <summary>
        /// Validates a caller-supplied timesheet activity code for a project. An existing
        /// code may be linked only if no other project uses it. A project's current code
        /// can be replaced only while it has no timesheet entries, since the project's
        /// actuals come through that code. A new code must not duplicate another code's
        /// name, the same rule the UI applies.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="activityCode">Activity code supplied by the caller.</param>
        /// <param name="projectName">Name the project will have, used to name a new code.</param>
        /// <param name="project">The project being updated, or <c>null</c> for a new project.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        private List<string> ValidateTimesheetActivityCode(PPMToolContext context, string activityCode, string? projectName, Project? project)
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(activityCode))
            {
                errors.Add("TimesheetActivityCode cannot be blank");
                return errors;
            }

            var requested = innateCodeService.GetByActivityCode(context, activityCode);
            var current = project?.InnateActivity;
            if (current != null && requested?.InnateCodeId == current.InnateCodeId)
                return errors;

            if (current != null)
            {
                var entries = context.TimesheetEntries.Count(e => e.InnateCodeTask.InnateCode.InnateCodeId == current.InnateCodeId);
                if (entries > 0)
                {
                    errors.Add($"Timesheet code '{current.ActivityCode}' has {entries} timesheet {(entries == 1 ? "entry" : "entries")}, so replacing it would hide them from this Project's actuals");
                    return errors;
                }
            }

            if (requested != null)
            {
                var linkedRtp = context.Projects
                    .Where(p => p.ProjectId != (project == null ? 0 : project.ProjectId) && p.InnateActivity != null && p.InnateActivity.InnateCodeId == requested.InnateCodeId)
                    .Select(p => (int?)p.RTP)
                    .FirstOrDefault();
                if (linkedRtp != null)
                    errors.Add($"Timesheet code '{requested.ActivityCode}' already belongs to the Project with RTP {linkedRtp}");
            }
            else if (current == null || LinkedToAnotherProject(context, current, project!.ProjectId))
            {
                if (innateCodeService.DuplicateDetected(context, NewTimesheetActivityCode(activityCode, projectName ?? "")))
                    errors.Add($"A timesheet code named '{projectName}' already exists, so a new code '{activityCode.Trim()}' for this Project would duplicate it");
            }

            return errors;
        }

        /// <summary>
        /// Returns the timesheet activity code a project should be linked to, as validated by
        /// <see cref="ValidateTimesheetActivityCode"/>: an existing code, unchanged; the project's
        /// current code renamed in place, when it is unused and no other project shares it; or a
        /// new code named after the project, with the default tasks.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="activityCode">Activity code supplied by the caller.</param>
        /// <param name="projectName">Project name, used to name a new code.</param>
        /// <param name="currentCode">The project's current code, or <c>null</c>.</param>
        /// <param name="projectId">The project being linked.</param>
        /// <returns>The code to link.</returns>
        private InnateCode ResolveTimesheetActivityCode(PPMToolContext context, string activityCode, string projectName, InnateCode? currentCode, int projectId)
        {
            var requested = innateCodeService.GetByActivityCode(context, activityCode);
            if (requested != null)
                return requested;

            if (currentCode != null && !LinkedToAnotherProject(context, currentCode, projectId))
            {
                currentCode.ActivityCode = activityCode.Trim();
                return currentCode;
            }

            var created = NewTimesheetActivityCode(activityCode, projectName);
            if (innateCodeService.Add(context, created, commitChanges: false) < 0)
                throw new InvalidOperationException($"InnateCodeService.Add refused timesheet code '{created.ActivityCode}' despite passing validation");
            return created;
        }

        /// <summary>
        /// Whether a timesheet activity code is linked to any project other than the given one.
        /// </summary>
        private static bool LinkedToAnotherProject(PPMToolContext context, InnateCode code, int projectId) =>
            context.Projects.Any(p => p.ProjectId != projectId && p.InnateActivity != null && p.InnateActivity.InnateCodeId == code.InnateCodeId);

        /// <summary>
        /// A new, unsaved timesheet activity code named after a project, with the default tasks.
        /// </summary>
        private static InnateCode NewTimesheetActivityCode(string activityCode, string projectName) => new()
        {
            ActivityCode = activityCode.Trim(),
            ActivityName = projectName,
            IsActive = true,
            Tasks = DefaultInnateCodeTasks(),
        };

        /// <summary>
        /// The tasks a timesheet code created by projects/add or projects/update starts with.
        /// </summary>
        /// <returns>Default innate-code task templates.</returns>
        private static List<InnateCodeTask> DefaultInnateCodeTasks() => new()
        {
            new() { TaskName = "Development", Duty = Duty.ProjectWork },
            new() { TaskName = "Management", Duty = Duty.ProjectAndServiceMgmt },
            new() { TaskName = "Maintenance", Duty = Duty.ProjectWork }
        };
    }
}
