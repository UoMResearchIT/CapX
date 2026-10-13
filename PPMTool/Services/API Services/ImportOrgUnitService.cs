// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for DI injection that can manipulate Faculty and School entities as part of API calls.
    /// </summary>
    public class ImportOrgUnitService
    {
        private readonly FacultyService facultyService;
        private readonly SchoolService schoolService;

        public ImportOrgUnitService(
            FacultyService facultyService,
            SchoolService schoolService)
        {
            this.facultyService = facultyService;
            this.schoolService = schoolService;
        }

        /// <summary>
        /// Creates a new faculty and its associated schools in a single transaction.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming faculty creation request.</param>
        /// <returns>Identifier payload for the created faculty and school identifiers.</returns>
        /// <exception cref="InvalidOperationException">Thrown when persistence reports a duplicate despite successful validation.</exception>
        public ImportFacultyResponseDTO CreateFaculty(PPMToolContext context, ImportFacultyRequestDTO request)
        {
            using var transaction = context.Database.BeginTransaction();

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

            transaction.Commit();
            return new ImportFacultyResponseDTO(faculty.FacultyId, schoolIds);
        }

        /// <summary>
        /// Updates an existing faculty.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming faculty update request.</param>
        /// <returns>Identifier payload for the updated faculty.</returns>
        /// <exception cref="InvalidOperationException">Thrown when persistence reports a duplicate despite successful validation.</exception>
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
        /// Creates a new school and associates it with an existing faculty.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming school creation request.</param>
        /// <returns>Identifier payload for the created school and its faculty.</returns>
        /// <exception cref="InvalidOperationException">Thrown when persistence reports a duplicate despite successful validation.</exception>
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
        /// Updates an existing school, including optional faculty reassignment.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming school update request.</param>
        /// <returns>Identifier payload for the updated school and its faculty.</returns>
        /// <exception cref="InvalidOperationException">Thrown when persistence reports a duplicate despite successful validation.</exception>
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
        /// Finds a faculty by code.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="code">Faculty code to resolve.</param>
        /// <returns>The matching faculty, or <c>null</c> when none exists.</returns>
        private static Faculty? FindFacultyByCode(PPMToolContext context, string code) =>
            context.Faculties
                .FirstOrDefault(f => f.Code.Trim().ToLower() == code.Trim().ToLower());

        /// <summary>
        /// Finds a school by code.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="code">School code to resolve.</param>
        /// <returns>The matching school, or <c>null</c> when none exists.</returns>
        private static School? FindSchoolByCode(PPMToolContext context, string code) =>
            context.Schools
                .Include(s => s.Faculty)
                .FirstOrDefault(s => s.Code.Trim().ToLower() == code.Trim().ToLower());

        /// <summary>
        /// Validates a faculty creation request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming faculty creation request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
        public List<string> ValidateFaculty(PPMToolContext context, ImportFacultyRequestDTO request)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(request.Name)) errors.Add("Name is required");
            if (string.IsNullOrWhiteSpace(request.Code)) errors.Add("Code is required");
            if (!string.IsNullOrWhiteSpace(request.Name) && !string.IsNullOrWhiteSpace(request.Code)
                && facultyService.DuplicateDetected(context, new Faculty { Name = request.Name, Code = request.Code }))
                errors.Add($"A Faculty named '{request.Name}' or with code '{request.Code}' already exists");

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
        /// Validates a faculty update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming faculty update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
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
            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name cannot be blank");
            if (request.NewCode != null && string.IsNullOrWhiteSpace(request.NewCode))
                errors.Add("NewCode cannot be blank");

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
        /// Validates a school creation request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming school creation request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
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
        /// Validates a school update request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming school update request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
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
            if (request.Name != null && string.IsNullOrWhiteSpace(request.Name))
                errors.Add("Name cannot be blank");
            if (request.NewCode != null && string.IsNullOrWhiteSpace(request.NewCode))
                errors.Add("NewCode cannot be blank");

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
    }
}
