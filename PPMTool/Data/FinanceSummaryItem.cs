// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
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
            FundsRequested = fundsRequested;
            FundsReceived = fundsReceived;
            ActualHours = project.SubTasks?.RoundedSum(x => x.ActualWorkHours) ?? 0;
            PlannedCostColour = PlannedCost > Budget ? "red" : "green";
            ActualCostColour = ActualCost > PlannedCost ? "red" : "green";
            FundsReceivedColour = (FundsReceived < Budget || (FundsReceived == 0 && Budget > 0)) ? "red" : "green";
            FundsRequestedColour = (FundsRequested > FundsReceived || (FundsRequested == 0 && Budget > 0)) ? "red" : "green";
            FundsOwed = FundsRequested - FundsReceived;
            FundsOwedColour = (FundsOwed > 0) ? "red" : "green";
        }
    }
}
