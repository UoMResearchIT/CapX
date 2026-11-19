// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// User endpoint methods.
/// </summary>
public static class Users
{
    /// <summary>
    /// Create a User, bare or linked to an existing Person, as a Superuser can
    /// on the Manage Access page. Signing in only finds an existing User.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportUserResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateUser(
        PPMToolContext context,
        ImportUserService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportUserDTO request)
    {
        return GeneralHelpers.ExecuteImportWrite(
            settingsService,
            http,
            logger,
            $"{nameof(Users)}.{nameof(CreateUser)}",
            validate: _ => importService.ValidateUser(context, request),
            logValidationFailure: errors => logger.LogWarning("API: Users: user validation failed for '{CASUserName}': {Errors}", request.CASUserName, string.Join("; ", errors)),
            execute: caller =>
            {
                var result = importService.CreateUser(context, request);
                logger.LogInformation(
                    "API: Users: created User {UserId} '{CASUserName}' by {User}",
                    result.UserId, request.CASUserName, caller.Name);
                // There is no GET /api/users/getById to point a Location header at.
                return Results.Json(result, statusCode: StatusCodes.Status201Created);
            },
            logException: ex => logger.LogError(ex, "API: Users: error creating user '{CASUserName}'", request.CASUserName));
    }
}
