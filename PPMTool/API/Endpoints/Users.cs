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
/// User endpoint methods. CreateUser is Superuser-only write access,
/// gated behind SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
/// </summary>
public static class Users
{
    /// <summary>
    /// Create an Access Control User directly -- a bare User, or one
    /// linked to an existing Person via PersonId. Not a substitute for
    /// normal SSO first-login (how someone gets their own first CapX
    /// account); for direct provisioning instead.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportUserResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreateUser(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportUserDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "Users.CreateUser");
            if (!allowed) return gateResult!;

            var errors = importService.ValidateUser(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: Users: user validation failed for '{CASUserName}': {Errors}", request.CASUserName, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreateUser(context, request);
            logger.LogInformation(
                "API: Users: created User {UserId} '{CASUserName}' by {User}",
                result.UserId, request.CASUserName, caller!.Name);
            // No GET /api/users/getById exists yet
            // no Location header to point at, just the response body.
            return Results.Json(result, statusCode: StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: Users: error creating user '{CASUserName}'", request.CASUserName);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
