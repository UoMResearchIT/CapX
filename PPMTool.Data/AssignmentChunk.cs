// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;
using System.Diagnostics;
using PPMTool.Data.Entities;
using PPMTool.Data.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a chunk of an assignemnt with a constant grade and financial year
    /// </summary>
    public class AssignmentChunk : DateRange
    {
        public string EmployeeName { get; set; } = null!;

        [Description("The grade of the person for the duration of this assignment")]
        public int Grade { get; set; }

        /// <summary>
        /// This is the FTE of the resource for this assignment chunk based on the project work duty allocation
        /// </summary>
        [Description("The assignment FTE in terms of the person's project allowance in our planning system")]
        public double FTE { get; set; }

        /// <summary>
        /// This is the FTE of the resource that is being billed to the project for this assignment chunk (essentially the equivalent FTE including indirects if the project charges them)
        /// </summary>
        [Description("The FTE we are recharging to the project for this assignment")]
        public double BilledFTE { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = null!;

        public string LeadRSE { get; set; } = null!;

        public string TaskName { get; set; } = null!;

        public string PI { get; set; } = null!;

        public string UpperOrgUnit { get; set; } = null!;

        public string LowerOrgUnit { get; set; } = null!;

        /// <summary>
        /// This is the estimated salary cost of the individual resource based on their grade and FTE -- essentially the cost to the department
        /// </summary>
        [Description("Estimated salary cost based on mid-grade -- i.e. cost to the department for employing the person for this assignment period")]
        public double SalaryCostEstimate { get; set; }

        /// <summary>
        /// This is the planned cost of the resource for the assignment chunk as lifted from the task itself. If not using the day-rate model for costing, this will be the same as the salary cost estimate.
        /// </summary>
        [Description("Estimated cost of assignment based on what model was used to cost the project and nature of funding source")]
        public double PlannedCost { get; set; }

        private DateTime startDate;
        public new DateTime StartDate
        {
            get => startDate;
            set
            {
                if (startDate != value)
                {
                    startDate = value;
                    FinancialYear = FinancialReference.GetFinancialYear(startDate);
                }
            }
        }

        public int FinancialYear { get; set; }

        public string? AccountCode { get; set; }

        [Description("Based on how we understand the costing was done - DI/DA or something else because DA/DI doesn't apply")]
        public string? FundingSourceType { get; set; }

        [Description("Costs of the assignment that we estimate will be covered by what we have been told is in the funding source")]
        public double AmountCovered { get; set; }

        [Description("Whether the overall assignment is considered fully covered, partially covered or not covered at all by the money in the funding source")]
        public string BudgetStatus { get; set; } = null!;

        [Description("Supplementary notes on what we know about the funding source used to cover the cost of this assignment")]
        public string? FundingSourceDescription { get; set; }

        [Description("What type of duty this assignment represents - some types of work are not necessarily rechargeable")]
        public string AssignmentType { get; set; } = Duty.ProjectWork.GetDescription();

        /// <summary>
        /// This is the unique key which identifies the resource from which this chunk was defined
        /// </summary>
        public string UniqueResourceKey { get; }

        public AssignmentChunk(string resourceKey)
        {
            UniqueResourceKey = resourceKey;
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="taskToCopy"></param>
        public AssignmentChunk(AssignmentChunk taskToCopy)
        {
            EmployeeName = taskToCopy.EmployeeName;
            Grade = taskToCopy.Grade;
            FTE = taskToCopy.FTE;
            BilledFTE = taskToCopy.BilledFTE;
            ProjectId = taskToCopy.ProjectId;
            ProjectName = taskToCopy.ProjectName;
            LeadRSE = taskToCopy.LeadRSE;
            UpperOrgUnit = taskToCopy.UpperOrgUnit;
            LowerOrgUnit = taskToCopy.LowerOrgUnit;
            PI = taskToCopy.PI;
            TaskName = taskToCopy.TaskName;
            StartDate = new DateTime(taskToCopy.StartDate.Ticks);
            EndDate = new DateTime(taskToCopy.EndDate.Ticks);
            FinancialYear = taskToCopy.FinancialYear;
            AccountCode = taskToCopy.AccountCode;
            FundingSourceType = taskToCopy.FundingSourceType;
            FundingSourceDescription = taskToCopy.FundingSourceDescription;
            AmountCovered = taskToCopy.AmountCovered;
            BudgetStatus = taskToCopy.BudgetStatus;
            SalaryCostEstimate = taskToCopy.SalaryCostEstimate;
            PlannedCost = taskToCopy.PlannedCost;
            AssignmentType = taskToCopy.AssignmentType;
            UniqueResourceKey = taskToCopy.UniqueResourceKey;
        }

        /// <summary>
        /// Based on available financial references, recompute the estimated salary cost of the assignment based on the mid-grade costs of the assignee
        /// </summary>
        /// <param name="finrefs"></param>
        /// <param name="shouldUpdatePlanned"></param>
        internal void RecomputeChunkCosts(IEnumerable<FinancialReference> finrefs, bool shouldUpdatePlanned)
        {
            try
            {
                var annualCosts = finrefs.GetSuitableFinancialReference(FinancialYear).GetMidGradeCosts(Grade);
                var fractionOfYear = (EndDate.Date.Subtract(StartDate.Date).TotalDays + 1) / 365d;

                // The cost of a resource uses the BilledFTE which means it includes indirects if the model permits it
                SalaryCostEstimate = annualCosts * BilledFTE * fractionOfYear;

                // If the planned cost figures should be updated then they will match the salary cost estimate
                if (shouldUpdatePlanned) PlannedCost = SalaryCostEstimate;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
