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

        // Every request body that names a Project by RTP declares it as a
        // non-nullable int, so an omitted RTP binds to 0 rather than failing.
        private const string rtpRequired = "RTP is required and must be greater than zero";

        private readonly FacultyService facultyService;
        private readonly SchoolService schoolService;
        private readonly ProjectService projectService;
        private readonly SubTaskService subTaskService;
        private readonly NoteService noteService;
        private readonly FinancialReferenceService financialReferenceService;
        private readonly SettingsService settingsService;
        private readonly TimesheetService timesheetService;
        private readonly PersonService personService;
        private readonly UserService userService;

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
            this.facultyService = facultyService;
            this.schoolService = schoolService;
            this.projectService = projectService;
            this.subTaskService = subTaskService;
            this.noteService = noteService;
            this.financialReferenceService = financialReferenceService;
            this.settingsService = settingsService;
            this.timesheetService = timesheetService;
            this.personService = personService;
            this.userService = userService;
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
                && facultyService.DuplicateDetected(context, new Faculty { Name = request.Name, Code = request.Code }))
                errors.Add($"A Faculty named '{request.Name}' or with code '{request.Code}' already exists");

            // SchoolService.DuplicateDetected rejects a repeated name as well as a
            // repeated code within one Faculty, so both are checked here: otherwise
            // the second School fails only after the Faculty has been written.
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in request.Schools ?? Array.Empty<ImportSchoolDTO>())
            {
                if (string.IsNullOrWhiteSpace(s.Name)) errors.Add($"School Name is required (code '{s.Code}')");
                else if (!seenNames.Add(s.Name.Trim().ToLowerInvariant()))
                    errors.Add($"Duplicate School name '{s.Name}' within this request");
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
            var facultyId = facultyService.Add(context, faculty);
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
                var schoolId = schoolService.Add(context, school);
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
            if (facultyService.DuplicateDetected(context, probe))
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

            var result = facultyService.Update(context, faculty);
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
                    && schoolService.DuplicateDetected(context, new School { Name = request.Name, Code = request.Code, Faculty = faculty }))
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
            var schoolId = schoolService.Add(context, school);
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
                if (schoolService.DuplicateDetected(context, probe))
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

            var result = schoolService.Update(context, school);
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
            else if (projectService.DuplicateDetected(context, new Project { Name = request.Name }))
                errors.Add($"A Project named '{request.Name}' already exists");
            // RTP is a non-nullable int on the DTO, so an omitted field binds to 0.
            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (context.Projects.Any(p => p.RTP == request.RTP))
                errors.Add($"A Project with RTP {request.RTP} already exists");
            if (string.IsNullOrWhiteSpace(request.PI)) errors.Add("PI is required");
            if (string.IsNullOrWhiteSpace(request.RequestDocLink)) errors.Add("RequestDocLink is required");
            if (!Enum.TryParse<CostModel>(request.CostModel, out var costModel))
                errors.Add($"CostModel '{request.CostModel}' is not a valid value");
            else if (costModel == CostModel.DayRate && request.DayRate <= 0)
                errors.Add("DayRate must be greater than zero when CostModel is 'DayRate'");
            if (!Enum.TryParse<ProjectStatus>(request.ProjectStatus, out _))
                errors.Add($"ProjectStatus '{request.ProjectStatus}' is not a valid value");
            if (request.ManagementEndDate.Date < request.ManagementStartDate.Date)
                errors.Add("ManagementEndDate must be on or after ManagementStartDate");

            var school = FindActiveSchoolByCode(context, request.SchoolCode);
            if (school == null)
                errors.Add($"SchoolCode '{request.SchoolCode}' does not match any active School");

            if (!string.IsNullOrWhiteSpace(request.ProjectManagerUsername) && FindUserByUsername(context, request.ProjectManagerUsername)?.Person == null)
                errors.Add($"ProjectManagerUsername '{request.ProjectManagerUsername}' not found, or has no linked Person");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = FindUserByUsername(context, r.Username)?.Person;
                if (person == null)
                    errors.Add($"Resourcing username '{r.Username}' not found, or has no linked Person");
                assignments.Add((person, r.Username, r.AssignmentFTE));
            }
            // Create puts create-time resourcing on its Delivery task: ProjectWork,
            // starting on ManagementStartDate.
            errors.AddRange(ValidateAssignments(context, assignments, Duty.ProjectWork, request.ManagementStartDate));

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
            var projectId = projectService.Add(context, project); // commits, assigns ProjectId
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
                StartDate = AsUnspecifiedKind(request.ManagementStartDate),
                EndDate = AsUnspecifiedKind(request.ManagementEndDate),
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
                    StartDate = AsUnspecifiedKind(request.ManagementStartDate),
                    EndDate = AsUnspecifiedKind(request.ManagementEndDate),
                };
                delivery.Schedule();
                subTaskService.Add(context, delivery);

                foreach (var r in resourcing)
                {
                    var person = FindUserByUsername(context, r.Username)!.Person!;
                    var resource = new Resource
                    {
                        Person = person,
                        SubTask = delivery,
                        AssignmentFTE = r.AssignmentFTE,
                        IsProvisional = true, // migrated data -- flag for PM review, not treated as confirmed
                    };
                    context.Resources.Add(resource);
                    resourcesCreated++;
                }
                Reschedule(context, delivery);
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

                var createdDate = AsUnspecifiedKind(c.CreatedDate);
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
        /// Validate a PUT /api/projects/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateProjectUpdate(PPMToolContext context, UpdateProjectRequestDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
            {
                errors.Add(rtpRequired);
                return errors;
            }
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
                else if (projectService.DuplicateDetected(context, new Project { ProjectId = project.ProjectId, Name = request.Name }))
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
        /// Validate a GET /api/tasks/getAll request without reading
        /// anything else. An empty list for a valid RTP is a normal
        /// result (a Project with no non-Leadership tasks yet), not an
        /// error.
        /// </summary>
        public List<string> ValidateTasksGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (FindProjectByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// All SubTasks for one Project. Caller is responsible for
        /// validating the RTP first (ValidateTasksGet).
        /// </summary>
        public List<TaskDTO> GetTasksForRTP(PPMToolContext context, int rtp)
        {
            var project = FindProjectByRTP(context, rtp)!;

            return project.SubTasks
                .OrderBy(t => t.StartDate)
                .Select(t => new TaskDTO(
                    t.SubTaskId, rtp, t.Name, t.TaskDuty.ToString(),
                    t.StartDate, t.EndDate, t.Demand, t.OriginalDemand, t.UnmetDemand
                ))
                .ToList();
        }

        /// <summary>
        /// Validate a POST /api/tasks/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateTaskCreate(PPMToolContext context, ImportTaskDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
            {
                errors.Add(rtpRequired);
                return errors;
            }
            var project = FindProjectByRTP(context, request.RTP);
            if (project == null)
            {
                errors.Add($"RTP {request.RTP} does not match any Project");
                return errors;
            }

            if (string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name cannot be blank");

            Duty? duty = null;
            if (!Enum.TryParse<Duty>(request.TaskDuty, out var parsedDuty))
                errors.Add($"TaskDuty '{request.TaskDuty}' is not a valid value");
            else
                duty = parsedDuty;

            if (!string.IsNullOrWhiteSpace(request.Name) && duty != null && duty != Duty.ProjectAndServiceMgmt
                && project.SubTasks.Any(t => t.Name == request.Name))
                errors.Add($"A task named '{request.Name}' already exists on Project {request.RTP}");

            if (request.EndDate.Date < request.StartDate.Date)
                errors.Add("EndDate must be on or after StartDate");

            if (HasDigitsAfterThirdDecimalPlace(request.Demand))
                errors.Add("Demand cannot have digits after the third decimal place");
            else if (request.Demand <= 0)
                errors.Add("Demand must be greater than zero");

            var assignments = new List<(Person? Person, string Assignee, double FTE)>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = FindUserByUsername(context, r.Username)?.Person;
                if (person == null)
                    errors.Add($"Resourcing Username '{r.Username}' not found, or has no linked Person");
                assignments.Add((person, r.Username, r.AssignmentFTE));
            }
            // An unparseable TaskDuty is already reported above; the other rules still apply.
            errors.AddRange(ValidateAssignments(context, assignments, duty ?? Duty.ProjectWork, request.StartDate));

            return errors;
        }

        /// <summary>
        /// Create the Task (+ Resourcing). Caller is responsible for
        /// validating first. Mirrors ImportService.Create's own
        /// Leadership/Delivery task creation exactly (FixedDuration,
        /// fixed start/end), generalised to an arbitrary caller-supplied
        /// Name/Duty/dates/Demand.
        /// </summary>
        public ImportTaskResponseDTO CreateTask(PPMToolContext context, ImportTaskDTO request)
        {
            var project = FindProjectByRTP(context, request.RTP)!;
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
                // SubTasks.StartDate/EndDate map to Postgres "timestamp without
                // time zone" columns (see FixDateTimeTimezone migration) -- the
                // same class of bug already fixed once on Note.CreatedDate/
                // EditedDate (Npgsql hard-rejects Kind=Utc for that column
                // type). A caller passing a full "...Z" ISO-8601 timestamp
                // rather than a bare date would otherwise crash the request.
                StartDate = AsUnspecifiedKind(request.StartDate),
                EndDate = AsUnspecifiedKind(request.EndDate),
            };
            task.Schedule();
            subTaskService.Add(context, task);

            var created = new List<Resource>();
            foreach (var r in request.Resourcing ?? Array.Empty<ImportResourcingDTO>())
            {
                var person = FindUserByUsername(context, r.Username)!.Person!;
                var resource = new Resource
                {
                    Person = person,
                    SubTask = task,
                    AssignmentFTE = r.AssignmentFTE,
                    IsProvisional = true, // migrated data -- flag for PM review, not treated as confirmed
                };
                context.Resources.Add(resource);
                created.Add(resource);
            }
            // The task was scheduled before any resource existed, so it is scheduled
            // again now they do: that is what gives each one its PlannedWorkHours.
            Reschedule(context, task);

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new ImportTaskResponseDTO(task.SubTaskId, created.Count);
        }

        /// <summary>
        /// Validate a PUT /api/tasks/update request without writing
        /// anything.
        /// </summary>
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
                if (!Enum.TryParse<Duty>(request.TaskDuty, out var parsed))
                    errors.Add($"TaskDuty '{request.TaskDuty}' is not a valid value");
                else
                    newDuty = parsed;
            }
            var resolvedDuty = newDuty ?? task.TaskDuty;

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

            // Starting the task before one of its assignees starts is what Schedule()
            // refuses, so it is refused here rather than failing on save.
            foreach (var r in task.AssignedResources)
            {
                if (AssigneeStartsTooLate(r.Person, resolvedStart) is { } tooLate)
                    errors.Add(tooLate);
            }

            if (request.Demand.HasValue)
            {
                if (HasDigitsAfterThirdDecimalPlace(request.Demand.Value))
                    errors.Add("Demand cannot have digits after the third decimal place");
                else if (request.Demand.Value < 0)
                    errors.Add("Demand cannot be negative");
            }

            return errors;
        }

        /// <summary>
        /// Update the Task's core fields. Caller is responsible for
        /// validating first. Only fields actually supplied are touched;
        /// Demand updates the current Demand only, never OriginalDemand
        /// (see UpdateTaskRequestDTO remarks). Doesn't touch Resourcing --
        /// additive, via POST /api/tasks/add or the existing UI.
        /// </summary>
        public UpdateTaskResponseDTO UpdateTask(PPMToolContext context, UpdateTaskRequestDTO request)
        {
            var (project, task) = FindProjectAndTaskById(context, request.SubTaskId)!.Value;

            if (request.Name != null) task.Name = request.Name;
            if (request.TaskDuty != null) task.TaskDuty = Enum.Parse<Duty>(request.TaskDuty);
            if (request.StartDate.HasValue) task.StartDate = AsUnspecifiedKind(request.StartDate.Value);
            if (request.EndDate.HasValue) task.EndDate = AsUnspecifiedKind(request.EndDate.Value);
            if (request.Demand.HasValue) task.Demand = request.Demand.Value;
            Reschedule(context, task);
            subTaskService.Update(context, task);

            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            context.SaveChangesWithRetry();

            return new UpdateTaskResponseDTO(task.SubTaskId);
        }

        /// <summary>
        /// Validate a GET /api/tasks/resourcing/getAll request without
        /// reading anything.
        /// </summary>
        public List<string> ValidateTaskResourcingGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (FindProjectByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// Every resourcing assignment across one Project's SubTasks.
        /// Caller is responsible for validating the RTP first
        /// (ValidateTaskResourcingGet).
        ///
        /// This is the read side the write endpoints need to be usable
        /// safely: without it a caller can't tell which assignments
        /// already exist, and an import either duplicates work or has to
        /// track what it sent out-of-band.
        /// </summary>
        public List<TaskResourceDTO> GetResourcingForRTP(PPMToolContext context, int rtp)
        {
            var project = FindProjectByRTP(context, rtp)!;

            // Person has no reverse nav to User, so usernames are looked up the
            // other way round -- and a Person can legitimately back more than one
            // User (no unique constraint on Users.PersonId), so dedupe
            // deterministically on the lowest UserId rather than letting
            // ToDictionary throw. Same shape, and the same reason, as
            // People.GetAllPeople's own lookup.
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
        /// Validate a POST /api/tasks/resourcing/add request without
        /// writing anything.
        /// </summary>
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

        // The per-assignment rules every resourcing path shares, so create-time
        // resourcing (projects/add, tasks/add) can't accept what tasks/resourcing/add
        // refuses. Resolving each Person stays with the caller, since the
        // create-time DTO is username-only; a row whose Person didn't resolve has
        // already been reported, and gets only the FTE checks. existingTask is the
        // task being added to, when it already exists.
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
                if (HasDigitsAfterThirdDecimalPlace(fte))
                    errors.Add($"AssignmentFTE for '{assignee}' cannot have digits after the third decimal place");
                else if (fte <= 0)
                    errors.Add($"AssignmentFTE for '{assignee}' must be greater than zero");

                if (person == null) continue;

                // Only managers can be assigned to a leadership-duty task -- the same
                // rule AddTask.razor enforces.
                if (managerPersonIds != null && !managerPersonIds.Contains(person.PersonId))
                    errors.Add($"Only managers can be assigned to leadership tasks ('{assignee}')");

                if (AssigneeStartsTooLate(person, taskStart) is { } tooLate)
                    errors.Add(tooLate);

                // Reject rather than merge or duplicate: a second assignment for the
                // same person would double this task's resourcing (on a re-run, for
                // an existing task), and the caller has GET + update to do it
                // deliberately.
                if (existingTask != null && existingTask.AssignedResources.Any(existing => existing.Person.PersonId == person.PersonId))
                    errors.Add($"'{assignee}' is already assigned to SubTaskId {existingTask.SubTaskId} -- use PUT /api/tasks/resourcing/update to change the existing assignment");
                if (!seenPersonIds.Add(person.PersonId))
                    errors.Add($"'{assignee}' appears more than once in this request");
            }

            return errors;
        }

        /// <summary>
        /// Add resourcing to an existing Task. Caller is responsible for
        /// validating first.
        /// </summary>
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
                    // Imported resourcing is provisional unless the caller says
                    // otherwise, matching ImportService.Create -- migrated data is
                    // flagged for PM review rather than presented as confirmed.
                    IsProvisional = r.IsProvisional ?? true,
                };
                context.Resources.Add(resource);
                created.Add(resource);
            }

            Reschedule(context, task);
            RecalculateProject(context, project);

            return new ImportTaskResourcingResponseDTO(task.SubTaskId, created.Count);
        }

        /// <summary>
        /// Validate a PUT /api/tasks/resourcing/update request without
        /// writing anything.
        /// </summary>
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
                if (HasDigitsAfterThirdDecimalPlace(request.AssignmentFTE.Value))
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

            // Re-checked against the task the assignment will end up on, not the one
            // it started on: moving a non-manager onto a leadership task has to fail
            // the same way assigning one there directly does.
            if (targetTask.TaskDuty == Duty.ProjectAndServiceMgmt
                && !userService.GetAllManagerPersonId(context).Contains(resource.Person.PersonId))
                errors.Add($"Only managers can be assigned to leadership tasks ('{resource.Person.Name}')");

            if (AssigneeStartsTooLate(resource.Person, targetTask.StartDate) is { } tooLate)
                errors.Add(tooLate);

            return errors;
        }

        /// <summary>
        /// Update an existing resourcing assignment. Caller is
        /// responsible for validating first. Only fields actually
        /// supplied are touched.
        /// </summary>
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

                // A move across projects changes the cost picture on both sides, so
                // the one being left has to be recalculated too, not just the one
                // being joined.
                if (targetProject.RTP != project.RTP) movedFromProject = project;
                project = targetProject;
            }

            context.Resources.Update(resource);
            if (landedOn != currentTask)
                Reschedule(context, currentTask, landedOn);
            else
                Reschedule(context, currentTask);
            if (movedFromProject != null) RecalculateProject(context, movedFromProject, commit: false);
            RecalculateProject(context, project);

            return new UpdateTaskResourcingResponseDTO(resource.ResourceId, landedOn.SubTaskId);
        }

        // Every API path that changes a task's assignments has to do what the UI's
        // resource grid (AddTask.razor.cs) does on save: Schedule() is the only
        // thing that sets each Resource's PlannedWorkHours, which DayRate costs
        // are computed from, and the Data Dashboard sums SubTask.UnmetDemand as
        // stored. Both read AssignedResources, so change detection runs first:
        // whether EF has fixed a newly added or moved Resource into (or out of)
        // the collection yet otherwise depends on when it last detected changes.
        // Validation already rejects everything Schedule() can refuse here (an
        // assignee who starts after the task), so an error is a bug, not a 400.
        private static void Reschedule(PPMToolContext context, params SubTask[] tasks)
        {
            context.ChangeTracker.DetectChanges();
            foreach (var task in tasks)
            {
                var error = task.Schedule();
                if (error != null)
                    throw new InvalidOperationException($"SubTask.Schedule() failed for SubTaskId {task.SubTaskId} despite passing validation: {error}");
                task.UpdateUnmetDemand();
            }
        }

        // The one condition under which Schedule() refuses a task's assignments:
        // someone assigned who starts after the task does. The UI refuses the same.
        private static string? AssigneeStartsTooLate(Person person, DateTime taskStart) =>
            person.StartDate > taskStart
                ? $"'{person.Name}' does not start until {person.StartDate:yyyy-MM-dd}, after the task's start date {taskStart:yyyy-MM-dd}"
                : null;

        /// <summary>
        /// Validate a POST /api/timesheets/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var errors = new List<string>();

            var hasUsername = !string.IsNullOrWhiteSpace(request.Username);
            var hasPersonId = request.PersonId.HasValue;
            if (hasUsername == hasPersonId)
                errors.Add("Exactly one of Username or PersonId must be supplied");
            else if (hasUsername && FindUserByUsername(context, request.Username!)?.Person == null)
                errors.Add($"Username '{request.Username}' not found, or has no linked Person");
            else if (hasPersonId && personService.GetById(context, request.PersonId!.Value) == null)
                errors.Add($"PersonId {request.PersonId} does not exist");

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
        /// Idempotent: re-importing the same (Person, WeekStartDate,
        /// TaskName) overwrites the existing entry's hours rather than
        /// accumulating on top of them.
        /// </summary>
        public ImportTimesheetResponseDTO CreateOrUpdateTimesheetEntry(PPMToolContext context, ImportTimesheetEntryDTO request)
        {
            var person = !string.IsNullOrWhiteSpace(request.Username)
                ? FindUserByUsername(context, request.Username)!.Person!
                : personService.GetById(context, request.PersonId!.Value);
            var project = FindProjectWithInnateActivity(context, request.ProjectId)!;
            var task = project.InnateActivity!.Tasks.First(t => t.TaskName.Trim().Equals(request.TaskName.Trim(), StringComparison.OrdinalIgnoreCase));
            var weekStartDate = AsUnspecifiedKind(request.WeekStartDate);

            var timesheet = context.Timesheets
                .Include(t => t.TimesheetEntries)
                .FirstOrDefault(t => t.Owner.PersonId == person.PersonId && t.StartDate == weekStartDate);

            var timesheetCreated = timesheet == null;
            if (timesheet == null)
            {
                timesheet = new Timesheet
                {
                    Owner = person,
                    StartDate = weekStartDate,
                    Status = TimesheetStatus.Approved, // historical actuals -- not pending review
                    DateStatusChanged = DateTime.Now,
                };
                var timesheetId = timesheetService.Add(context, timesheet);
                if (timesheetId < 0)
                    throw new InvalidOperationException($"TimesheetService.Add returned {timesheetId} (duplicate) despite passing ValidateTimesheetEntry() -- possible race condition");
            }

            var entry = timesheet.TimesheetEntries.FirstOrDefault(e => e.InnateCodeTaskId == task.InnateCodeTaskId);
            var entryCreated = entry == null;
            if (entry == null)
            {
                entry = new TimesheetEntry { Timesheet = timesheet, InnateCodeTask = task };
                timesheetService.AddEntry(context, entry, commitChanges: false);
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

        /// <summary>
        /// Validate a PUT /api/timesheets/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateTimesheetEntryUpdate(PPMToolContext context, UpdateTimesheetEntryRequestDTO request)
        {
            var errors = new List<string>();

            var entry = FindTimesheetEntryById(context, request.TimesheetEntryId);
            if (entry == null)
            {
                errors.Add($"TimesheetEntryId {request.TimesheetEntryId} does not exist");
                return errors;
            }

            foreach (var (label, hours) in DayHours(request))
            {
                if (hours.HasValue && hours.Value < 0) errors.Add($"{label} cannot be negative");
            }

            if (request.NewTaskName != null)
            {
                var innateCode = entry.InnateCodeTask.InnateCode;
                var newTask = innateCode.Tasks.FirstOrDefault(t => t.TaskName.Trim().Equals(request.NewTaskName.Trim(), StringComparison.OrdinalIgnoreCase));
                if (newTask == null)
                    errors.Add($"NewTaskName '{request.NewTaskName}' does not match any InnateCodeTask under this entry's InnateActivity ('{innateCode.ActivityName}'); available: {string.Join(", ", innateCode.Tasks.Select(t => t.TaskName))}");
                else if (newTask.InnateCodeTaskId != entry.InnateCodeTaskId
                         && entry.Timesheet.TimesheetEntries.Any(e => e.InnateCodeTaskId == newTask.InnateCodeTaskId && e.TimesheetEntryId != entry.TimesheetEntryId))
                    errors.Add($"Timesheet {entry.TimesheetId} already has a separate entry for task '{newTask.TaskName}' -- can't move this entry there too");
            }

            return errors;
        }

        /// <summary>
        /// Update an existing TimesheetEntry's day hours and/or task.
        /// Caller is responsible for validating first. Only fields actually
        /// supplied in the request are touched.
        /// </summary>
        public UpdateTimesheetEntryResponseDTO UpdateTimesheetEntry(PPMToolContext context, UpdateTimesheetEntryRequestDTO request)
        {
            var entry = FindTimesheetEntryById(context, request.TimesheetEntryId)!;

            if (request.NewTaskName != null)
                entry.InnateCodeTask = entry.InnateCodeTask.InnateCode.Tasks.First(t => t.TaskName.Trim().Equals(request.NewTaskName.Trim(), StringComparison.OrdinalIgnoreCase));

            if (request.MondayHours.HasValue) entry.MondayHours = request.MondayHours.Value;
            if (request.TuesdayHours.HasValue) entry.TuesdayHours = request.TuesdayHours.Value;
            if (request.WednesdayHours.HasValue) entry.WednesdayHours = request.WednesdayHours.Value;
            if (request.ThursdayHours.HasValue) entry.ThursdayHours = request.ThursdayHours.Value;
            if (request.FridayHours.HasValue) entry.FridayHours = request.FridayHours.Value;
            if (request.SaturdayHours.HasValue) entry.SaturdayHours = request.SaturdayHours.Value;
            if (request.SundayHours.HasValue) entry.SundayHours = request.SundayHours.Value;
            entry.UpdateTotalHours();

            timesheetService.UpdateEntry(context, entry, commitChanges: false);
            context.SaveChangesWithRetry();

            return new UpdateTimesheetEntryResponseDTO(entry.TimesheetEntryId, entry.TotalHours);
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

        private static IEnumerable<(string Label, double? Hours)> DayHours(UpdateTimesheetEntryRequestDTO request)
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

            var changeDate = AsUnspecifiedKind(request.ChangeDate);
            var change = person.WorkloadModelChanges.FirstOrDefault(c => c.ChangeDate == changeDate);
            var created = change == null;
            if (change == null)
            {
                change = new WorkloadModelChange { Person = person };
                context.WorkloadModelChanges.Add(change);
            }

            change.ChangeDate = changeDate;
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
                if (personService.DuplicateDetected(context, probe))
                    errors.Add($"A Person named '{request.Name}' already exists");
                else if (personService.DuplicateInitialsDetected(context, probe))
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
                // Same Npgsql Kind bug as Note/SubTask -- People.StartDate/EndDate is
                // also "timestamp without time zone".
                StartDate = AsUnspecifiedKind(request.StartDate),
                EndDate = request.EndDate.HasValue ? AsUnspecifiedKind(request.EndDate.Value) : null,
                FTE = request.FTE,
            };
            personService.Add(context, person);

            return new ImportPersonResponseDTO(person.PersonId, person.ShortName);
        }

        /// <summary>
        /// Validate a PUT /api/people/update request without writing
        /// anything.
        /// </summary>
        public List<string> ValidatePersonUpdate(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            var errors = new List<string>();

            var person = personService.GetById(context, request.PersonId);
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
                if (personService.DuplicateDetected(context, probe))
                    errors.Add($"A different Person named '{request.Name}' already exists");
                else if (personService.DuplicateInitialsDetected(context, probe))
                    errors.Add($"A different Person with initials '{probe.ShortName}' already exists");
            }

            errors.AddRange(ValidateLineManagerFields(context, request, person.PersonId));

            return errors;
        }

        /// <summary>
        /// Shared validation for the optional line-manager fields on
        /// PUT /api/people/update. Returns an empty list when neither is
        /// supplied, since line manager is optional on an update.
        /// </summary>
        private List<string> ValidateLineManagerFields(PPMToolContext context, UpdatePersonRequestDTO request, int subjectPersonId)
        {
            var errors = new List<string>();

            var hasId = request.LineManagerPersonId.HasValue;
            var hasUsername = !string.IsNullOrWhiteSpace(request.LineManagerUsername);

            if (hasId && hasUsername)
            {
                errors.Add("Supply at most one of LineManagerPersonId or LineManagerUsername, not both");
                return errors;
            }

            if (!hasId && !hasUsername) return errors;

            var manager = ResolveLineManager(context, request);
            if (manager == null)
            {
                errors.Add(hasId
                    ? $"LineManagerPersonId {request.LineManagerPersonId} does not exist"
                    : $"LineManagerUsername '{request.LineManagerUsername}' not found, or has no linked Person");
            }
            else if (manager.PersonId == subjectPersonId)
            {
                // Mirrors AddPerson.razor.cs, which excludes the person being edited from
                // its own line-manager dropdown.
                errors.Add("A Person cannot be their own line manager");
            }

            return errors;
        }

        /// <summary>
        /// Resolve the line manager named by whichever of the two fields was
        /// supplied, or null if neither was supplied or it did not resolve.
        /// </summary>
        private Person? ResolveLineManager(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            if (request.LineManagerPersonId.HasValue)
                return personService.GetById(context, request.LineManagerPersonId.Value);

            if (!string.IsNullOrWhiteSpace(request.LineManagerUsername))
                return FindUserByUsername(context, request.LineManagerUsername)?.Person;

            return null;
        }

        /// <summary>
        /// Update the Person's Name, StartDate, EndDate, FTE and/or
        /// LineManager. Caller is responsible for validating first. See
        /// UpdatePersonRequestDTO remarks -- EndDate and LineManager can
        /// only be set, not cleared, here.
        /// </summary>
        public ImportPersonResponseDTO UpdatePerson(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            var person = personService.GetById(context, request.PersonId)!;

            if (request.Name != null) person.Name = request.Name.Trim(); // setter also re-derives ShortName
            if (request.StartDate.HasValue) person.StartDate = AsUnspecifiedKind(request.StartDate.Value);
            if (request.EndDate.HasValue) person.EndDate = AsUnspecifiedKind(request.EndDate.Value);
            if (request.FTE.HasValue) person.FTE = request.FTE.Value;

            var lineManager = ResolveLineManager(context, request);
            if (lineManager != null) person.LineManager = lineManager;

            var result = personService.Update(context, person);
            if (result < 0)
                throw new InvalidOperationException($"PersonService.Update returned {result} (duplicate) despite passing ValidatePersonUpdate() -- possible race condition");

            // Mirrors AddPerson.razor.cs's own edit flow -- a renamed Person may have a
            // linked User (this endpoint isn't restricted to the bare, login-less People
            // POST /api/people/add creates), whose display name would otherwise go stale.
            if (request.Name != null)
                userService.UpdateDisplayName(context, person);

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
                person = personService.GetById(context, request.PersonId.Value);
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
                if (userService.DuplicateDetected(context, probe))
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
                user.Person = personService.GetById(context, request.PersonId.Value); // Name re-set from Person.Name by the setter
            userService.Add(context, user);

            return new ImportUserResponseDTO(user.UserId);
        }

        /// <summary>
        /// Validate a POST /api/projects/notes/add request without writing
        /// anything.
        /// </summary>
        public List<string> ValidateNotesImport(PPMToolContext context, ImportNotesRequestDTO request)
        {
            var errors = new List<string>();

            if (request.RTP <= 0)
                errors.Add(rtpRequired);
            else if (FindProjectByRTP(context, request.RTP) == null)
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

                var createdDate = AsUnspecifiedKind(c.CreatedDate);
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
        /// Validate a GET /api/projects/notes/getAll request. Just RTP
        /// existence -- an existing Project with zero Notes is a normal
        /// empty-list result, not an error.
        /// </summary>
        public List<string> ValidateNotesGet(PPMToolContext context, int rtp)
        {
            var errors = new List<string>();
            if (FindProjectByRTP(context, rtp) == null)
                errors.Add($"RTP {rtp} does not match any Project");
            return errors;
        }

        /// <summary>
        /// All Notes for one Project, oldest first. Caller is responsible
        /// for validating the RTP first (ValidateNotesGet).
        /// </summary>
        public List<NoteDTO> GetNotesForRTP(PPMToolContext context, int rtp)
        {
            var project = FindProjectByRTP(context, rtp)!;

            // Convert to a list before projecting to a DTO -- User.GetName()
            // isn't translatable to SQL, same reason GetAllProjectsAsync's
            // own Select() runs after ToListAsync() rather than as part of
            // the query itself.
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
        /// Validate a PUT /api/projects/notes/update request without
        /// writing anything.
        /// </summary>
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
        /// Update an existing Note's content. Caller is responsible for
        /// validating first. Sets Editor/EditedDate the same way a real
        /// UI edit would, rather than silently rewriting history.
        /// </summary>
        public UpdateNoteResponseDTO UpdateNote(PPMToolContext context, UpdateNoteRequestDTO request, User caller)
        {
            var note = context.Notes.First(n => n.NoteId == request.NoteId);
            note.HtmlContent = request.HtmlContent;
            note.Editor = caller;
            note.EditedDate = AsUnspecifiedKind(DateTime.UtcNow);
            noteService.Update(context, note);
            context.SaveChangesWithRetry();

            return new UpdateNoteResponseDTO(note.NoteId);
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

        // Both include chains are required, not optional -- ValidateTimesheetEntryUpdate/
        // UpdateTimesheetEntry read entry.Timesheet.TimesheetEntries (to check for a
        // collision when moving to a different task) and entry.InnateCodeTask.InnateCode.Tasks
        // (to resolve NewTaskName), same class of bug as FindUserByUsername's
        // WorkloadModelChanges include above.
        private static TimesheetEntry? FindTimesheetEntryById(PPMToolContext context, int timesheetEntryId) =>
            context.TimesheetEntries
                .Include(e => e.Timesheet)
                    .ThenInclude(t => t.TimesheetEntries)
                .Include(e => e.InnateCodeTask)
                    .ThenInclude(t => t.InnateCode)
                        .ThenInclude(c => c.Tasks)
                .FirstOrDefault(e => e.TimesheetEntryId == timesheetEntryId);

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

        // Goes through ProjectService.GetByRTP rather than a query of its own:
        // UpdateProject's call to project.UpdateProjectMetaData needs the same full
        // graph (SubTasks/AssignedResources/Person/WorkloadModelChanges,
        // FundingSources, etc.) that ProjectService.GetAll() includes, and whose
        // NRE-prone dependencies read directly (see the class of bug noted on
        // SchoolService.GetAllActive() elsewhere in this codebase). GetByRTP currently
        // loads every project to find one; any optimisation there (#301) applies
        // here with no further change.
        private Project? FindProjectByRTP(PPMToolContext context, int rtp) =>
            projectService.GetByRTP(context, rtp);

        // Two queries rather than one Include chain on SubTasks directly: the caller
        // (ValidateTaskUpdate/UpdateTask) needs the fully-loaded owning Project too, for
        // IsUniqueTaskNameInProject and UpdateProjectMetaData -- both NRE-prone against a
        // partial graph, same class of bug as FindUserByUsername's WorkloadModelChanges
        // include above. Reusing FindProjectByRTP's own full Include chain for that is
        // simpler than assembling an equivalent one from the SubTask side, and EF's
        // identity map returns the same tracked SubTask instance either way.
        private (Project Project, SubTask Task)? FindProjectAndTaskById(PPMToolContext context, int subTaskId)
        {
            // Nullable, so "no such task" can't be confused with a project whose RTP is 0.
            var rtp = context.SubTasks
                .Where(t => t.SubTaskId == subTaskId)
                .Select(t => (int?)t.OwningProject.RTP)
                .FirstOrDefault();
            if (rtp == null) return null;

            var project = FindProjectByRTP(context, rtp.Value);
            var task = project?.SubTasks.FirstOrDefault(t => t.SubTaskId == subTaskId);
            return task == null ? null : (project!, task);
        }

        // Resolve the Person an assignment refers to, by exactly one of Username or
        // PersonId. Returns the errors rather than throwing so the validate pass can
        // report every bad row in one response, and is re-run (errors discarded) on
        // the write path so resolution logic lives in exactly one place.
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
                var person = FindUserByUsername(context, assignment.Username!)?.Person;
                if (person == null)
                    errors.Add($"Username '{assignment.Username}' not found, or has no linked Person");
                return (person, errors);
            }

            var byId = personService.GetById(context, assignment.PersonId!.Value);
            if (byId == null)
                errors.Add($"PersonId {assignment.PersonId} does not exist");
            return (byId, errors);
        }

        // Whichever identifier the caller actually supplied, for error messages --
        // reporting a null Username at someone who supplied a PersonId is noise.
        private static string DescribeAssignee(ImportResourceAssignmentDTO assignment) =>
            !string.IsNullOrWhiteSpace(assignment.Username)
                ? assignment.Username!
                : assignment.PersonId.HasValue ? $"PersonId {assignment.PersonId}" : "(no Username or PersonId)";

        // Same two-step as FindProjectAndTaskById, and for the same reason: callers
        // need the fully-loaded owning Project for UpdateProjectMetaData, and the
        // Resource/SubTask instances handed back are the tracked ones from that
        // graph, so mutating them is what gets saved.
        private (Project Project, SubTask Task, Resource Resource)? FindProjectAndResourceById(
            PPMToolContext context, int resourceId)
        {
            var rtp = context.Resources
                .Where(r => r.ResourceId == resourceId)
                .Select(r => (int?)r.SubTask.OwningProject.RTP)
                .FirstOrDefault();
            if (rtp == null) return null;

            var project = FindProjectByRTP(context, rtp.Value);
            var task = project?.SubTasks.FirstOrDefault(t => t.AssignedResources.Any(r => r.ResourceId == resourceId));
            var resource = task?.AssignedResources.FirstOrDefault(r => r.ResourceId == resourceId);
            return resource == null ? null : (project!, task!, resource);
        }

        // Resourcing changes feed straight into cost calculation, so every write
        // path has to re-run CapX's own engine rather than leaving the project's
        // stored costs stale. Same call the create paths make.
        private void RecalculateProject(PPMToolContext context, Project project, bool commit = true)
        {
            var financialReferences = financialReferenceService.GetAllOrDefault(context);
            var indirectsPercentage = settingsService.GetSetting(SettingType.BAUTopSliceFractionDefault, 0f);
            project.UpdateProjectMetaData(true, financialReferences, indirectsPercentage);
            if (commit) context.SaveChangesWithRetry();
        }

        // Same check AddTask.razor's HandleSubmit runs before saving (Demand/
        // OriginalDemand/AssignmentFTE) -- EF/Postgres would happily store more
        // precision, but the UI treats it as invalid, so the API rejects it too
        // rather than silently accepting data the UI itself would refuse.
        private static bool HasDigitsAfterThirdDecimalPlace(double number)
        {
            var truncated = Math.Truncate(number * 1000) / 1000;
            return number != truncated;
        }

        // Note.CreatedDate/EditedDate map to Postgres "timestamp without time
        // zone" columns. A DateTime deserialized from an ISO-8601 JSON value
        // ending in "Z" (as every real ImportCommentDTO.CreatedDate does)
        // carries Kind=Utc, which Npgsql now hard-rejects for that column
        // type ("Cannot write DateTime with Kind=UTC to PostgreSQL type
        // 'timestamp without time zone'") -- confirmed live, 2026-09-04, on
        // the first real notes/add call. Strip the Kind rather than convert
        // the value; the column has no timezone to convert to anyway.
        private static DateTime AsUnspecifiedKind(DateTime dt) =>
            DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
    }
}
