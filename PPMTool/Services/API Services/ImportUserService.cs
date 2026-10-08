// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Services
{
    /// <summary>
    /// Service for API import operations that validate and create User entities.
    /// </summary>
    public class ImportUserService
    {
        private readonly PersonService personService;
        private readonly UserService userService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImportUserService"/> class.
        /// </summary>
        /// <param name="personService">Service used to resolve people linked to imported users.</param>
        /// <param name="userService">Service used to validate and persist users.</param>
        public ImportUserService(PersonService personService, UserService userService)
        {
            this.personService = personService;
            this.userService = userService;
        }

        /// <summary>
        /// Validates a user import request without writing to the database.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming user import request.</param>
        /// <returns>Validation errors, or an empty list when valid.</returns>
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

            if (!string.IsNullOrWhiteSpace(request.RoleType) && !ImportHelper.TryParseDefined<RoleType>(request.RoleType, out _))
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
        /// Creates a user from an already-validated import request.
        /// </summary>
        /// <param name="context">Database context.</param>
        /// <param name="request">Incoming user import request.</param>
        /// <returns>The created user identifier payload.</returns>
        public ImportUserResponseDTO CreateUser(PPMToolContext context, ImportUserDTO request)
        {
            var user = new User
            {
                CASUserName = request.CASUserName.Trim(),
                EmailAddress = request.EmailAddress.Trim(),
                Name = request.Name?.Trim() ?? "",
                RoleType = string.IsNullOrWhiteSpace(request.RoleType) ? RoleType.None : Enum.Parse<RoleType>(request.RoleType),
            };
            if (request.PersonId.HasValue)
                user.Person = personService.GetById(context, request.PersonId.Value);
            userService.Add(context, user);

            return new ImportUserResponseDTO(user.UserId);
        }
    }
}
