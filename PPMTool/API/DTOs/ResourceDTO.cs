// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
    public sealed record ResourceDTO(
        int ResourceId,
        int SubTaskId,
        int PersonId,
        int? FundedFromFundingSourceId,
        double AssignmentFTE,
        double BilledFTE,
        double PlannedCost
    );
}
