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
    /// Service for API import operations that validate and mutate projects and project notes.
    /// </summary>
    public class ImportProjectService
    {
        public const string FallbackAuthorUsername = "migration-import";
        private const string rtpRequired = "RTP is required and must be greater than zero";

        private readonly ProjectService projectService;
        private readonly SubTaskService subTaskService;
        private readonly NoteService noteService;
        private readonly FinancialReferenceService financialReferenceService;
        private readonly SettingsService settingsService;
        private readonly UserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportProjectService"/> class.
        /// </summary>
        /// <param name="projectService">Service used to manage projects.</param>
        /// <param name="subTaskService">Service used to manage project tasks.</param>
        /// <param name="noteService">Service used to manage notes.</param>
        /// <param name="financialReferenceService">Service used to load financial references.</param>
        /// <param name="settingsService">Service used to load calculation settings.</param>
        /// <param name="userService">Service used to resolve users and manager information.</param>
        public ImportProjectService(
            ProjectService projectService,
            SubTaskService subTaskService,
            NoteService noteService,
            FinancialReferenceService financialReferenceService,
            SettingsService settingsService,
            UserService userService)
        {
            this.projectService = projectService;
            this.subTaskService = subTaskService;
            this.noteService = noteService;
            this.financialReferenceService = financialReferenceService;
            this.settingsService = settingsService;
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
                errors.Add($"Caller '{caller.CASUserName}' has no linked Person -- required to set as the imported Project's RequestOwner");

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            else if (projectService.DuplicateDetected(context, new Project { Name = request.Name }))
                errors.Add($"A Project named '{request.Name}' already exists");

            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (context.Projects.Any(p => p.RTP == request.RTP))
                errors.Add($"A Project with RTP {request.RTP} already exists");
            else
            {
                var existingCode = ImportHelper.FindInnateCodeForRTP(context, request.RTP);
                if (existingCode != null)
                {
                    var linkedRtp = context.Projects
                        .Where(p => p.InnateActivity != null && p.InnateActivity.InnateCodeId == existingCode.InnateCodeId)
                        .Select(p => (int?)p.RTP)
                        .FirstOrDefault();
                    if (linkedRtp != null)
                        errors.Add($"Timesheet code '{existingCode.ActivityCode}' already belongs to the Project with RTP {linkedRtp}");
                }
                else if (!string.IsNullOrWhiteSpace(request.Name)
                         && context.InnateCodes.Any(c => c.ActivityName.Trim().ToLower() == request.Name.Trim().ToLower()))
                {
                    errors.Add($"A timesheet code named '{request.Name}' already exists, so a new code for this Project would duplicate it");
                }
            }

            if (string.IsNullOrWhiteSpace(request.PI)) errors.Add("PI is required");
            if (string.IsNullOrWhiteSpace(request.RequestDocLink)) errors.Add("RequestDocLink is required");
            if (!ImportHelper.TryParseDefined<CostModel>(request.CostModel, out var costModel))
                errors.Add($"CostModel '{request.CostModel}' is not a valid value");
            else if (costModel == CostModel.DayRate && request.DayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");
            if (!ImportHelper.TryParseDefined<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");
            if (request.ManagementEndDate.Date < request.ManagementStartDate.Date)
                errors.Add("ManagementEndDate must be on or after ManagementStartDate");

            var school = ImportHelper.FindActiveSchoolByCode(context, request.SchoolCode);
            if (school == null)
                errors.Add($"SchoolCode '{request.SchoolCode}' does not match any active School");

            if (!string.IsNullOrWhiteSpace(request.ProjectManagerUsername) && ImportHelper.FindUserByUsername(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = ImportHelper.FindUserByUsername(context, r.Username)?.Person;
                if (person == null)
                    errors.Add($"Resourcing username '{r.Username}' not found, or has no linked Person");
                assignments.Add((person, r.Username, r.AssignmentFTE));
            }
            errors.AddRange(ValidateAssignments(context, assignments, Duty.ProjectWork, request.ManagementStartDate));

            if ((request.Comments?.Count ?? 0) > 0 && ImportHelper.FindUserByUsername(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it before importing comments");

            return errors;
        }

        /// <summary>
        /// Creates a project, related innate activity/task scaffolding, optional resourcing, and optional imported notes.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming project creation request.</param>
        /// <param name="caller">Authenticated caller creating the project.</param>
        /// <returns>Identifier payload for the created project and related record counts.</returns>
        public ImportProjectResponseDTO Create(PPMToolContext context, ImportProjectRequestDTO request, User caller)
        {
            var school = ImportHelper.FindActiveSchoolByCode(context, request.SchoolCode)!;
            var projectManager = string.IsNullOrWhiteSpace(request.ProjectManagerUsername)
                ? null
                : ImportHelper.FindUserByUsername(context, request.ProjectManagerUsername)?.Person;
            var costModel = Enum.Parse<CostModel>(request.CostModel);

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

            var innateActivity = ImportHelper.FindInnateCodeForRTP(context, project.RTP) ?? new InnateCode
            {
                ActivityCode = ImportHelper.InnateActivityCodeFor(project.RTP),
                ActivityName = project.Name,
                IsActive = true,
                Tasks = new List<InnateCodeTask>(),
            };
            foreach (var defaultTask in ImportHelper.DefaultInnateCodeTasks())
            {
                if (!innateActivity.Tasks.Any(t => t.TaskName.Trim().Equals(defaultTask.TaskName, StringComparison.OrdinalIgnoreCase)))
                    innateActivity.Tasks.Add(defaultTask);
            }
            project.InnateActivity = innateActivity;
            context.SaveChangesWithRetry();

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
                StartDate = ImportHelper.AsUnspecifiedKind(request.ManagementStartDate),
                EndDate = ImportHelper.AsUnspecifiedKind(request.ManagementEndDate),
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
                    StartDate = ImportHelper.AsUnspecifiedKind(request.ManagementStartDate),
                    EndDate = ImportHelper.AsUnspecifiedKind(request.ManagementEndDate),
                };
                delivery.Schedule();
                subTaskService.Add(context, delivery);

                foreach (var r in resourcing)
                {
                    var person = ImportHelper.FindUserByUsername(context, r.Username)!.Person!;
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
                ImportHelper.Reschedule(context, delivery);
                context.SaveChangesWithRetry();
            }

            var notesCreated = 0;
            var fallbackAuthor = ImportHelper.FindUserByUsername(context, FallbackAuthorUsername);
            foreach (var c in request.Comments ?? Array.Empty<ImportCommentDTO>())
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : ImportHelper.FindUserByUsername(context, c.AuthorUsername);
                author ??= fallbackAuthor!;

                var content = author.CASUserName == FallbackAuthorUsername
                    ? $"<p><em>Originally posted by {c.AuthorDisplayName} on Planner, {c.CreatedDate:yyyy-MM-dd}:</em></p>{c.ContentHtml}"
                    : c.ContentHtml;

                var createdDate = ImportHelper.AsUnspecifiedKind(c.CreatedDate);
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
                if (!ImportHelper.TryParseDefined<CostModel>(request.CostModel, out var parsed))
                    errors.Add($"CostModel '{request.CostModel}' is not a valid value");
                else
                    costModel = parsed;
            }
            var resolvedCostModel = costModel ?? project.CostModel;
            var resolvedDayRate = request.DayRate ?? project.DayRate;
            if (resolvedCostModel == CostModel.DayRate && resolvedDayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");

            if (request.ProjectStatus != null && !ImportHelper.TryParseDefined<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");

            if (request.SchoolCode != null && ImportHelper.FindActiveSchoolByCode(context, request.SchoolCode) == null)
                errors.Add($"SchoolCode '{request.SchoolCode}' does not match any active School");

            if (!string.IsNullOrEmpty(request.ProjectManagerUsername) && ImportHelper.FindUserByUsername(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

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

            if (request.Name != null) project.Name = request.Name;
            if (request.PI != null) project.PI = request.PI;
            if (request.SchoolCode != null) project.School = ImportHelper.FindActiveSchoolByCode(context, request.SchoolCode)!;
            if (request.ProjectManagerUsername != null)
                project.ProjectManager = request.ProjectManagerUsername == ""
                    ? null
                    : ImportHelper.FindUserByUsername(context, request.ProjectManagerUsername)!.Person!;
            if (request.Budget.HasValue) project.Budget = request.Budget.Value;
            if (request.CostModel != null) project.CostModel = Enum.Parse<CostModel>(request.CostModel);
            if (request.DayRate.HasValue) project.DayRate = request.DayRate.Value;
            if (project.CostModel != CostModel.DayRate) project.DayRate = 0;
            if (request.ProjectStatus != null) project.ProjectStatus = Enum.Parse<ProjectStatus>(request.ProjectStatus);
            if (request.Description != null) project.Description = request.Description;
            if (request.RequestDocLink != null) project.RequestDocLink = request.RequestDocLink;
            if (request.ScrumProjectLink != null) project.ScrumProjectLink = request.ScrumProjectLink == "" ? null : request.ScrumProjectLink;

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
        /// Validates a notes import request for an existing project.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming notes import request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateNotesImport(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (projectService.GetByRTP(context, request.RTP) == null)
                errors.Add($"RTP {request.RTP} does not match any Project");

            var comments = request.Comments ?? Array.Empty<ImportCommentDTO>();
            if (comments.Count == 0)
                errors.Add("Comments must contain at least one entry");

            if (comments.Count > 0 && ImportHelper.FindUserByUsername(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it (POST /api/users/add) before importing comments");

            return errors;
        }

        /// <summary>
        /// Adds imported notes to an existing project.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming notes import request.</param>
        /// <returns>Identifier payload for the project and number of created notes.</returns>
        public ImportNotesResponseDTO AddNotes(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var project = projectService.GetByRTP(context, request.RTP)!;
            var fallbackAuthor = ImportHelper.FindUserByUsername(context, FallbackAuthorUsername);

            var notesCreated = 0;
            foreach (var c in request.Comments)
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : ImportHelper.FindUserByUsername(context, c.AuthorUsername);
                author ??= fallbackAuthor!;

                var content = author.CASUserName == FallbackAuthorUsername
                    ? $"<p><em>Originally posted by {c.AuthorDisplayName} on Planner, {c.CreatedDate:yyyy-MM-dd}:</em></p>{c.ContentHtml}"
                    : c.ContentHtml;

                var createdDate = ImportHelper.AsUnspecifiedKind(c.CreatedDate);
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
            note.EditedDate = ImportHelper.AsUnspecifiedKind(DateTime.UtcNow);
            noteService.Update(context, note);
            context.SaveChangesWithRetry();

            return new UpdateNoteResponseDTO(note.NoteId);
        }

        /// <summary>
        /// Validates assignment constraints including FTE, manager-only duties, availability, and duplicates.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="assignments">Assignments to validate.</param>
        /// <param name="duty">Task duty for manager-only checks.</param>
        /// <param name="taskStart">Task start date.</param>
        /// <param name="existingTask">Optional existing task to detect duplicate assignees.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        private List<string> ValidateAssignments(
            PPMToolContext context,
            IEnumerable<(Person? Person, string Assignee, double FTE)> assignments,
            Duty duty,
            DateTime taskStart,
            SubTask? existingTask = null)
        {
            var errors = new List<string>();
            var managerPersonIds = duty == Duty.ProjectAndServiceMgmt
                ? userService.GetAllManagerPersonId(context).ToHashSet()
                : null;
            var seenPersonIds = new HashSet<int>();

            foreach (var (person, assignee, fte) in assignments)
            {
                if (ImportHelper.HasDigitsAfterThirdDecimalPlace(fte))
                    errors.Add($"AssignmentFTE for '{assignee}' cannot have digits after the third decimal place");
                else if (fte <= 0)
                    errors.Add($"AssignmentFTE for '{assignee}' must be greater than zero");

                if (person == null) continue;

                if (managerPersonIds != null && !managerPersonIds.Contains(person.PersonId))
                    errors.Add($"Only managers can be assigned to leadership tasks ('{assignee}')");

                if (ImportHelper.AssigneeStartsTooLate(person, taskStart) is { } tooLate)
                    errors.Add(tooLate);

                if (existingTask != null && existingTask.AssignedResources.Any(existing => existing.Person.PersonId == person.PersonId))
                    errors.Add($"'{assignee}' is already assigned to SubTaskId {existingTask.SubTaskId} -- use PUT /api/tasks/resourcing/update to change the existing assignment");
                if (!seenPersonIds.Add(person.PersonId))
                    errors.Add($"'{assignee}' appears more than once in this request");
            }

            return errors;
        }
    }
}
