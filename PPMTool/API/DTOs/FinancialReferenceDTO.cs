// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a financial reference suitable for
    /// API data exports.
    /// </summary>
    /// <param name="FinancialYear">The financial year represented by the reference.</param>
    /// <param name="Grade41Costs">The costs associated with grade 4.1.</param>
    /// <param name="Grade51Costs">The costs associated with grade 5.1.</param>
    /// <param name="Grade55Costs">The costs associated with grade 5.5.</param>
    /// <param name="Grade65Costs">The costs associated with grade 6.5.</param>
    /// <param name="Grade71Costs">The costs associated with grade 7.1.</param>
    /// <param name="Grade75Costs">The costs associated with grade 7.5.</param>
    public sealed record FinancialReferenceDTO(
        int FinancialYear,
        double Grade41Costs,
        double Grade51Costs,
        double Grade55Costs,
        double Grade65Costs,
        double Grade71Costs,
        double Grade75Costs
    );
}