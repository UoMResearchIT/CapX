using PPMTool.Data.Context;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data.Helpers
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
        /// <param name="resources"></param>
        /// <param name="leadershipSourceId"></param>
        /// <param name="leadershipCosts"></param>
        /// <param name="fundingSources"></param>
        /// <param name="requestedFromInvoices"></param>
        /// <param name="receivedFromPayments"></param>
        /// <returns></returns>
        internal static TransactionBreakdown ComputeTransactionBreakdown(
            PPMToolContext context,
            int leadershipSourceId,
            double leadershipCosts,
            IEnumerable<Resource> resources,
            IEnumerable<FundingSource> fundingSources,
            double requestedFromInvoices,
            double receivedFromPayments)
        {
            // DA is just the sum of the DA funding sources
            var da = fundingSources
                .Where(x => x.FundingSourceType == FundingSourceType.DA)
                .RoundedSum(x => x.AmountAvailable, 2);

            // DI is based on the salary costs and assignments of the resources
            var di = resources
                .Where(x => x.FundedFrom?.FundingSourceType == FundingSourceType.DI)
                .RoundedSum(x => x.PlannedCost, 2);

            // Add to these totals the leadership costs if relevant
            var leadershipSource = fundingSources.FirstOrDefault(x => x.FundingSourceId == leadershipSourceId);
            if (leadershipSource != null)
            {
                if (leadershipSource.FundingSourceType == FundingSourceType.DA)
                {
                    da += leadershipCosts;
                }
                else if (leadershipSource.FundingSourceType == FundingSourceType.DI)
                {
                    di += leadershipCosts;
                }
            }

            // Create the item adding in the invoiced amounts and the direct payments
            return new TransactionBreakdown(da, di, requestedFromInvoices, receivedFromPayments, fundingSources);
        }
    }
}
