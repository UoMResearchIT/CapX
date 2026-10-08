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
    /// Service for API import operations that validate and mutate Person entities.
    /// </summary>
    public class ImportPersonService
    {
        private readonly PersonService personService;
        private readonly UserService userService;

        public ImportPersonService(
            PersonService personService,
            UserService userService)
        {
            this.personService = personService;
            this.userService = userService;
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
                StartDate = AsUnspecifiedKind(request.StartDate),
                EndDate = request.EndDate.HasValue ? AsUnspecifiedKind(request.EndDate.Value) : null,
                FTE = request.FTE,
            };
            personService.Add(context, person);

            return new ImportPersonResponseDTO(person.PersonId, person.ShortName);
        }

        /// <summary>
        /// Validate a PUT /api/people/update request without writing anything.
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

            if (request.StartDate.HasValue)
            {
                var newStart = AsUnspecifiedKind(request.StartDate.Value);
                var earlierTasks = context.Resources
                    .Where(r => r.Person.PersonId == person.PersonId && r.SubTask.StartDate < newStart)
                    .Select(r => new { r.SubTask.SubTaskId, r.SubTask.Name, r.SubTask.StartDate, r.SubTask.OwningProject.RTP })
                    .ToList();
                foreach (var t in earlierTasks)
                    errors.Add($"StartDate {newStart:yyyy-MM-dd} is after the start date {t.StartDate:yyyy-MM-dd} of task '{t.Name}' (SubTaskId {t.SubTaskId}, RTP {t.RTP}), which this person is assigned to");
            }

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
        /// Update the Person's Name, StartDate, EndDate, FTE and/or LineManager.
        /// </summary>
        public ImportPersonResponseDTO UpdatePerson(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            var person = personService.GetById(context, request.PersonId)!;

            if (request.Name != null) person.Name = request.Name.Trim();
            if (request.StartDate.HasValue) person.StartDate = AsUnspecifiedKind(request.StartDate.Value);
            if (request.EndDate.HasValue) person.EndDate = AsUnspecifiedKind(request.EndDate.Value);
            if (request.FTE.HasValue) person.FTE = request.FTE.Value;

            var lineManager = ResolveLineManager(context, request);
            if (lineManager != null) person.LineManager = lineManager;

            var result = personService.Update(context, person);
            if (result < 0)
                throw new InvalidOperationException($"PersonService.Update returned {result} (duplicate) despite passing ValidatePersonUpdate() -- possible race condition");

            if (request.Name != null)
                userService.UpdateDisplayName(context, person);

            return new ImportPersonResponseDTO(person.PersonId, person.ShortName);
        }

        /// <summary>
        /// Validate the LineManagerPersonId and LineManagerUsername fields in an UpdatePersonRequestDTO.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <param name="subjectPersonId"></param>
        /// <returns></returns>
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
                errors.Add("A Person cannot be their own line manager");
            }

            return errors;
        }

        /// <summary>
        /// Resolve the line manager Person from an UpdatePersonRequestDTO, using either LineManagerPersonId or LineManagerUsername.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        private Person? ResolveLineManager(PPMToolContext context, UpdatePersonRequestDTO request)
        {
            if (request.LineManagerPersonId.HasValue)
                return personService.GetById(context, request.LineManagerPersonId.Value);

            if (!string.IsNullOrWhiteSpace(request.LineManagerUsername))
                return FindUserByUsername(context, request.LineManagerUsername)?.Person;

            return null;
        }

        /// <summary>
        /// Finds a User by username with linked Person and WorkloadModelChanges.
        /// </summary>
        /// <param name="context">The database context.</param>
        /// <param name="username">The username to search for.</param>
        private static User? FindUserByUsername(PPMToolContext context, string username) =>
            context.Users
                .Include(u => u.Person)
                    .ThenInclude(p => p!.WorkloadModelChanges)
                .FirstOrDefault(u => u.CASUserName.Trim().ToLower() == username.Trim().ToLower());

        /// <summary>
        /// Ensure a DateTime is of Kind Unspecified, converting if necessary. This is important for storing dates in the database without timezone information.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static DateTime AsUnspecifiedKind(DateTime dt) =>
            dt.Kind == DateTimeKind.Unspecified ? dt : DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);
    }
}
