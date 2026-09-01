// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a person suitable for API data exports.
    /// </summary>
    /// <param name="PersonId">The unique person ID.</param>
    /// <param name="Name">The full name of the person.</param>
    /// <param name="ShortName">The initials/short name of the person.</param>
    /// <param name="PostFTE">The contract FTE of the person.</param>
    /// <param name="StartDate">The start date for the person.</param>
    /// <param name="EndDate">The end date for the person, if they have left.</param>
    /// <param name="LineManagerId">The person ID of their line manager, if present.</param>
    /// <param name="LineManagerName">The name of their line manager, if present.</param>
    /// <param name="Username">CapX Access Control username (CASUserName) of the linked User, if any -- null if this Person has no login. Needed to target this person via the Superuser "/add" import endpoints, which resolve by username, not PersonId.</param>
    public sealed record PersonDTO(
        int PersonId,
        string Name,
        string ShortName,
        double PostFTE,
        DateTime StartDate,
        DateTime? EndDate,
        int? LineManagerId,
        string LineManagerName,
        string? Username
    );
}
