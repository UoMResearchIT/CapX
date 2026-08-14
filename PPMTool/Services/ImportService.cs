// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Backs the Superuser-only POST /api/import/* endpoints
    /// (see PPMTool.API.Endpoints.Import), gated behind
    /// SettingType.ImportApiEnabled (towards #1310).
    /// </summary>
    public class ImportService
    {
        public const string FallbackAuthorUsername = "migration-import";

        private readonly FacultyService _facultyService;
        private readonly SchoolService _schoolService;
        private readonly ProjectService _projectService;
        private readonly SubTaskService _subTaskService;
        private readonly NoteService _noteService;
        private readonly FinancialReferenceService _financialReferenceService;
        private readonly SettingsService _settingsService;
        private readonly TimesheetService _timesheetService;

        public ImportService(
            FacultyService facultyService,
            SchoolService schoolService,
            ProjectService projectService,
            SubTaskService subTaskService,
            NoteService noteService,
            FinancialReferenceService financialReferenceService,
            SettingsService settingsService,
            TimesheetService timesheetService)
        {
            _facultyService = facultyService;
            _schoolService = schoolService;
            _projectService = projectService;
            _subTaskService = subTaskService;
            _noteService = noteService;
            _financialReferenceService = financialReferenceService;
            _settingsService = settingsService;
            _timesheetService = timesheetService;
        }

        /// <summary>
        /// Validate a POST /api/import/faculty request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateFaculty(PPMToolContext context, ImportFacultyRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            if (string.IsNullOrWhiteSpace(request.Code)) errors.Add("Code is required");
            if (!string.IsNullOrWhiteSpace(request.Name) && !string.IsNullOrWhiteSpace(request.Code)
                && _facultyService.DuplicateDetected(context, new Faculty { Name = request.Name, Code = request.Code }))
                errors.Add($"A Faculty named '{request.Name}' or with code '{request.Code}' already exists");

            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in request.Schools ?? Array.Empty<ImportSchoolDTO>())
            {
                if (string.IsNullOrWhiteSpace(s.Name)) errors.Add($"School Name is required (code '{s.Code}')");
                if (string.IsNullOrWhiteSpace(s.Code)) errors.Add($"School Code is required (name '{s.Name}')");
                else if (!seenCodes.Add(s.Code.Trim().ToLowerInvariant()))
                    errors.Add($"Duplicate School code '{s.Code}' within this request");
            }

            return errors;
        }

        /// <summary>
        /// Create the Faculty (+ any Schools). Caller is responsible for
        /// validating first.
        /// </summary>
        public ImportFacultyResponseDTO CreateFaculty(PPMToolContext context, ImportFacultyRequestDTO request)
        {
            var faculty = new Faculty
            {
                Name = request.Name,
                Code = request.Code,
            };
            var facultyId = _facultyService.Add(context, faculty);
            if (facultyId < 0)
                throw new InvalidOperationException($"FacultyService.Add returned {facultyId} (duplicate) despite passing ValidateFaculty() -- possible race condition");

            var schoolIds = new List<int>();
            foreach (var s in request.Schools ?? Array.Empty<ImportSchoolDTO>())
            {
                var school = new School
                {
                    Name = s.Name,
                    Code = s.Code,
                    Faculty = faculty,
                };
                var schoolId = _schoolService.Add(context, school);
                if (schoolId < 0)
                    throw new InvalidOperationException($"SchoolService.Add returned {schoolId} for School '{s.Name}' despite passing ValidateFaculty()");
                schoolIds.Add(schoolId);
            }

            return new ImportFacultyResponseDTO(faculty.FacultyId, schoolIds);
        }

        /// <summary>
        /// Validate a POST /api/import/project request without writing
        /// anything. Returns the errors that would prevent import, or an
        /// empty list if it's valid.
        /// </summary>
        public List<string> Validate(PPMToolContext context, ImportProjectRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            else if (_projectService.DuplicateDetected(context, new Project { Name = request.Name }))
                errors.Add($"A Project named '{request.Name}' already exists");
            if (context.Projects.Any(p => p.RTP == request.RTP))
                errors.Add($"A Project with RTP {request.RTP} already exists");
            if (string.IsNullOrWhiteSpace(request.PI)) errors.Add("PI is required");
            if (string.IsNullOrWhiteSpace(request.RequestDocLink)) errors.Add("RequestDocLink is required");
            if (!Enum.TryParse<CostModel>(request.CostModel, out var costModel))
                errors.Add($"CostModel '{request.CostModel}' is not a valid value");
            else if (costModel == CostModel.DayRate && request.DayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");
            if (!Enum.TryParse<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");

            var school = FindActiveSchoolByCode(context, request.SchoolCode);
            if (school == null)
                errors.Add($"SchoolCode '{request.SchoolCode}' does not match any active School");

            if (!string.IsNullOrWhiteSpace(request.ProjectManagerUsername) && FindUserByUsername(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                if (FindUserByUsername(context, r.Username)?.Person == null)
                    errors.Add($"Resourcing username '{r.Username}' not found, or has no linked Person");
            }

            if ((request.Comments?.Count ?? 0) > 0 && FindUserByUsername(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it before importing comments");

            return errors;
        }

        /// <summary>
        /// Create the Project (+ SubTasks/Resources/Notes). Caller is
        /// responsible for validating first and for transaction/commit
        /// semantics -- this just adds entities and saves.
        /// </summary>
        public ImportProjectResponseDTO Create(PPMToolContext context, ImportProjectRequestDTO request)
        {
            var school = FindActiveSchoolByCode(context, request.SchoolCode)!;
            var projectManager = string.IsNullOrWhiteSpace(request.ProjectManagerUsername)
                ? null
                : FindUserByUsername(context, request.ProjectManagerUsername)?.Person;
            var costModel = Enum.Parse<CostModel>(request.CostModel);

            var project = new Project
            {
                Name = request.Name,
                RTP = request.RTP,
                PI = request.PI,
                School = school,
                ProjectManager = projectManager,
                Budget = request.Budget,
                CostModel = costModel,
                DayRate = costModel == CostModel.DayRate ? request.DayRate : 0,
                ProjectStatus = Enum.Parse<ProjectStatus>(request.ProjectStatus),
                Description = request.Description,
                RequestDocLink = request.RequestDocLink,
                ScrumProjectLink = string.IsNullOrWhiteSpace(request.ScrumProjectLink) ? null : request.ScrumProjectLink,
            };
            var projectId = _projectService.Add(context, project); // commits, assigns ProjectId
            if (projectId < 0)
                throw new InvalidOperationException($"ProjectService.Add returned {projectId} (duplicate name/RTP) despite passing Validate() -- possible race condition");

            // Every Project needs an InnateActivity code so hours logged against it in
            // Timesheets can actually be attributed back (CapX computes a Project's
            // actuals by querying Approved Timesheets linked via this code -- see
            // AddTask.razor.cs, TimesheetService.GetAllForInnateCode). Without one, a
            // future POST /api/import/timesheet call for this project has nothing to
            // attach to. Mirrors SeedHelper.EnsureInnateCodeExists/GetDefaultInnateCodeTasks
            // exactly: one InnateCode per project keyed "S-RES-RTP-{RTP}", with the same
            // three default tasks.
            project.InnateActivity = new InnateCode
            {
                ActivityCode = $"S-RES-RTP-{project.RTP}",
                ActivityName = project.Name,
                IsActive = true,
                Tasks = new List<InnateCodeTask>
                {
                    new() { TaskName = "Development", Duty = Duty.ProjectWork },
                    new() { TaskName = "Management", Duty = Duty.ProjectAndServiceMgmt },
                    new() { TaskName = "Maintenance", Duty = Duty.ProjectWork },
                },
            };
            context.SaveChangesWithRetry();

            // Every Project needs a task carrying Duty.ProjectAndServiceMgmt --
            // ProjectStatusEvaluator flags "This project does not have a project
            // management task!" as an error otherwise. Shaped the same way
            // SeedHelper.CreateLeadershipSubTask builds one: fixed
            // start/end, FixedDuration, demand from the same setting the UI
            // defaults to. OriginalDemand must be > 0 (see AddTask.razor.cs)
            // so it can't be left at the entity default.
            var managementFte = _settingsService.GetSetting(SettingType.TechnicalLeadershipDefaultFTE, 0.05f);
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
                StartDate = request.ManagementStartDate,
                EndDate = request.ManagementEndDate,
            };
            managementTask.Schedule();
            _subTaskService.Add(context, managementTask);

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
                    StartDate = request.ManagementStartDate,
                    EndDate = request.ManagementEndDate,
                };
                delivery.Schedule();
                _subTaskService.Add(context, delivery);

                foreach (var r in resourcing)
                {
                    var person = FindUserByUsername(context, r.Username)!.Person!;
                    context.Resources.Add(new Resource
                    {
                        Person = person,
                        SubTask = delivery,
                        AssignmentFTE = r.AssignmentFTE,
                        IsProvisional = true, // migrated data -- flag for PM review, not treated as confirmed
                    });
                    resourcesCreated++;
                }
                context.SaveChangesWithRetry();
            }

            var notesCreated = 0;
            var fallbackAuthor = FindUserByUsername(context, FallbackAuthorUsername);
            foreach (var c in request.Comments ?? Array.Empty<ImportCommentDTO>())
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : FindUserByUsername(context, c.AuthorUsername);
                author ??= fallbackAuthor!; // validated to exist if there are any comments

                var content = author.CASUserName == FallbackAuthorUsername
                    ? $"<p><em>Originally posted by {c.AuthorDisplayName} on Planner, {c.CreatedDate:yyyy-MM-dd}:</em></p>{c.ContentHtml}"
                    : c.ContentHtml;

                _noteService.Add(context, new Note
                {
                    Project = project,
                    Author = author,
                    HtmlContent = content,
                    CreatedDate = c.CreatedDate,
                    EditedDate = c.CreatedDate,
                });
                notesCreated++;
            }

            var financialReferences = _financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = _settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new ImportProjectResponseDTO(project.ProjectId, resourcesCreated, notesCreated);
        }

        /// <summary>
        /// Validate a POST /api/import/timesheet request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var errors = new List<string>();

            if (FindUserByUsername(context, request.Username)?.Person == null)
                errors.Add($"Username '{request.Username}' not found, or has no linked Person");

            if (request.WeekStartDate.DayOfWeek != DayOfWeek.Monday)
                errors.Add($"WeekStartDate '{request.WeekStartDate:yyyy-MM-dd}' is a {request.WeekStartDate.DayOfWeek}, not a Monday -- CapX Timesheets are always Monday-start weeks");

            foreach (var (label, hours) in DayHours(request))
            {
                if (hours < 0) errors.Add($"{label} cannot be negative");
            }

            var project = FindProjectWithInnateActivity(context, request.ProjectId);
            if (project == null)
                errors.Add($"ProjectId {request.ProjectId} does not exist");
            else if (project.InnateActivity == null)
                errors.Add($"Project {request.ProjectId} ('{project.Name}') has no InnateActivity code -- only projects created via POST /api/import/project (or otherwise already linked) can receive imported timesheet entries");
            else if (!project.InnateActivity.Tasks.Any(t => t.TaskName.Trim().Equals(request.TaskName.Trim(), StringComparison.OrdinalIgnoreCase)))
                errors.Add($"TaskName '{request.TaskName}' does not match any InnateCodeTask under project {request.ProjectId}'s InnateActivity ('{project.InnateActivity.ActivityName}'); available: {string.Join(", ", project.InnateActivity.Tasks.Select(t => t.TaskName))}");

            return errors;
        }

        /// <summary>
        /// Create or update the Timesheet + TimesheetEntry for this
        /// person/week/task. Caller is responsible for validating first.
        /// Idempotent: re-importing the same (Username, WeekStartDate,
        /// TaskName) overwrites the existing entry's hours rather than
        /// accumulating on top of them.
        /// </summary>
        public ImportTimesheetResponseDTO CreateOrUpdateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var person = FindUserByUsername(context, request.Username)!.Person!;
            var project = FindProjectWithInnateActivity(context, request.ProjectId)!;
            var task = project.InnateActivity!.Tasks.First(t => t.TaskName.Trim().Equals(request.TaskName.Trim(), StringComparison.OrdinalIgnoreCase));

            var timesheet = context.Timesheets
                .Include(t => t.TimesheetEntries)
                .FirstOrDefault(t => t.Owner.PersonId == person.PersonId && t.StartDate == request.WeekStartDate);

            var timesheetCreated = timesheet == null;
            if (timesheet == null)
            {
                timesheet = new Timesheet
                {
                    Owner = person,
                    StartDate = request.WeekStartDate,
                    Status = TimesheetStatus.Approved, // historical actuals -- not pending review
                    DateStatusChanged = DateTime.Now,
                };
                var timesheetId = _timesheetService.Add(context, timesheet);
                if (timesheetId < 0)
                    throw new InvalidOperationException($"TimesheetService.Add returned {timesheetId} (duplicate) despite passing ValidateTimesheetEntry() -- possible race condition");
            }

            var entry = timesheet.TimesheetEntries.FirstOrDefault(e => e.InnateCodeTaskId == task.InnateCodeTaskId);
            var entryCreated = entry == null;
            if (entry == null)
            {
                entry = new TimesheetEntry { Timesheet = timesheet, InnateCodeTask = task };
                _timesheetService.AddEntry(context, entry, commitChanges: false);
            }

            entry.MondayHours = request.MondayHours;
            entry.TuesdayHours = request.TuesdayHours;
            entry.WednesdayHours = request.WednesdayHours;
            entry.ThursdayHours = request.ThursdayHours;
            entry.FridayHours = request.FridayHours;
            entry.SaturdayHours = request.SaturdayHours;
            entry.SundayHours = request.SundayHours;
            entry.UpdateTotalHours();
            context.SaveChangesWithRetry();

            return new ImportTimesheetResponseDTO(timesheet.TimesheetId, timesheetCreated, entryCreated, entry.TotalHours);
        }

        private static IEnumerable<(string Label, double Hours)> DayHours(ImportTimesheetEntryDTO request)
        {
            yield return ("MondayHours", request.MondayHours);
            yield return ("TuesdayHours", request.TuesdayHours);
            yield return ("WednesdayHours", request.WednesdayHours);
            yield return ("ThursdayHours", request.ThursdayHours);
            yield return ("FridayHours", request.FridayHours);
            yield return ("SaturdayHours", request.SaturdayHours);
            yield return ("SundayHours", request.SundayHours);
        }

        // .Include(InnateActivity.Tasks) is required, not optional -- same class of bug as
        // FindUserByUsername's WorkloadModelChanges include (see below): without it,
        // project.InnateActivity.Tasks is null/empty after materialization even for a
        // project that has tasks in the DB, so task-name matching would silently fail.
        private static Project? FindProjectWithInnateActivity(PPMToolContext context, int projectId) =>
            context.Projects
                .Include(p => p.InnateActivity)
                    .ThenInclude(a => a!.Tasks)
                .FirstOrDefault(p => p.ProjectId == projectId);

        // ThenInclude(WorkloadModelChanges) is required, not optional -- AssignmentHelper.GetAssignmentChunks
        // (called via Project.UpdateProjectMetaData -> SubTask.UpdateSubTaskCosts -> Resource.UpdateResourceCosts)
        // reads person.WorkloadModelChanges directly. EF leaves un-included navigation collections null after
        // a query (the entity's C# field initializer doesn't survive materialization), so omitting this NREs
        // deep inside cost calculation instead of failing validation up front.
        private static User? FindUserByUsername(PPMToolContext context, string username) =>
            context.Users
                .Include(u => u.Person)
                    .ThenInclude(p => p!.WorkloadModelChanges)
                .FirstOrDefault(u => u.CASUserName.Trim().ToLower() == username.Trim().ToLower());

        // .Include(s => s.Faculty) is required, not optional -- same class of bug as
        // FindUserByUsername's WorkloadModelChanges include. AssignmentHelper.GetAssignmentChunks
        // reads project.School.Faculty.Name directly; SchoolService.GetAllActive() doesn't include
        // it (returns IEnumerable<School>, not IQueryable, so callers can't add it after the fact
        // either), so this needs its own query rather than reusing that service method.
        private static School? FindActiveSchoolByCode(PPMToolContext context, string code) =>
            context.Schools
                .Include(s => s.Faculty)
                .FirstOrDefault(s => s.IsActive && s.Code.Trim().ToLower() == code.Trim().ToLower());
    }
}
