// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.API.Helpers;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Financial reference endpoint methods.
    /// </summary>
    public static class FinancialReferences
    {
        /// <summary>
        /// Get all financial references.
        /// </summary>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FinancialReferenceDTO>))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetAllFinancialReferencesAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http)
        {
            try
            {
                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);

                if (!canAccess)
                {
                    logger.LogWarning("API: GetAllFinancialReferences: Caller does not have permission to access financial reference data.");
                    return Results.Unauthorized();
                }

                var financialReferences = await context.FinancialReferences
                    .OrderBy(x => x.FinancialYear)
                    .ToListAsync();

                var financialReferenceDtos = financialReferences
                    .Select(x => new FinancialReferenceDTO(
                        FinancialYear: x.FinancialYear,
                        Grade41Costs: x.Grade41Costs,
                        Grade51Costs: x.Grade51Costs,
                        Grade55Costs: x.Grade55Costs,
                        Grade65Costs: x.Grade65Costs,
                        Grade71Costs: x.Grade71Costs,
                        Grade75Costs: x.Grade75Costs
                    ))
                    .ToList();

                logger.LogInformation(
                    "API: GetAllFinancialReferences: Returned {Count} financial reference records.",
                    financialReferenceDtos.Count);

                return Results.Json(financialReferenceDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "API: GetAllFinancialReferences: error");

                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Get one financial reference by financial reference year.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <param name="http"></param>
        /// <param name="financialYear">The financial reference year.</param>
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FinancialReferenceDTO))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public static async Task<IResult> GetFinancialReferenceByFinancialYearAsync(
            PPMToolContext context,
            ILogger logger,
            HttpContext http,
            [FromQuery] int? financialYear)
        {
            try
            {
                if (!financialYear.HasValue || financialYear.Value <= 0)
                {
                    logger.LogWarning(
                        "API: GetFinancialReferenceByFinancialYear: Invalid financial year {FinancialYear}",
                        financialYear);

                    return Results.BadRequest("Financial year must be greater than zero.");
                }

                var caller = GeneralHelpers.GetCurrentUser(http);
                var canAccess = GeneralHelpers.IsSuperUserOrManager(caller);

                if (!canAccess)
                {
                    logger.LogWarning("API: GetFinancialReferenceByFinancialYear: Caller does not have permission to access financial reference data.");
                    return Results.Unauthorized();
                }

                var financialReference = await context.FinancialReferences
                    .FirstOrDefaultAsync(
                        x => x.FinancialReferenceId ==
                            financialYear.Value);

                if (financialReference == null)
                {
                    logger.LogWarning(
                        "API: GetFinancialReferenceByFinancialYear: No financial reference found for financial year {FinancialYear}",
                        financialYear);

                    return Results.NotFound();
                }

                var dto = new FinancialReferenceDTO(
                    FinancialYear: financialReference.FinancialYear,
                    Grade41Costs: financialReference.Grade41Costs,
                    Grade51Costs: financialReference.Grade51Costs,
                    Grade55Costs: financialReference.Grade55Costs,
                    Grade65Costs: financialReference.Grade65Costs,
                    Grade71Costs: financialReference.Grade71Costs,
                    Grade75Costs: financialReference.Grade75Costs
                );

                logger.LogInformation(
                    "API: GetFinancialReferenceByFinancialYear: Returned financial reference record for financial year {FinancialYear}",
                    financialYear);

                return Results.Json(dto);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "API: GetFinancialReferenceByFinancialYear: error");

                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}