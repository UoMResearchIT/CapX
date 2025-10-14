using System.Diagnostics;
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

            // Add to these totals the leadership costs if DI
            var leadershipSource = fundingSources.FirstOrDefault(x => x.FundingSourceId == leadershipSourceId);
            if (leadershipSource != null && leadershipSource.FundingSourceType == FundingSourceType.DI)
            {
                di += leadershipCosts;
            }

            // Create the item adding in the invoiced amounts and the direct payments
            return new TransactionBreakdown(da, di, requestedFromInvoices, receivedFromPayments, fundingSources);
        }

        /// <summary>
        /// Generates a series of project budget detail objects, one for each resource assign to subtasks on the projects supplied.
        /// Resources with no funding source will be ignored and will not be included in the dictionary.
        /// They can be assumed to be not in budget.
        /// </summary>
        /// <param name="projects"></param>
        /// <returns></returns>
        internal static IDictionary<string, AssignmentBudgetDetail> GetProjectBudgetDetail(IEnumerable<Project> projects)
        {
            // Initialise the dictionary
            var budgets = new Dictionary<string, AssignmentBudgetDetail>();

            foreach (var project in projects)
            {
                // Get all subtasks and add the leadership tasks if necessary
                var subtasks = project.SubTasks.ToList();
                if (project.CostModel == CostModel.TechAndLeadership)
                {
                    subtasks.AddRange(project.GenerateLeadershipTasks());
                }

                // Get all the resource assignments with funding sources
                var assignments = subtasks
                    .SelectMany(x => x.AssignedResources)
                    .Where(x => x.FundedFrom != null)
                    .ToList();

                // Get the start and end dates of the marching window
                var currentDate = project.StartDate.Date;
                var endDate = project.EndDate.Date;

                // Map funding sources to temporary counter
                var fundingPots = new Dictionary<int, double>();
                foreach (var fs in project.FundingSources)
                {
                    // Add to existing or create new as required
                    if (fundingPots.TryGetValue(fs.FundingSourceId, out var existingAmount))
                    {
                        fundingPots[fs.FundingSourceId] = existingAmount + fs.AmountAvailable;
                    }
                    else
                    {
                        fundingPots[fs.FundingSourceId] = fs.AmountAvailable;
                    }
                }

                Debug.WriteLine($"** {fundingPots.Count} funding pots for {project.GetFullName()}. Total budget of {fundingPots.Sum(x => x.Value):C0}. {assignments.Count} assignments with funding sources.");

                // If no funding pots or billable assignments then move to next project
                if (fundingPots.Count == 0 || assignments.Count == 0)
                {
                    continue;
                }

                // Initialise the budget details using a dictionary for lookup
                var budgetMap = assignments.ToDictionary(
                    x => x.GenerateUniqueResourceKey(),
                    x => new AssignmentBudgetDetail
                    {
                        Resource = x,
                        InBudget = 0,
                        DailyCost = x.PlannedCost / x.SubTask.DurationDays,
                        Status = BudgetStatus.FullyInBudget
                    });

                budgets.AddRange(budgetMap);

                // March through the project
                while (currentDate <= endDate)
                {
                    // Get all assignments on current day
                    foreach (var assignment in assignments)
                    {
                        // If no assignments running on the day then move to next day
                        if (!assignment.SubTask.IsWithin(currentDate))
                            continue;

                        // Get budget detail of the assignment fro the dictionary
                        var budgetDetail = budgetMap[assignment.GenerateUniqueResourceKey()];

                        // Skip if already marked as out of budget as nothing to do
                        if (budgetDetail.Status == BudgetStatus.NotInBudget)
                            continue;

                        // Is first day of the assignment
                        var isFirstDayOfAssignment = currentDate.Date == assignment.SubTask.StartDate.Date;

                        // Log current funding pot status and get pot value for update
                        var fsId = assignment.FundedFrom.FundingSourceId;
                        if (!fundingPots.TryGetValue(fsId, out var potValue))
                            continue;
                        var potHasMoneyBeforeUpdate = potValue > 0;

                        // Update the funding pot information by deducting the costs
                        potValue -= budgetDetail.DailyCost;
                        fundingPots[fsId] = potValue;

                        // Check funding pot status after
                        var potEmptyAfterUpdate = potValue <= 0;

                        // If the assignment has just started and the funding pot was already negative then mark as out of budget
                        if (isFirstDayOfAssignment && !potHasMoneyBeforeUpdate)
                        {
                            // Task never had budget on day one so fully out of budget
                            budgetDetail.Status = BudgetStatus.NotInBudget;
                        }
                        // If something was positive and has now gone negative then flag the status as partially in budget
                        else if (budgetDetail.Status == BudgetStatus.FullyInBudget &&
                                 potHasMoneyBeforeUpdate && potEmptyAfterUpdate)
                        {
                            // Downgrade to partial and stash the expiry date
                            budgetDetail.Status = BudgetStatus.PartiallyInBudget;
                            budgetDetail.FundingSourceExpired = currentDate;

                            // Update the in budget amount by the remainder
                            budgetDetail.InBudget += -potValue;
                        }

                        // If still in budget then update the amount on the budget detail
                        if (budgetDetail.Status == BudgetStatus.FullyInBudget)
                        {
                            budgetDetail.InBudget += budgetDetail.DailyCost;
                        }
                    }

                    // Advance to next day
                    currentDate = currentDate.AddDays(1);
                }
            }

            return budgets;
        }
    }
}
