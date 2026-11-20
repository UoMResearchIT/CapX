// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a person suitable for API data exports.
    /// </summary>
    /// <param name="PersonId">The unique person ID.</param>
    /// <param name="EmployeeId">Employee ID</param>
    /// <param name="Name">The full name of the person.</param>
    /// <param name="ShortName">The initials/short name of the person.</param>
    /// <param name="PostFTE">The contract FTE of the person.</param>
    /// <param name="StartDate">The start date for the person.</param>
    /// <param name="EndDate">The end date for the person, if they have left.</param>
    /// <param name="LineManagerId">The person ID of their line manager, if present.</param>
    /// <param name="LineManagerName">The name of their line manager, if present.</param>
    /// <param name="Usernames">Usernames (CASUserName) of every User linked to the Person, in UserId order; empty if they have none.</param>
    public sealed record PersonDTO(
        int PersonId,
        string? EmployeeId,
        string Name,
        string ShortName,
        double PostFTE,
        DateOnly StartDate,
        DateOnly? EndDate,
        int? LineManagerId,
        string? LineManagerName,
        IReadOnlyList<string> Usernames
    );

    /// <summary>
    /// Request body for POST /api/people/add. Creates a Person with no User,
    /// for someone who never logs in, such as a former member of staff whose
    /// work is being recorded. A duplicate name or initials is rejected, as
    /// in the UI.
    /// Kept separate from <see cref="PersonDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="Name">Full name; the initials are derived from it</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">Null if still in post</param>
    /// <param name="FTE">The post's FTE (<see cref="PPMTool.Data.Entities.Person.FTE"/>), 0.0-1.0. Part-time working belongs in a workload model change instead.</param>
    public sealed record WritePersonDTO(
        string Name,
        DateTime StartDate,
        DateTime? EndDate,
        double FTE
    );

    /// <summary>Response for a created Person.</summary>
    /// <param name="PersonId"></param>
    /// <param name="ShortName">The derived initials</param>
    public sealed record WritePersonResponseDTO(
        int PersonId,
        string ShortName
    );

    /// <summary>
    /// Request body for PUT /api/people/update. Identifies the Person by
    /// PersonId and changes the fields supplied. EndDate and the line manager
    /// can be set but not cleared; clear them in the UI. Name the line manager
    /// by at most one of LineManagerPersonId or LineManagerUsername; a request
    /// with both is refused. LineManagerPersonId is the primary form and always
    /// works, including for a Person with no User, whom the UI's own
    /// line-manager picker can't offer. LineManagerUsername is a convenience for
    /// imports keyed on logins.
    /// </summary>
    /// <param name="PersonId"></param>
    /// <param name="Name">Full name; the initials are re-derived</param>
    /// <param name="StartDate">Not after the start of any Task the person is assigned to</param>
    /// <param name="EndDate"></param>
    /// <param name="FTE">0.0-1.0</param>
    /// <param name="LineManagerPersonId">Id of the line manager's Person, the primary form; not the Person being updated</param>
    /// <param name="LineManagerUsername">Username (CASUserName) of a User linked to the line manager's Person. A username that matches no User, or whose User has no linked Person, fails validation with a 400.</param>
    public sealed record UpdatePersonRequestDTO(
        int PersonId,
        string? Name,
        DateTime? StartDate,
        DateTime? EndDate,
        double? FTE,
        int? LineManagerPersonId = null,
        string? LineManagerUsername = null
    );
}
