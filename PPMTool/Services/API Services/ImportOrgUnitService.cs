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
        /// Creates a new Faculty and its associated Schools in a single transaction. If any part of the operation fails, the entire transaction is rolled back.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// Updates an existing Faculty's details. If the update fails due to a duplicate code, an InvalidOperationException is thrown.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// Creates a new School and associates it with an existing Faculty. If the creation fails due to a duplicate code, an InvalidOperationException is thrown.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// Updates an existing School's details, including its name, code, and associated Faculty. If the update fails due to a duplicate code, an InvalidOperationException is thrown.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
        /// Finds a Faculty entity by its code, ignoring case and whitespace. Returns null if no matching Faculty is found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private static Faculty? FindFacultyByCode(PPMToolContext context, string code) =>
            context.Faculties
                .FirstOrDefault(f => f.Code.Trim().ToLower() == code.Trim().ToLower());

        /// <summary>
        /// Finds a School entity by its code, ignoring case and whitespace. Returns null if no matching School is found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="code"></param>
        /// <returns></returns>
        private static School? FindSchoolByCode(PPMToolContext context, string code) =>
            context.Schools
                .Include(s => s.Faculty)
                .FirstOrDefault(s => s.Code.Trim().ToLower() == code.Trim().ToLower());

        /// <summary>
        /// Validates the given ImportFacultyRequestDTO and returns a list of error messages if any validation errors are found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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
        /// Validates the given UpdateFacultyRequestDTO and returns a list of error messages if any validation errors are found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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
        /// Validates the given ImportSchoolRequestDTO and returns a list of error messages if any validation errors are found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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
        /// Validates the given UpdateSchoolRequestDTO and returns a list of error messages if any validation errors are found.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
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
