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
    /// <param name="Username">Username (CASUserName) of the Person's User, or null if they have none. Where a Person has more than one User, the one with the lowest UserId.</param>
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
        string? Username
    );
}
