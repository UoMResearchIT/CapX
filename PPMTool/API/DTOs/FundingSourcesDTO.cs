// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a funding source suitable for API data exports.
    /// </summary>
    /// <param name="FundingSourceId">The unique funding source ID.</param>
    /// <param name="ProjectId">The ID of the project the funding source belongs to.</param>
    /// <param name="AmountAvailable">The amount of funding available.</param>
    /// <param name="Description">The description of the funding source, if present.</param>
    /// <param name="AccountCode">The account code for the funding source, if present.</param>
    /// <param name="FundingSourceType">The funding source type as a string.</param>
    public sealed record FundingSourceDTO(
        int FundingSourceId,
        int ProjectId,
        double AmountAvailable,
        string Description,
        string AccountCode,
        string FundingSourceType
    );
}
