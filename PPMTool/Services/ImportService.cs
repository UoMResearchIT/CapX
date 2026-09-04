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
    /// Backs the Superuser-only bulk-write "/add" endpoints (Faculties,
    /// Schools, Projects, Timesheets, WorkloadModels), gated behind
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
        private readonly PersonService _personService;
        private readonly UserService _userService;

        public ImportService(
            FacultyService facultyService,
            SchoolService schoolService,
            ProjectService projectService,
            SubTaskService subTaskService,
            NoteService noteService,
            FinancialReferenceService financialReferenceService,
            SettingsService settingsService,
            TimesheetService timesheetService,
            PersonService personService,
            UserService userService)
        {
            _facultyService = facultyService;
            _schoolService = schoolService;
            _projectService = projectService;
            _subTaskService = subTaskService;
            _noteService = noteService;
            _financialReferenceService = financialReferenceService;
            _settingsService = settingsService;
            _timesheetService = timesheetService;
            _personService = personService;
            _userService = userService;
        }

        /// <summary>
        /// Validate a POST /api/faculties/add request without writing
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
        /// Validate a PUT /api/faculties/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateFacultyUpdate(PPMToolContext context, UpdateFacultyRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add("Code is required");
                return errors;
            }

            var faculty = FindFacultyByCode(context, request.Code);
            if (faculty == null)
            {
                errors.Add($"Code '{request.Code}' does not match any Faculty");
                return errors;
            }

            if (request.Name == null && request.NewCode == null)
                errors.Add("At least one of Name or NewCode must be supplied");

            var probe = new Faculty
            {
                FacultyId = faculty.FacultyId,
                Name = request.Name ?? faculty.Name,
                Code = request.NewCode ?? faculty.Code,
            };
            if (_facultyService.DuplicateDetected(context, probe))
                errors.Add($"A different Faculty named '{probe.Name}' or with code '{probe.Code}' already exists");

            return errors;
        }

        /// <summary>
        /// Update the Faculty's Name and/or Code. Caller is responsible for
        /// validating first.
        /// </summary>
        public UpdateFacultyResponseDTO UpdateFaculty(PPMToolContext context, UpdateFacultyRequestDTO request)
        {
            var faculty = FindFacultyByCode(context, request.Code)!;
            if (request.Name != null) faculty.Name = request.Name;
            if (request.NewCode != null) faculty.Code = request.NewCode;

            var result = _facultyService.Update(context, faculty);
            if (result < 0)
                throw new InvalidOperationException($"FacultyService.Update returned {result} (duplicate) despite passing ValidateFacultyUpdate() -- possible race condition");

            return new UpdateFacultyResponseDTO(faculty.FacultyId);
        }

        /// <summary>
        /// Validate a POST /api/schools/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateSchool(PPMToolContext context, ImportSchoolRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            if (string.IsNullOrWhiteSpace(request.Code)) errors.Add("Code is required");
            if (string.IsNullOrWhiteSpace(request.FacultyCode)) errors.Add("FacultyCode is required");

            if (!string.IsNullOrWhiteSpace(request.FacultyCode))
            {
                var faculty = FindFacultyByCode(context, request.FacultyCode);
                if (faculty == null)
                {
                    errors.Add($"FacultyCode '{request.FacultyCode}' does not match any Faculty");
                }
                else if (!string.IsNullOrWhiteSpace(request.Name) && !string.IsNullOrWhiteSpace(request.Code)
                    && _schoolService.DuplicateDetected(context, new School { Name = request.Name, Code = request.Code, Faculty = faculty }))
                {
                    errors.Add($"A School named '{request.Name}' or with code '{request.Code}' already exists under Faculty '{faculty.Name}'");
                }
            }

            return errors;
        }

        /// <summary>
        /// Create the School under an existing Faculty. Caller is
        /// responsible for validating first.
        /// </summary>
        public ImportSchoolResponseDTO CreateSchool(PPMToolContext context, ImportSchoolRequestDTO request)
        {
            var faculty = FindFacultyByCode(context, request.FacultyCode)!;
            var school = new School
            {
                Name = request.Name,
                Code = request.Code,
                Faculty = faculty,
            };
            var schoolId = _schoolService.Add(context, school);
            if (schoolId < 0)
                throw new InvalidOperationException($"SchoolService.Add returned {schoolId} for School '{request.Name}' despite passing ValidateSchool() -- possible race condition");

            return new ImportSchoolResponseDTO(schoolId, faculty.FacultyId);
        }

        /// <summary>
        /// Validate a PUT /api/schools/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateSchoolUpdate(PPMToolContext context, UpdateSchoolRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Code))
            {
                errors.Add("Code is required");
                return errors;
            }

            var school = FindSchoolByCode(context, request.Code);
            if (school == null)
            {
                errors.Add($"Code '{request.Code}' does not match any School");
                return errors;
            }

            if (request.Name == null && request.NewCode == null && request.NewFacultyCode == null)
                errors.Add("At least one of Name, NewCode, or NewFacultyCode must be supplied");

            var faculty = school.Faculty;
            if (request.NewFacultyCode != null)
            {
                faculty = FindFacultyByCode(context, request.NewFacultyCode);
                if (faculty == null)
                    errors.Add($"NewFacultyCode '{request.NewFacultyCode}' does not match any Faculty");
            }

            if (faculty != null)
            {
                var probe = new School
                {
                    SchoolId = school.SchoolId,
                    Name = request.Name ?? school.Name,
                    Code = request.NewCode ?? school.Code,
                    Faculty = faculty,
                };
                if (_schoolService.DuplicateDetected(context, probe))
                    errors.Add($"A different School named '{probe.Name}' or with code '{probe.Code}' already exists under Faculty '{faculty.Name}'");
            }

            return errors;
        }

        /// <summary>
        /// Update the School's Name, Code, and/or parent Faculty. Caller is
        /// responsible for validating first.
        /// </summary>
        public ImportSchoolResponseDTO UpdateSchool(PPMToolContext context, UpdateSchoolRequestDTO request)
        {
            var school = FindSchoolByCode(context, request.Code)!;
            if (request.Name != null) school.Name = request.Name;
            if (request.NewCode != null) school.Code = request.NewCode;
            if (request.NewFacultyCode != null) school.Faculty = FindFacultyByCode(context, request.NewFacultyCode)!;

            var result = _schoolService.Update(context, school);
            if (result < 0)
                throw new InvalidOperationException($"SchoolService.Update returned {result} (duplicate) despite passing ValidateSchoolUpdate() -- possible race condition");

            return new ImportSchoolResponseDTO(school.SchoolId, school.Faculty.FacultyId);
        }

        /// <summary>
        /// Validate a POST /api/projects/add request without writing
        /// anything. Returns the errors that would prevent import, or an
        /// empty list if it's valid.
        /// </summary>
        public List<string> Validate(PPMToolContext context, ImportProjectRequestDTO request, User caller)
        {
            var errors = new List<string>();

            // RequestOwnerId is a required, non-nullable FK on Project ("the person who
            // created the project request -- automatically set to the logged in user").
            // There's no interactive login here, so it's set to the API caller instead --
            // needs a linked Person the same way ProjectManager/Resourcing usernames do.
            if (caller.Person == null)
                errors.Add($"Caller '{caller.CASUserName}' has no linked Person -- required to set as the imported Project's RequestOwner");

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
        public ImportProjectResponseDTO Create(PPMToolContext context, ImportProjectRequestDTO request, User caller)
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
                RequestOwner = caller.Person!, // validated non-null in Validate()
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
            // future POST /api/timesheets/add call for this project has nothing to
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
        /// Validate a PUT /api/projects/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateProjectUpdate(PPMToolContext context, UpdateProjectRequestDTO request)
        {
            var errors = new List<string>();

            var project = FindProjectByRTP(context, request.RTP);
            if (project == null)
            {
                errors.Add($"RTP {request.RTP} does not match any Project");
                return errors;
            }

            if (request.Name != null)
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                    errors.Add("Name cannot be blank");
                else if (_projectService.DuplicateDetected(context, new Project { ProjectId = project.ProjectId, Name = request.Name }))
                    errors.Add($"A different Project named '{request.Name}' already exists");
            }

            CostModel? costModel = null;
            if (request.CostModel != null)
            {
                if (!Enum.TryParse<CostModel>(request.CostModel, out var parsed))
                    errors.Add($"CostModel '{request.CostModel}' is not a valid value");
                else
                    costModel = parsed;
            }
            var resolvedCostModel = costModel ?? project.CostModel;
            var resolvedDayRate = request.DayRate ?? project.DayRate;
            if (resolvedCostModel == CostModel.DayRate && resolvedDayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");

            if (request.ProjectStatus != null && !Enum.TryParse<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");

            if (request.SchoolCode != null && FindActiveSchoolByCode(context, request.SchoolCode) == null)
                errors.Add($"SchoolCode '{request.SchoolCode}' does not match any active School");

            if (!string.IsNullOrEmpty(request.ProjectManagerUsername) && FindUserByUsername(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            return errors;
        }

        /// <summary>
        /// Update the Project's core scalar fields. Caller is responsible
        /// for validating first. Doesn't touch Resourcing or Comments --
        /// those are additive actions with their own semantics via POST
        /// /api/projects/add.
        /// </summary>
        public UpdateProjectResponseDTO UpdateProject(PPMToolContext context, UpdateProjectRequestDTO request)
        {
            var project = FindProjectByRTP(context, request.RTP)!;

            if (request.Name != null) project.Name = request.Name;
            if (request.PI != null) project.PI = request.PI;
            if (request.SchoolCode != null) project.School = FindActiveSchoolByCode(context, request.SchoolCode)!;
            if (request.ProjectManagerUsername != null)
                project.ProjectManager = request.ProjectManagerUsername == ""
                    ? null
                    : FindUserByUsername(context, request.ProjectManagerUsername)!.Person!;
            if (request.Budget.HasValue) project.Budget = request.Budget.Value;
            if (request.CostModel != null) project.CostModel = Enum.Parse<CostModel>(request.CostModel);
            if (request.DayRate.HasValue) project.DayRate = request.DayRate.Value;
            if (project.CostModel != CostModel.DayRate) project.DayRate = 0; // same invariant Create() enforces
            if (request.ProjectStatus != null) project.ProjectStatus = Enum.Parse<ProjectStatus>(request.ProjectStatus);
            if (request.Description != null) project.Description = request.Description;
            if (request.RequestDocLink != null) project.RequestDocLink = request.RequestDocLink;
            if (request.ScrumProjectLink != null) project.ScrumProjectLink = request.ScrumProjectLink == "" ? null : request.ScrumProjectLink;

            var result = _projectService.Update(context, project);
            if (result < 0)
                throw new InvalidOperationException($"ProjectService.Update returned {result} (duplicate) despite passing ValidateProjectUpdate() -- possible race condition");

            var financialReferences = _financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = _settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new UpdateProjectResponseDTO(project.ProjectId);
        }

        /// <summary>
        /// Validate a POST /api/timesheets/add request without writing
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
                errors.Add($"Project {request.ProjectId} ('{project.Name}') has no InnateActivity code -- only projects created via POST /api/projects/add (or otherwise already linked) can receive imported timesheet entries");
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

        /// <summary>
        /// Validate a POST /api/workloadmodels/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateWorkloadModelChange(PPMToolContext context, ImportWorkloadModelChangeDTO request)
        {
            var errors = new List<string>();

            if (FindUserByUsername(context, request.Username)?.Person == null)
                errors.Add($"Username '{request.Username}' not found, or has no linked Person");

            if (request.Grade < 4 || request.Grade > 9)
                errors.Add($"Grade {request.Grade} is out of range (must be 4-9)");

            foreach (var (label, fte) in DutyFTEs(request))
            {
                if (fte < 0.0 || fte > 1.0) errors.Add($"{label} {fte} is out of range (must be 0.0-1.0)");
            }

            return errors;
        }

        /// <summary>
        /// Create or update the WorkloadModelChange for this person/date.
        /// Caller is responsible for validating first. Idempotent:
        /// re-importing the same (Username, ChangeDate) overwrites the
        /// existing change rather than creating a duplicate -- CapX itself
        /// rejects two changes on the same date for one person (see
        /// AddWorkloadModelChange.razor.cs), so overwrite is the only
        /// import semantics that doesn't risk violating that.
        /// </summary>
        public ImportWorkloadModelChangeResponseDTO CreateOrUpdateWorkloadModelChange(PPMToolContext context, ImportWorkloadModelChangeDTO request)
        {
            var person = FindUserByUsername(context, request.Username)!.Person!;

            var change = person.WorkloadModelChanges.FirstOrDefault(c => c.ChangeDate == request.ChangeDate);
            var created = change == null;
            if (change == null)
            {
                change = new WorkloadModelChange { Person = person };
                context.WorkloadModelChanges.Add(change);
            }

            change.ChangeDate = request.ChangeDate;
            change.Grade = request.Grade;
            change.ProjectWorkFTE = request.ProjectWorkFTE;
            change.BusinessAsUsualFTE = request.BusinessAsUsualFTE;
            change.PersonalDevelopmentFTE = request.PersonalDevelopmentFTE;
            change.StaffManagementFTE = request.StaffManagementFTE;
            change.ArchitectureFTE = request.ArchitectureFTE;
            change.ServiceManagementFTE = request.ServiceManagementFTE; // setter derives ProjectAndServiceManagementFTE
            change.ProjectManagementFTE = request.ProjectManagementFTE; // setter derives ProjectAndServiceManagementFTE
            change.Notes = request.Notes;
            context.SaveChangesWithRetry();

            return new ImportWorkloadModelChangeResponseDTO(change.WorkloadModelChangeId, created);
        }

        /// <summary>
        /// Validate a POST /api/people/add request without writing anything.
        /// </summary>
        public List<string> ValidatePerson(PPMToolContext context, ImportPersonDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name is required");

            if (request.FTE < 0.0 || request.FTE > 1.0)
                errors.Add($"FTE {request.FTE} is out of range (must be 0.0-1.0)");

            if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
                errors.Add("EndDate cannot be before StartDate");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                // Same duplicate checks Pages/AddPerson.razor.cs runs via
                // PersonService.Add -- probe with an unsaved Person so
                // ShortName is derived the same way (Person.Name's setter)
                // rather than re-implementing GetInitials() here.
                var probe = new Person { Name = request.Name.Trim() };
                if (_personService.DuplicateDetected(context, probe))
                    errors.Add($"A Person named '{request.Name}' already exists");
                else if (_personService.DuplicateInitialsDetected(context, probe))
                    errors.Add($"A Person with initials '{probe.ShortName}' already exists");
            }

            return errors;
        }

        /// <summary>
        /// Create the Person. Caller is responsible for validating first.
        /// </summary>
        public ImportPersonResponseDTO CreatePerson(PPMToolContext context, ImportPersonDTO request)
        {
            var person = new Person
            {
                Name = request.Name.Trim(),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                FTE = request.FTE,
            };
            _personService.Add(context, person);

            return new ImportPersonResponseDTO(person.PersonId, person.ShortName);
        }

        /// <summary>
        /// Validate a PUT /api/people/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidatePersonUpdate(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            var errors = new List<string>();

            var person = _personService.GetById(context, request.PersonId);
            if (person == null)
            {
                errors.Add($"PersonId {request.PersonId} does not exist");
                return errors;
            }

            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name cannot be blank");

            if (request.FTE.HasValue && (request.FTE.Value < 0.0 || request.FTE.Value > 1.0))
                errors.Add($"FTE {request.FTE} is out of range (must be 0.0-1.0)");

            var resolvedStart = request.StartDate ?? person.StartDate;
            var resolvedEnd = request.EndDate ?? person.EndDate;
            if (resolvedEnd.HasValue && resolvedEnd.Value < resolvedStart)
                errors.Add("EndDate cannot be before StartDate");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                var probe = new Person { PersonId = person.PersonId, Name = request.Name.Trim() };
                if (_personService.DuplicateDetected(context, probe))
                    errors.Add($"A different Person named '{request.Name}' already exists");
                else if (_personService.DuplicateInitialsDetected(context, probe))
                    errors.Add($"A different Person with initials '{probe.ShortName}' already exists");
            }

            return errors;
        }

        /// <summary>
        /// Update the Person's Name, StartDate, EndDate, and/or FTE. Caller
        /// is responsible for validating first. See UpdatePersonRequestDTO
        /// remarks -- EndDate can only be set, not cleared, here.
        /// </summary>
        public ImportPersonResponseDTO UpdatePerson(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            var person = _personService.GetById(context, request.PersonId)!;

            if (request.Name != null) person.Name = request.Name.Trim(); // setter also re-derives ShortName
            if (request.StartDate.HasValue) person.StartDate = request.StartDate.Value;
            if (request.EndDate.HasValue) person.EndDate = request.EndDate.Value;
            if (request.FTE.HasValue) person.FTE = request.FTE.Value;

            var result = _personService.Update(context, person);
            if (result < 0)
                throw new InvalidOperationException($"PersonService.Update returned {result} (duplicate) despite passing ValidatePersonUpdate() -- possible race condition");

            // Mirrors AddPerson.razor.cs's own edit flow -- a renamed Person may have a
            // linked User (this endpoint isn't restricted to the bare, login-less People
            // POST /api/people/add creates), whose display name would otherwise go stale.
            if (request.Name != null)
                _userService.UpdateDisplayName(context, person);

            return new ImportPersonResponseDTO(person.PersonId, person.ShortName);
        }

        /// <summary>
        /// Validate a POST /api/users/add request without writing anything.
        /// </summary>
        public List<string> ValidateUser(PPMToolContext context, ImportUserDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.CASUserName))
                errors.Add("CASUserName is required");
            if (string.IsNullOrWhiteSpace(request.EmailAddress))
                errors.Add("EmailAddress is required");

            Person? person = null;
            if (request.PersonId.HasValue)
            {
                person = _personService.GetById(context, request.PersonId.Value);
                if (person == null)
                    errors.Add($"PersonId {request.PersonId} does not exist");
            }
            else if (string.IsNullOrWhiteSpace(request.Name))
            {
                errors.Add("Name is required when PersonId is not given");
            }

            if (!string.IsNullOrWhiteSpace(request.RoleType) && !Enum.TryParse<RoleType>(request.RoleType, out _))
                errors.Add($"RoleType '{request.RoleType}' is not a valid value");

            if (!string.IsNullOrWhiteSpace(request.CASUserName))
            {
                var probe = new User { CASUserName = request.CASUserName.Trim(), Name = person?.Name ?? request.Name?.Trim() ?? "" };
                if (_userService.DuplicateDetected(context, probe))
                    errors.Add($"A User named '{request.CASUserName}' already exists");
            }

            return errors;
        }

        /// <summary>
        /// Create a User, optionally linked to an existing Person. Caller
        /// is responsible for validating first.
        /// </summary>
        public ImportUserResponseDTO CreateUser(PPMToolContext context, ImportUserDTO request)
        {
            var user = new User
            {
                CASUserName = request.CASUserName.Trim(),
                EmailAddress = request.EmailAddress.Trim(),
                Name = request.Name?.Trim() ?? "", // overwritten by the Person setter below if PersonId was given
                RoleType = string.IsNullOrWhiteSpace(request.RoleType) ? RoleType.None : Enum.Parse<RoleType>(request.RoleType),
            };
            if (request.PersonId.HasValue)
                user.Person = _personService.GetById(context, request.PersonId.Value); // Name re-set from Person.Name by the setter
            _userService.Add(context, user);

            return new ImportUserResponseDTO(user.UserId);
        }

        /// <summary>
        /// Validate a POST /api/projects/notes/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateNotesImport(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var errors = new List<string>();

            if (FindProjectByRTP(context, request.RTP) == null)
                errors.Add($"RTP {request.RTP} does not match any Project");

            if (request.Comments.Count == 0)
                errors.Add("Comments must contain at least one entry");

            if (request.Comments.Count > 0 && FindUserByUsername(context, FallbackAuthorUsername) == null)
                errors.Add($"Fallback author User '{FallbackAuthorUsername}' does not exist -- create it (POST /api/users/add) before importing comments");

            return errors;
        }

        /// <summary>
        /// Add Comments as Notes to an existing Project. Caller is
        /// responsible for validating first. Same author-resolution/
        /// fallback logic as Create()'s own Comments handling -- kept as a
        /// small local duplicate rather than a shared private helper,
        /// since Create()'s version is entangled with the rest of that
        /// method's single SaveChangesWithRetry() at the end.
        /// </summary>
        public ImportNotesResponseDTO AddNotes(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var project = FindProjectByRTP(context, request.RTP)!;
            var fallbackAuthor = FindUserByUsername(context, FallbackAuthorUsername);

            var notesCreated = 0;
            foreach (var c in request.Comments)
            {
                var author = string.IsNullOrWhiteSpace(c.AuthorUsername)
                    ? null
                    : FindUserByUsername(context, c.AuthorUsername);
                author ??= fallbackAuthor!; // validated to exist in ValidateNotesImport

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
            context.SaveChangesWithRetry();

            return new ImportNotesResponseDTO(project.ProjectId, notesCreated);
        }

        private static IEnumerable<(string Label, double FTE)> DutyFTEs(ImportWorkloadModelChangeDTO request)
        {
            yield return ("ProjectWorkFTE", request.ProjectWorkFTE);
            yield return ("BusinessAsUsualFTE", request.BusinessAsUsualFTE);
            yield return ("PersonalDevelopmentFTE", request.PersonalDevelopmentFTE);
            yield return ("StaffManagementFTE", request.StaffManagementFTE);
            yield return ("ArchitectureFTE", request.ArchitectureFTE);
            yield return ("ServiceManagementFTE", request.ServiceManagementFTE);
            yield return ("ProjectManagementFTE", request.ProjectManagementFTE);
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

        private static Faculty? FindFacultyByCode(PPMToolContext context, string code) =>
            context.Faculties
                .FirstOrDefault(f => f.Code.Trim().ToLower() == code.Trim().ToLower());

        // Unlike FindActiveSchoolByCode, not filtered to IsActive -- update needs to
        // find (and potentially reactivate) an inactive School too.
        private static School? FindSchoolByCode(PPMToolContext context, string code) =>
            context.Schools
                .Include(s => s.Faculty)
                .FirstOrDefault(s => s.Code.Trim().ToLower() == code.Trim().ToLower());

        // Reuses ProjectService.GetAll()'s own Include chain rather than assembling a
        // partial one here -- UpdateProject's call to project.UpdateProjectMetaData needs
        // the same full graph (SubTasks/AssignedResources/Person/WorkloadModelChanges,
        // FundingSources, etc.) that method's NRE-prone dependencies read directly (see
        // the ProjectService.GetAll() include list, and the class of bug noted on
        // SchoolService.GetAllActive() elsewhere in this codebase).
        private Project? FindProjectByRTP(PPMToolContext context, int rtp) =>
            _projectService.GetAll(context).FirstOrDefault(p => p.RTP == rtp);
    }
}
