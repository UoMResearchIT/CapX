// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a financial reference suitable for
    /// API data exports.
    /// </summary>
    /// <param name="FinancialReferenceId">ID of the entity in the DB</param>
    /// <param name="FinancialYear">The financial year represented by the reference.</param>
    /// <param name="Values">KVP list of the financial reference values in the reference if any exist.</param>
    public sealed record FinancialReferenceDTO(
        int FinancialReferenceId,
        int FinancialYear,
        IDictionary<string, float> Values
    );
}