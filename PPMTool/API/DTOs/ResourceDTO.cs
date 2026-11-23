// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a resource suitable for API data exports.
    /// </summary>
    /// <param name="ResourceId">The unique resource ID.</param>
    /// <param name="SubTaskId">The ID of the sub-task the resource belongs to.</param>
    /// <param name="PersonId">The ID of the person assigned to the resource.</param>
    /// <param name="FundedFromFundingSourceId">The ID of the funding source funding the resource, if present.</param>
    /// <param name="AssignmentFTE">The assignment FTE.</param>
    /// <param name="BilledFTE">The billed FTE.</param>
    /// <param name="PlannedCost">The planned cost.</param>
    /// <param name="PersonName">The name of the person assigned.</param>
    /// <param name="Usernames">Usernames (CASUserName) of every User linked to the person, in UserId order; empty if they have none.</param>
    /// <param name="IsProvisional">Whether the assignment is provisional.</param>
    public sealed record ResourceDTO(
        int ResourceId,
        int SubTaskId,
        int PersonId,
        int? FundedFromFundingSourceId,
        double AssignmentFTE,
        double BilledFTE,
        double PlannedCost,
        string PersonName,
        IReadOnlyList<string> Usernames,
        bool IsProvisional
    );

    /// <summary>
    /// Request body for POST /api/resources/add. Adds assignments to an
    /// existing Task. A person already assigned to the Task is rejected rather
    /// than duplicated: use PUT /api/resources/update to change their
    /// assignment. Not allowed on a fixed-work Task, whose dates CapX
    /// recalculates from the FTE assigned to it.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/subtasks/getAll</param>
    /// <param name="Resourcing">At least one assignment</param>
    public sealed record WriteResourcesDTO(
        int SubTaskId,
        IReadOnlyList<WriteResourceDTO> Resourcing
    );

    /// <summary>
    /// One assignment to add to an existing Task. Name the person by exactly
    /// one of Username or PersonId; PersonId reaches a Person with no User,
    /// such as one created by POST /api/people/add. An assignment has no dates
    /// of its own: its period is its Task's.
    /// Kept separate from <see cref="ResourceDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="PersonId">Id of a Person, with or without a User</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="IsProvisional">Defaults to true</param>
    public sealed record WriteResourceDTO(
        string? Username,
        int? PersonId,
        double AssignmentFTE,
        bool? IsProvisional
    );

    /// <summary>Response for added assignments.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record WriteResourcesResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/resources/update. Identifies the
    /// assignment by ResourceId and changes the fields supplied. Setting
    /// AssignmentFTE to zero retracts the assignment; there is no DELETE.
    /// Moving it to another Task (NewSubTaskId) is how its period changes.
    /// Not allowed when either Task is fixed-work.
    /// </summary>
    /// <param name="ResourceId">From GET /api/resources/getAllForProject or GET /api/resources/getAll</param>
    /// <param name="AssignmentFTE">Zero or more, at most 3 decimal places</param>
    /// <param name="NewSubTaskId">Task to move the assignment to</param>
    /// <param name="IsProvisional"></param>
    public sealed record UpdateResourceRequestDTO(
        int ResourceId,
        double? AssignmentFTE,
        int? NewSubTaskId,
        bool? IsProvisional
    );

    /// <summary>Response for an updated assignment.</summary>
    /// <param name="ResourceId"></param>
    /// <param name="SubTaskId">The Task the assignment is now on</param>
    public sealed record UpdateResourceResponseDTO(
        int ResourceId,
        int SubTaskId
    );
}
