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
    /// Service for API import operations that validate and mutate project tasks and task resourcing.
    /// </summary>
    public class ImportTaskService
    {
        private const string rtpRequired = "RTP is required and must be greater than zero";

        private readonly ProjectService projectService;
        private readonly SubTaskService subTaskService;
        private readonly FinancialReferenceService financialReferenceService;
        private readonly SettingsService settingsService;
        private readonly PersonService personService;
        private readonly UserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportTaskService"/> class.
        /// </summary>
        /// <param name="projectService">Service used to manage projects.</param>
        /// <param name="subTaskService">Service used to manage tasks.</param>
        /// <param name="financialReferenceService">Service used to load financial references.</param>
        /// <param name="settingsService">Service used to load calculation settings.</param>
        /// <param name="personService">Service used to resolve people for assignments.</param>
        /// <param name="userService">Service used to resolve user and manager information.</param>
        public ImportTaskService(
            ProjectService projectService,
            SubTaskService subTaskService,
            FinancialReferenceService financialReferenceService,
            SettingsService settingsService,
            PersonService personService,
            UserService userService)
        {
            this.projectService = projectService;
            this.subTaskService = subTaskService;
            this.financialReferenceService = financialReferenceService;
            this.settingsService = settingsService;
            this.personService = personService;
            this.userService = userService;
        }

        /// <summary>
        /// Validates a tasks-by-RTP lookup request.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTasksGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (projectService.GetByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// Gets tasks for a project identified by RTP.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Tasks for the project.</returns>
        public List<TaskDTO> GetTasksForRTP(PPMToolContext context, int rtp)
        {
            var project = projectService.GetByRTP(context, rtp)!;

            return project.SubTasks
                .OrderBy(t => t.StartDate)
                .Select(t => new TaskDTO(
                    t.SubTaskId, rtp, t.Name, t.TaskDuty.ToString(),
                    t.StartDate, t.EndDate, t.Demand, t.OriginalDemand, t.UnmetDemand
                ))
                .ToList();
        }

        /// <summary>
        /// Validates a task creation request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task creation request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTaskCreate(PPMToolContext context, ImportTaskDTO request)
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

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name cannot be blank");

            Duty? duty = null;
            if (!ImportHelper.TryParseDefined<Duty>(request.TaskDuty, out var parsedDuty))
                errors.Add($"TaskDuty '{request.TaskDuty}' is not a valid value");
            else
                duty = parsedDuty;

            if (!string.IsNullOrWhiteSpace(request.Name) && duty != null && duty != Duty.ProjectAndServiceMgmt
                && project.SubTasks.Any(t => t.Name == request.Name))
                errors.Add($"A task named '{request.Name}' already exists on Project {request.RTP}");

            if (request.EndDate.Date < request.StartDate.Date)
                errors.Add("EndDate must be on or after StartDate");

            if (ImportHelper.HasDigitsAfterThirdDecimalPlace(request.Demand))
                errors.Add("Demand cannot have digits after the third decimal place");
            else if (request.Demand <= 0)
                errors.Add("Demand must be greater than zero");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = ImportHelper.FindUserByUsername(context, r.Username)?.Person;
                if (person == null)
                    errors.Add($"Resourcing Username '{r.Username}' not found, or has no linked Person");
                assignments.Add((person, r.Username, r.AssignmentFTE));
            }
            errors.AddRange(ValidateAssignments(context, assignments, duty ?? Duty.ProjectWork, request.StartDate));

            return errors;
        }

        /// <summary>
        /// Creates a task and optional initial resourcing assignments.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task creation request.</param>
        /// <returns>Identifier payload for the created task and assignment count.</returns>
        public ImportTaskResponseDTO CreateTask(PPMToolContext context, ImportTaskDTO request)
        {
            var project = projectService.GetByRTP(context, request.RTP)!;
            var duty = Enum.Parse<Duty>(request.TaskDuty);

            var task = new SubTask
            {
                Name = request.Name,
                TaskDuty = duty,
                TaskType = TaskType.FixedDuration,
                HasFixedStart = true,
                HasFixedEndDate = true,
                Demand = request.Demand,
                OriginalDemand = request.Demand,
                OwningProject = project,
                StartDate = ImportHelper.AsUnspecifiedKind(request.StartDate),
                EndDate = ImportHelper.AsUnspecifiedKind(request.EndDate),
            };
            task.Schedule();
            subTaskService.Add(context, task);

            var created = new List<Resource>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = ImportHelper.FindUserByUsername(context, r.Username)!.Person!;
                var resource = new Resource
                {
                    Person = person,
                    SubTask = task,
                    AssignmentFTE = r.AssignmentFTE,
                    IsProvisional = true,
                };
                context.Resources.Add(resource);
                created.Add(resource);
            }
            ImportHelper.Reschedule(context, task);

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new ImportTaskResponseDTO(task.SubTaskId, created.Count);
        }

        /// <summary>
        /// Validates a task update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTaskUpdate(PPMToolContext context, UpdateTaskRequestDTO request)
        {
            var errors = new List<string>();

            var found = FindProjectAndTaskById(context, request.SubTaskId);
            if (found == null)
            {
                errors.Add($"SubTaskId {request.SubTaskId} does not exist");
                return errors;
            }
            var (project, task) = found.Value;

            Duty? newDuty = null;
            if (request.TaskDuty != null)
            {
                if (!ImportHelper.TryParseDefined<Duty>(request.TaskDuty, out var parsed))
                    errors.Add($"TaskDuty '{request.TaskDuty}' is not a valid value");
                else
                    newDuty = parsed;
            }
            var resolvedDuty = newDuty ?? task.TaskDuty;

            if (resolvedDuty == Duty.ProjectAndServiceMgmt && task.TaskDuty != Duty.ProjectAndServiceMgmt)
            {
                var managerPersonIds = userService.GetAllManagerPersonId(context).ToHashSet();
                foreach (var r in task.AssignedResources.Where(r => !managerPersonIds.Contains(r.Person.PersonId)))
                    errors.Add($"Only managers can be assigned to leadership tasks ('{r.Person.Name}' is assigned to this task)");
            }

            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    errors.Add("Name cannot be blank");
                else if (resolvedDuty != Duty.ProjectAndServiceMgmt
                         && project.SubTasks.Any(t => t.SubTaskId != task.SubTaskId && t.Name == request.Name))
                    errors.Add($"A different task named '{request.Name}' already exists on Project {project.RTP}");
            }

            var resolvedStart = request.StartDate ?? task.StartDate;
            var resolvedEnd = request.EndDate ?? task.EndDate;
            if (resolvedEnd.Date < resolvedStart.Date)
                errors.Add("EndDate must be on or after StartDate");

            foreach (var r in task.AssignedResources)
            {
                if (ImportHelper.AssigneeStartsTooLate(r.Person, resolvedStart) is { } tooLate)
                    errors.Add(tooLate);
            }

            if (request.Demand.HasValue)
            {
                if (ImportHelper.HasDigitsAfterThirdDecimalPlace(request.Demand.Value))
                    errors.Add("Demand cannot have digits after the third decimal place");
                else if (request.Demand.Value < 0)
                    errors.Add("Demand cannot be negative");
            }

            return errors;
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task update request.</param>
        /// <returns>Identifier payload for the updated task.</returns>
        public UpdateTaskResponseDTO UpdateTask(PPMToolContext context, UpdateTaskRequestDTO request)
        {
            var (project, task) = FindProjectAndTaskById(context, request.SubTaskId)!.Value;

            if (request.Name != null) task.Name = request.Name;
            if (request.TaskDuty != null) task.TaskDuty = Enum.Parse<Duty>(request.TaskDuty);
            if (request.StartDate.HasValue) task.StartDate = ImportHelper.AsUnspecifiedKind(request.StartDate.Value);
            if (request.EndDate.HasValue) task.EndDate = ImportHelper.AsUnspecifiedKind(request.EndDate.Value);
            if (request.Demand.HasValue) task.Demand = request.Demand.Value;
            ImportHelper.Reschedule(context, task);
            subTaskService.Update(context, task);

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new UpdateTaskResponseDTO(task.SubTaskId);
        }

        /// <summary>
        /// Validates a task-resourcing-by-RTP lookup request.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTaskResourcingGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (projectService.GetByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// Gets task resourcing for a project identified by RTP.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="rtp">RTP value to look up.</param>
        /// <returns>Resourcing rows for tasks in the project.</returns>
        public List<TaskResourceDTO> GetResourcingForRTP(PPMToolContext context, int rtp)
        {
            var project = projectService.GetByRTP(context, rtp)!;

            var personIds = project.SubTasks
                .SelectMany(t => t.AssignedResources)
                .Select(r => r.Person.PersonId)
                .Distinct()
                .ToList();
            var usernamesByPersonId = context.Users
                .Where(u => u.Person != null && personIds.Contains(u.Person.PersonId))
                .OrderBy(u => u.UserId)
                .Select(u => new { u.Person!.PersonId, u.CASUserName })
                .ToList()
                .GroupBy(x => x.PersonId)
                .ToDictionary(g => g.Key, g => g.First().CASUserName);

            return project.SubTasks
                .OrderBy(t => t.StartDate)
                .SelectMany(t => t.AssignedResources.Select(r => new TaskResourceDTO(
                    r.ResourceId,
                    t.SubTaskId,
                    rtp,
                    t.Name,
                    t.TaskDuty.ToString(),
                    t.StartDate,
                    t.EndDate,
                    r.Person.PersonId,
                    r.Person.Name,
                    usernamesByPersonId.GetValueOrDefault(r.Person.PersonId),
                    r.AssignmentFTE,
                    r.IsProvisional
                )))
                .ToList();
        }

        /// <summary>
        /// Validates a request that adds resourcing assignments to a task.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task-resourcing add request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTaskResourcingAdd(PPMToolContext context, ImportTaskResourcingRequestDTO request)
        {
            var errors = new List<string>();

            var found = FindProjectAndTaskById(context, request.SubTaskId);
            if (found == null)
            {
                errors.Add($"SubTaskId {request.SubTaskId} does not exist");
                return errors;
            }
            var (_, task) = found.Value;

            var resourcing = request.Resourcing ?? Array.Empty<ImportResourceAssignmentDTO>();
            if (resourcing.Count == 0)
                errors.Add("Resourcing must contain at least one assignment");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in resourcing)
            {
                var (person, personErrors) = ResolveAssignmentPerson(context, r);
                errors.AddRange(personErrors);
                assignments.Add((person, DescribeAssignee(r), r.AssignmentFTE));
            }
            errors.AddRange(ValidateAssignments(context, assignments, task.TaskDuty, task.StartDate, existingTask: task));

            return errors;
        }

        /// <summary>
        /// Adds resourcing assignments to an existing task.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task-resourcing add request.</param>
        /// <returns>Identifier payload for the task and number of assignments created.</returns>
        public ImportTaskResourcingResponseDTO AddTaskResourcing(PPMToolContext context, ImportTaskResourcingRequestDTO request)
        {
            var (project, task) = FindProjectAndTaskById(context, request.SubTaskId)!.Value;

            var created = new List<Resource>();
            foreach (var r in request.Resourcing)
            {
                var (person, _) = ResolveAssignmentPerson(context, r);
                var resource = new Resource
                {
                    Person = person!,
                    SubTask = task,
                    AssignmentFTE = r.AssignmentFTE,
                    IsProvisional = r.IsProvisional ?? true,
                };
                context.Resources.Add(resource);
                created.Add(resource);
            }

            ImportHelper.Reschedule(context, task);
            RecalculateProject(context, project);

            return new ImportTaskResourcingResponseDTO(task.SubTaskId, created.Count);
        }

        /// <summary>
        /// Validates a task-resourcing update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task-resourcing update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateTaskResourcingUpdate(PPMToolContext context, UpdateTaskResourcingRequestDTO request)
        {
            var errors = new List<string>();

            var found = FindProjectAndResourceById(context, request.ResourceId);
            if (found == null)
            {
                errors.Add($"ResourceId {request.ResourceId} does not exist");
                return errors;
            }
            var (_, currentTask, resource) = found.Value;

            if (request.AssignmentFTE.HasValue)
            {
                if (ImportHelper.HasDigitsAfterThirdDecimalPlace(request.AssignmentFTE.Value))
                    errors.Add("AssignmentFTE cannot have digits after the third decimal place");
                else if (request.AssignmentFTE.Value < 0)
                    errors.Add("AssignmentFTE cannot be negative");
            }

            var targetTask = currentTask;
            if (request.NewSubTaskId.HasValue && request.NewSubTaskId.Value != currentTask.SubTaskId)
            {
                var target = FindProjectAndTaskById(context, request.NewSubTaskId.Value);
                if (target == null)
                {
                    errors.Add($"NewSubTaskId {request.NewSubTaskId} does not exist");
                    return errors;
                }
                targetTask = target.Value.Task;

                if (targetTask.AssignedResources.Any(existing => existing.Person.PersonId == resource.Person.PersonId))
                    errors.Add($"'{resource.Person.Name}' is already assigned to SubTaskId {request.NewSubTaskId} -- move would create a duplicate assignment");
            }

            if (targetTask.TaskDuty == Duty.ProjectAndServiceMgmt
                && !userService.GetAllManagerPersonId(context).Contains(resource.Person.PersonId))
                errors.Add($"Only managers can be assigned to leadership tasks ('{resource.Person.Name}')");

            if (ImportHelper.AssigneeStartsTooLate(resource.Person, targetTask.StartDate) is { } tooLate)
                errors.Add(tooLate);

            return errors;
        }

        /// <summary>
        /// Updates an existing task-resourcing assignment.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming task-resourcing update request.</param>
        /// <returns>Identifier payload for the updated assignment.</returns>
        public UpdateTaskResourcingResponseDTO UpdateTaskResourcing(PPMToolContext context, UpdateTaskResourcingRequestDTO request)
        {
            var (project, currentTask, resource) = FindProjectAndResourceById(context, request.ResourceId)!.Value;

            if (request.AssignmentFTE.HasValue) resource.AssignmentFTE = request.AssignmentFTE.Value;
            if (request.IsProvisional.HasValue) resource.IsProvisional = request.IsProvisional.Value;

            var landedOn = currentTask;
            Project? movedFromProject = null;
            if (request.NewSubTaskId.HasValue && request.NewSubTaskId.Value != currentTask.SubTaskId)
            {
                var (targetProject, targetTask) = FindProjectAndTaskById(context, request.NewSubTaskId.Value)!.Value;
                resource.SubTask = targetTask;
                landedOn = targetTask;

                if (targetProject.RTP != project.RTP) movedFromProject = project;
                project = targetProject;
            }

            context.Resources.Update(resource);
            if (landedOn != currentTask)
                ImportHelper.Reschedule(context, currentTask, landedOn);
            else
                ImportHelper.Reschedule(context, currentTask);
            if (movedFromProject != null) RecalculateProject(context, movedFromProject, commit: false);
            RecalculateProject(context, project);

            return new UpdateTaskResourcingResponseDTO(resource.ResourceId, landedOn.SubTaskId);
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

        /// <summary>
        /// Finds a project and task by task identifier.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="subTaskId">Task identifier.</param>
        /// <returns>The containing project and task, or <c>null</c> when not found.</returns>
        private (Project Project, SubTask Task)? FindProjectAndTaskById(PPMToolContext context, int subTaskId)
        {
            var rtp = context.SubTasks
                .Where(t => t.SubTaskId == subTaskId)
                .Select(t => (int?)t.OwningProject.RTP)
                .FirstOrDefault();
            if (rtp == null) return null;

            var project = projectService.GetByRTP(context, rtp.Value);
            var task = project?.SubTasks.FirstOrDefault(t => t.SubTaskId == subTaskId);
            return task == null ? null : (project!, task);
        }

        /// <summary>
        /// Resolves an assignment person from username or person ID and returns any resolution errors.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="assignment">Assignment identity payload.</param>
        /// <returns>The resolved person and any validation errors.</returns>
        private (Person? Person, List<string> Errors) ResolveAssignmentPerson(
            PPMToolContext context, ImportResourceAssignmentDTO assignment)
        {
            var errors = new List<string>();
            var hasUsername = !string.IsNullOrWhiteSpace(assignment.Username);
            var hasPersonId = assignment.PersonId.HasValue;

            if (hasUsername == hasPersonId)
            {
                errors.Add($"Exactly one of Username or PersonId must be supplied (got '{DescribeAssignee(assignment)}')");
                return (null, errors);
            }

            if (hasUsername)
            {
                var person = ImportHelper.FindUserByUsername(context, assignment.Username!)?.Person;
                if (person == null)
                    errors.Add($"Username '{assignment.Username}' not found, or has no linked Person");
                return (person, errors);
            }

            var byId = personService.GetById(context, assignment.PersonId!.Value);
            if (byId == null)
                errors.Add($"PersonId {assignment.PersonId} does not exist");
            return (byId, errors);
        }

        /// <summary>
        /// Produces a display label for an assignment identity payload.
        /// </summary>
        /// <param name="assignment">Assignment identity payload.</param>
        /// <returns>Username, person-id label, or a fallback marker.</returns>
        private static string DescribeAssignee(ImportResourceAssignmentDTO assignment) =>
            !string.IsNullOrWhiteSpace(assignment.Username)
                ? assignment.Username!
                : assignment.PersonId.HasValue ? $"PersonId {assignment.PersonId}" : "(no Username or PersonId)";

        /// <summary>
        /// Finds a project, task, and resource assignment by resource identifier.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="resourceId">Resource assignment identifier.</param>
        /// <returns>The containing project, task, and assignment, or <c>null</c> when not found.</returns>
        private (Project Project, SubTask Task, Resource Resource)? FindProjectAndResourceById(
            PPMToolContext context, int resourceId)
        {
            var rtp = context.Resources
                .Where(r => r.ResourceId == resourceId)
                .Select(r => (int?)r.SubTask.OwningProject.RTP)
                .FirstOrDefault();
            if (rtp == null) return null;

            var project = projectService.GetByRTP(context, rtp.Value);
            var task = project?.SubTasks.FirstOrDefault(t => t.AssignedResources.Any(r => r.ResourceId == resourceId));
            var resource = task?.AssignedResources.FirstOrDefault(r => r.ResourceId == resourceId);
            return resource == null ? null : (project!, task!, resource);
        }

        /// <summary>
        /// Recalculates project metadata after task or resourcing changes.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="project">Project to recalculate.</param>
        /// <param name="commit">Whether to commit changes immediately.</param>
        private void RecalculateProject(PPMToolContext context, Project project, bool commit = true)
        {
            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            if (commit) context.SaveChangesWithRetry();
        }
    }
}
