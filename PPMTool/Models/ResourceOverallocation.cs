// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Models
{
    /// <summary>
    /// Describes the first date on which a person is assigned above their contractual FTE.
    /// </summary>
    public sealed record ResourceOverallocation(
        int PersonId,
        string PersonName,
        DateTime Date,
        double AssignedFTE,
        double ContractualFTE);
}