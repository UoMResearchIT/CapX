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

        public double PlannedCost { get; set; }

        public double PlannedLeadershipCosts { get; set; }

        public double ActualCost { get; set; }

        public double ActualLeadershipCosts { get; set; }

        public double FundsRequested { get; set; }

        public double FundsReceived { get; set; }

        public double ActualHours { get; set; }

        public string PlannedCostColour { get; }

        public string ActualCostColour { get; }

        public string FundsReceivedColour { get; }

        public string FundsRequestedColour { get; }

        public double FundsOwed { get; }

        public string FundsOwedColour { get; }

        public double FundsDA { get; }

        public double FundsDI { get; }

        public MarkupString ListOfFundingSources { get; }


        public FinanceSummaryItem(Project project, TransactionBreakdown transactionBreakdown)
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
            Faculty = project.Faculty;
            ProjectPM = project.ProjectManager?.Name ?? "Not Set";
            ProjectStatus = project.ProjectStatus;
            CostModel = project.CostModel;
            DayRate = project.DayRate;
            Budget = project.Budget;
            PlannedCost = project.PlannedCost;
            PlannedLeadershipCosts = project.PlannedLeadershipCosts;
            ActualCost = project.ActualCost;
            ActualLeadershipCosts = project.ActualLeadershipCosts;
            FundsDA = transactionBreakdown.DirectlyAllocated;
            FundsDI = transactionBreakdown.DirectlyIncurred;
            FundsRequested = transactionBreakdown.Invoices;
            FundsReceived = transactionBreakdown.Payments;
            ActualHours = project.SubTasks?.RoundedSum(x => x.ActualWorkHours) ?? 0;
            PlannedCostColour = PlannedCost > Budget ? "red" : "green";
            ActualCostColour = ActualCost > PlannedCost ? "red" : "green";
            FundsReceivedColour = FundsReceived < FundsRequested ? "red" : "green";
            FundsRequestedColour = FundsRequested < Budget ? "red" : "green";
            FundsOwed = FundsRequested - FundsReceived;
            FundsOwedColour = (FundsOwed > 0) ? "red" : "green";

            var sourcesAsList = transactionBreakdown.FundingSources
                .Select(x => x.GetSensibleObjectName().Replace(" ", "&nbsp;"))
                .Distinct();
            ListOfFundingSources = (MarkupString)((sourcesAsList != null && sourcesAsList.Count() > 0) ? string.Join("<br />", sourcesAsList) : "None");
        }
    }
}
