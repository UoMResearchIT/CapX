using System;
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

        public string School { get; set; }

        public string Faculty { get; set; }

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


        public FinanceSummaryItem(Project project, double fundsRequested, double fundsReceived)
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
            School = project.School.ToNiceString();
            Faculty = project.Faculty.ToNiceString();
            ProjectPM = project.ProjectManager?.Name ?? "Not Set";
            CostModel = project.CostModel;
            DayRate = project.DayRate;
            Budget = project.Budget;
            PlannedCost = project.PlannedCost;
            PlannedLeadershipCosts = project.PlannedLeadershipCosts;
            ActualCost = project.ActualCost;
            ActualLeadershipCosts = project.ActualLeadershipCosts;
            FundsRequested = fundsRequested;
            FundsReceived = fundsReceived;
            ActualHours = project.SubTasks?.RoundedSum(x => x.ActualWorkHours) ?? 0;
            PlannedCostColour = PlannedCost > Budget ? "red" : "green";
            ActualCostColour = ActualCost > PlannedCost ? "red" : "green";
            FundsReceivedColour = FundsReceived < Budget ? "red" : "green";
            FundsRequestedColour = FundsRequested > FundsReceived ? "red" : "green";
        }
    }
}
