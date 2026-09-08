// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

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

            var personDtos = people.Select(x => new PersonDTO(
                PersonId: x.PersonId,
                Name: x.Name,
                ShortName: x.ShortName,
                PostFTE: x.FTE,
                StartDate: x.StartDate,
                EndDate: x.EndDate,
                LineManagerId: x.LineManager?.PersonId,
                LineManagerName: x.LineManager?.Name
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

            var dto = new PersonDTO(
                PersonId: person.PersonId,
                Name: person.Name,
                ShortName: person.ShortName,
                PostFTE: person.FTE,
                StartDate: person.StartDate,
                EndDate: person.EndDate,
                LineManagerId: person.LineManager?.PersonId,
                LineManagerName: person.LineManager?.Name
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
}
