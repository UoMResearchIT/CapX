// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Request body for POST /api/people/add. Creates a Person with no User,
    /// for someone who never logs in, such as a former member of staff whose
    /// work is being recorded. A duplicate name or initials is rejected, as
    /// in the UI.
    /// </summary>
    /// <param name="Name">Full name; the initials are derived from it</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">Null if still in post</param>
    /// <param name="FTE">The post's FTE (Person.FTE), 0.0-1.0. Part-time working belongs in a workload model change instead.</param>
    public sealed record ImportPersonDTO(
        string Name,
        DateTime StartDate,
        DateTime? EndDate,
        double FTE
    );

    /// <summary>Response for a created Person.</summary>
    /// <param name="PersonId"></param>
    /// <param name="ShortName">The derived initials</param>
    public sealed record ImportPersonResponseDTO(
        int PersonId,
        string ShortName
    );

    /// <summary>
    /// Request body for PUT /api/people/update. Identifies the Person by
    /// PersonId and changes the fields supplied. EndDate and the line manager
    /// can be set but not cleared; clear them in the UI. Name the line manager
    /// by at most one of LineManagerPersonId or LineManagerUsername;
    /// LineManagerPersonId reaches a Person with no User, whom the UI's own
    /// line-manager picker can't offer.
    /// </summary>
    /// <param name="PersonId"></param>
    /// <param name="Name">Full name; the initials are re-derived</param>
    /// <param name="StartDate">Not after the start of any Task the person is assigned to</param>
    /// <param name="EndDate"></param>
    /// <param name="FTE">0.0-1.0</param>
    /// <param name="LineManagerPersonId">Id of the line manager's Person; not the Person being updated</param>
    /// <param name="LineManagerUsername">Username of the line manager, which must have a linked Person</param>
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
