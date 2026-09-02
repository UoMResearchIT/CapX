// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;
using PPMTool.Services;

namespace PPMTool.API.Endpoints;

/// <summary>
/// People endpoint methods.
/// </summary>
public static class People
{
    /// <summary>
    /// Get all people.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<PersonDTO>))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetAllPeopleAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http)
    {
        try
        {
            var caller = GeneralHelpers.GetCurrentUser(http);
            var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);
            if (!canAccess)
            {
                logger.LogWarning("API: GetAllPeople: Caller does not have permission to access people data.");
                return Results.Unauthorized();
            }

            var people = await context.People
                .Include(x => x.LineManager)
                .OrderBy(x => x.Name)
                .ToListAsync();

            // Person has no reverse nav to User -- look usernames up the other way round.
            var usernamesByPersonId = await context.Users
                .Where(u => u.Person != null)
                .Select(u => new { u.Person!.PersonId, u.CASUserName })
                .ToDictionaryAsync(x => x.PersonId, x => x.CASUserName);

            var personDtos = people.Select(x => new PersonDTO(
                PersonId: x.PersonId,
                Name: x.Name,
                ShortName: x.ShortName,
                PostFTE: x.FTE,
                StartDate: x.StartDate,
                EndDate: x.EndDate,
                LineManagerId: x.LineManager?.PersonId,
                LineManagerName: x.LineManager?.Name,
                Username: usernamesByPersonId.GetValueOrDefault(x.PersonId)
            )).ToList();

            logger.LogInformation("API: GetAllPeople: Returned {Count} people records.", personDtos.Count);
            return Results.Json(personDtos);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetAllPeople: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get one person by person ID.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="logger"></param>
    /// <param name="http"></param>
    /// <param name="personId">The person ID.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PersonDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static async Task<IResult> GetPersonByIdAsync(
        PPMToolContext context,
        ILogger logger,
        HttpContext http,
        [FromQuery] int? personId)
    {
        try
        {
            if (!personId.HasValue || personId.Value <= 0)
            {
                logger.LogWarning("API: GetPersonById: Invalid personId {PersonId}", personId);
                return Results.BadRequest("Person ID must be greater than zero.");
            }

            var canAccess = GeneralHelpers.IsSuperUserOrManagerOrSelf(context, http, personId.Value);
            if (!canAccess)
            {
                logger.LogWarning("API: GetPersonById: Caller does not have permission to access this person's data.");
                return Results.Unauthorized();
            }

            var person = await context.People
                .Include(x => x.LineManager)
                .FirstOrDefaultAsync(x => x.PersonId == personId);

            if (person == null)
            {
                logger.LogWarning("API: GetPersonById: No person found for personId {PersonId}", personId);
                return Results.NotFound();
            }

            var username = await context.Users
                .Where(u => u.Person != null && u.Person.PersonId == person.PersonId)
                .Select(u => u.CASUserName)
                .FirstOrDefaultAsync();

            var dto = new PersonDTO(
                PersonId: person.PersonId,
                Name: person.Name,
                ShortName: person.ShortName,
                PostFTE: person.FTE,
                StartDate: person.StartDate,
                EndDate: person.EndDate,
                LineManagerId: person.LineManager?.PersonId,
                LineManagerName: person.LineManager?.Name,
                Username: username
            );

            logger.LogInformation("API: GetPersonById: Returned person record for personId {PersonId}", personId);
            return Results.Json(dto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: GetPersonById: error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Create a bare Person record, with no linked User/Access-Control
    /// account. Superuser-only write access, gated behind
    /// SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ImportPersonResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult CreatePerson(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] ImportPersonDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "People.CreatePerson");
            if (!allowed) return gateResult!;

            var errors = importService.ValidatePerson(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: People: person validation failed for '{Name}': {Errors}", request.Name, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.CreatePerson(context, request);
            logger.LogInformation(
                "API: People: created Person {PersonId} '{Name}' ({ShortName}) by {User}",
                result.PersonId, request.Name, result.ShortName, caller!.Name);
            return Results.Created($"/api/people/getById?personId={result.PersonId}", result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: People: error creating person '{Name}'", request.Name);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Update an existing bare Person's Name, StartDate, EndDate, and/or
    /// FTE. Identified by PersonId. Superuser-only write access, gated
    /// behind SettingType.ImportApiEnabled -- see UoMResearchIT/CapX#1310.
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ImportPersonResponseDTO))]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(ImportErrorDTO))]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public static IResult UpdatePerson(
        PPMToolContext context,
        ImportService importService,
        SettingsService settingsService,
        ILogger logger,
        HttpContext http,
        [FromBody] UpdatePersonRequestDTO request)
    {
        try
        {
            var (allowed, caller, gateResult) = GeneralHelpers.CheckImportApiGate(settingsService, http, logger, "People.UpdatePerson");
            if (!allowed) return gateResult!;

            var errors = importService.ValidatePersonUpdate(context, request);
            if (errors.Count > 0)
            {
                logger.LogWarning("API: People: person update validation failed for PersonId {PersonId}: {Errors}", request.PersonId, string.Join("; ", errors));
                return Results.BadRequest(new ImportErrorDTO(errors));
            }

            var result = importService.UpdatePerson(context, request);
            logger.LogInformation(
                "API: People: updated Person {PersonId} ('{ShortName}') by {User}",
                result.PersonId, result.ShortName, caller!.Name);
            return Results.Ok(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "API: People: error updating person {PersonId}", request.PersonId);
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }
}
