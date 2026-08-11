// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Backs the Superuser-only POST /api/import/* endpoints
    /// (see PPMTool.API.Endpoints.Import), gated behind
    /// SettingType.ImportApiEnabled. Currently just Faculty/School
    /// creation; more endpoints land here as they're built (towards #1310).
    /// </summary>
    public class ImportService
    {
        private readonly FacultyService _facultyService;
        private readonly SchoolService _schoolService;

        public ImportService(FacultyService facultyService, SchoolService schoolService)
        {
            _facultyService = facultyService;
            _schoolService = schoolService;
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
    }
}
