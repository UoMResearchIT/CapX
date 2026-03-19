using Microsoft.AspNetCore.Components;
using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents the key information for a project related to its financial state
    /// </summary>
    public class FinanceSummaryItem
    {
        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        public int ProjectRTP { get; set; }

        public string ProjectName { get; set; }

        public string ProjectPI { get; set; }

        public ProjectStatus ProjectStatus { get; set; }

        public School School { get; set; }

        public Faculty Faculty { get; set; }

        public string ProjectPM { get; set; }

        public CostModel CostModel { get; set; }

        public double DayRate { get; set; }

        public double Budget { get; set; }

        /// <summary>
        /// This represents the total of ALL costs (tech + leadership + indirects) for the project
        /// </summary>
        public double PlannedTotalCost { get; set; }

        /// <summary>
        /// This represents just the amount that is leadership costs
        /// </summary>
        public double PlannedLeadershipCosts { get; set; }

        /// <summary>
        /// This represents just the amount that is indirects
        /// </summary>
        public double PlannedIndirectCosts { get; set; }

        /// <summary>
        /// This represents the total actual costs (tech + leadership + indirects) for the project
        /// </summary>
        public double ActualTotalCost { get; set; }

        /// <summary>
        /// This represents just the amount that is leadership
        /// </summary>
        public double ActualLeadershipCosts { get; set; }

        /// <summary>
        /// This represents just the amount that is  indirects
        /// </summary>
        public double ActualIndirectCosts { get; set; }

        public double BudgetIndirectCosts { get; set; }

        public double FundsRequestedOther { get; set; }

        public double FundsReceivedOther { get; set; }

        public double ActualHours { get; set; }

        public string PlannedCostColour { get; }

        public string ActualCostColour { get; }

        public string FundsReceivedColour { get; }

        public string FundsRequestedColour { get; }

        public double FundsOwed { get; }

        public string FundsOwedColour { get; }

        public double FundsDA { get; }

        public double FundsDI { get; }

        public double AvailableFundsDI { get; }

        public string FundsDIColour { get; }

        public MarkupString ListOfFundingSources { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="project">A shallow copy of the project entity</param>
        /// <param name="projectManager"></param>
        /// <param name="actuals">Actuals summed across all resources and tasks for this project</param>
        /// <param name="transactionBreakdown"></param>
        public FinanceSummaryItem(Project project, Person projectManager, double actuals, TransactionBreakdown transactionBreakdown)
        {
            if (project == null)
            {
                return;
            }

            // Assign all the properties
            StartDate = project.StartDate;
            EndDate = project.EndDate;
            ProjectRTP = project.RTP;
            ProjectName = project.Name;
            ProjectPI = project.PI;
            School = project.School;
            Faculty = project.School.Faculty;
            ProjectPM = projectManager?.Name ?? "Not Set";
            ProjectStatus = project.ProjectStatus;
            CostModel = project.CostModel;
            DayRate = project.DayRate;
            Budget = project.Budget;
            BudgetIndirectCosts = project.BudgetedIndirects;
            PlannedTotalCost = project.GetTotalPlannedCosts();
            PlannedLeadershipCosts = project.PlannedLeadershipCosts;
            PlannedIndirectCosts = project.PlannedIndirectCost;
            ActualTotalCost = project.ActualCost;
            ActualLeadershipCosts = project.ActualLeadershipCosts;
            ActualIndirectCosts = project.ActualIndirectCost;
            FundsDA = transactionBreakdown.DirectlyAllocated;
            FundsDI = transactionBreakdown.DirectlyIncurred;
            AvailableFundsDI = transactionBreakdown.FundingSources.Where(x => x.FundingSourceType == FundingSourceType.DI).RoundedSum(x => x.AmountAvailable, 2);
            FundsRequestedOther = transactionBreakdown.Invoices;
            FundsReceivedOther = transactionBreakdown.Payments;
            ActualHours = actuals;
            PlannedCostColour = Math.Floor(PlannedTotalCost - Budget) > 0 ? "var(--rz-danger)" : "var(--rz-success)";
            ActualCostColour = Math.Floor(ActualTotalCost - PlannedTotalCost) > 0 ? "var(--rz-danger)" : "var(--rz-success)";
            FundsReceivedColour = Math.Floor(GetAllRequested() - GetAllReceived()) > 0 ? "var(--rz-danger)" : "var(--rz-success)";
            FundsRequestedColour = Math.Floor(Budget - GetAllRequested()) > 0 ? "var(--rz-danger)" : "var(--rz-success)";
            FundsOwed = GetAllRequested() - GetAllReceived();
            FundsOwedColour = (Math.Floor(FundsOwed) > 0) ? "var(--rz-danger)" : "var(--rz-success)";
            FundsDIColour = (Math.Floor(FundsDI - AvailableFundsDI) > 0) ? "var(--rz-danger)" : "var(--rz-success)";

            var sourcesAsList = transactionBreakdown.FundingSources
                .Select(x => x.GetSensibleObjectName().Replace(" ", "&nbsp;"))
                .Distinct();
            ListOfFundingSources = (MarkupString)((sourcesAsList != null && sourcesAsList.Count() > 0) ? string.Join("<br />", sourcesAsList) : "None");
        }

        /// <summary>
        /// Returns all requested funds from all funding source types
        /// </summary>
        /// <returns></returns>
        public double GetAllRequested()
        {
            return FundsDA + FundsDI + FundsRequestedOther;
        }

        /// <summary>
        /// Returns all received funds from all funding source types
        /// </summary>
        /// <returns></returns>
        public double GetAllReceived()
        {
            return FundsDA + GetReceivedDI() + FundsReceivedOther;
        }

        /// <summary>
        /// The amount DI costs received is either the planned costs of the resources (what is on academic timesheets)
        /// or it is the maximum amount avaialble in the DI funding sources as we can't claim what isn't there
        /// </summary>
        /// <returns></returns>
        public double GetReceivedDI()
        {
            return Math.Min(FundsDI, AvailableFundsDI);
        }

        /// <summary>
        /// Get the technical part of the planned project costs
        /// </summary>
        /// <returns></returns>
        public double GetTechPlannedCosts()
        {
            return PlannedTotalCost - PlannedLeadershipCosts - PlannedIndirectCosts;
        }

        /// <summary>
        /// Get the technical part of the actual project costs
        /// </summary>
        /// <returns></returns>
        public double GetTechActualCosts()
        {
            return ActualTotalCost - ActualLeadershipCosts - ActualIndirectCosts;
        }
    }
}
