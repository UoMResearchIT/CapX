// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data;
using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;
using PPMTool.Models;

namespace PPMTool.Helpers
{
    /// <summary>
    /// Helper to process finance data.sources into a form usable by other components
    /// </summary>
    public abstract class FinanceHelper
    {
        /// <summary>
        /// Calculate the funds requested for a project
        /// </summary>
        /// <param name="context"></param>
        /// <param name="costModel"></param>
        /// <param name="subTasks"></param>
        /// <param name="fundingSources"></param>
        /// <param name="requestedFromInvoices"></param>
        /// <param name="receivedFromPayments"></param>
        /// <returns></returns>
        internal static TransactionBreakdown ComputeTransactionBreakdown(
            PPMToolContext context,
            CostModel costModel,
            IEnumerable<SubTask> subTasks,
            IEnumerable<FundingSource> fundingSources,
            double requestedFromInvoices,
            double receivedFromPayments)
        {
            // DA is just the sum of the DA funding sources
            var da = fundingSources
                .Where(x => x.FundingSourceType == FundingSourceType.DA)
                .RoundedSum(x => x.AmountAvailable, 2);

            // DI is based on the salary costs and assignments of the resources
            var di = subTasks
                .Where(x => x.TaskDuty != Duty.ProjectAndServiceMgmt)
                .SelectMany(x => x.AssignedResources)
                .Where(x => x.FundedFrom?.FundingSourceType == FundingSourceType.DI)
                .RoundedSum(x => x.PlannedCost, 2);

            // Add to these totals the leadership costs that are funded through DI
            var leadershipCostsThatAreDI = subTasks
                .Where(x => x.TaskDuty == Duty.ProjectAndServiceMgmt && x.AssignedResources
                    .Any(x => x.FundedFrom?.FundingSourceType == FundingSourceType.DI))
                .RoundedSum(x => x.PlannedCost, 2);
            if (costModel.HasLeadership())
            {
                di += leadershipCostsThatAreDI;
            }

            // Create the item adding in the invoiced amounts and the direct payments
            return new TransactionBreakdown(da, di, requestedFromInvoices, receivedFromPayments, fundingSources);
        }

        /// <summary>
        /// Take the start and end dates and break it into chunks based on financial year boundaries
        /// </summary>
        /// <returns></returns>
        public static List<DateRange> GetFinancialYearDateRanges(DateTime startDate, DateTime endDate)
        {
            var result = new List<DateRange>();
            var overallRange = new DateRange
            {
                StartDate = startDate,
                EndDate = endDate
            };
            DateTime currentStart = overallRange.StartDate;
            DateTime financialYearEnd = new DateTime(currentStart.Month > 7 ? currentStart.Year + 1 : currentStart.Year, 7, 31);

            while (true)
            {
                // If the financial year end is not within the remaining range, break
                if (!overallRange.IsWithin(financialYearEnd))
                {
                    // Add the final chunk if there's still time left
                    if (currentStart <= overallRange.EndDate)
                    {
                        result.Add(new DateRange
                        {
                            StartDate = currentStart,
                            EndDate = overallRange.EndDate
                        });
                    }
                    break;
                }

                result.Add(new DateRange
                {
                    StartDate = currentStart,
                    EndDate = financialYearEnd
                });

                currentStart = financialYearEnd.AddDays(1);
                financialYearEnd = financialYearEnd.AddYears(1);
            }

            return result;
        }
    }
}
