// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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

        public int Grade { get; set; }

        /// <summary>
        /// Financial reference key used to compute costs for this assignment chunk
        /// </summary>
        public int? CostValueSetId { get; set; }

        /// <summary>
        /// Financial reference key name used for display in this assignment chunk
        /// </summary>
        public string? CostValueName { get; set; }

        /// <summary>
        /// This is the FTE of the resource for this assignment chunk based on the project work duty allocation
        /// </summary>
        public double FTE { get; set; }

        /// <summary>
        /// This is the FTE of the resource that is being billed to the project for this assignment chunk (essentially the equivalent FTE including indirects if the project charges them)
        /// </summary>
        public double BilledFTE { get; set; }

        public int ProjectId { get; set; }

        public string ProjectName { get; set; } = null!;

        public string LeadRSE { get; set; } = null!;

        public string TaskName { get; set; } = null!;

        public string PI { get; set; } = null!;

        public string UpperOrgUnit { get; set; } = null!;

        public string LowerOrgUnit { get; set; } = null!;

        /// <summary>
        /// This is the planned cost of the resource for the assignment chunk as lifted from the task itself. If not using the day-rate model for costing, this will be the same as the salary cost estimate.
        /// </summary>
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

        public string? FundingSourceType { get; set; }

        public string? FundingSourceDescription { get; set; }

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
            CostValueSetId = taskToCopy.CostValueSetId;
            CostValueName = taskToCopy.CostValueName;
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
            PlannedCost = taskToCopy.PlannedCost;
            AssignmentType = taskToCopy.AssignmentType;
            UniqueResourceKey = taskToCopy.UniqueResourceKey;
        }

        /// <summary>
        /// Based on available financial references, recompute the estimated salary cost of the assignment using the explicitly selected cost key.
        /// </summary>
        /// <param name="finrefs"></param>
        internal void RecomputeChunkCosts(IEnumerable<FinancialReference> finrefs)
        {
            try
            {
                var finRef = finrefs.GetSuitableFinancialReference(FinancialYear);
                var annualCosts = 0f;

                // If a cost value key has been explicitly selected then use that to get the annual costs, otherwise fall back to using the cost value name if available
                if (CostValueSetId != null)
                {
                    annualCosts = finRef.Values?.FirstOrDefault(x => x.FinancialReferenceValueSetId == CostValueSetId)?.Value ?? 0f;
                }

                // If the annual costs are still zero then try to use the cost value name to get the annual costs
                if (annualCosts == 0f && !string.IsNullOrWhiteSpace(CostValueName))
                {
                    annualCosts = finRef.GetValue(CostValueName);
                }

                var fractionOfYear = (EndDate.Date.Subtract(StartDate.Date).TotalDays + 1) / 365d;

                // If the planned cost figures should be updated then they will match the salary cost estimate
                // The cost of a resource uses the BilledFTE which means it includes indirects if the model permits it
                PlannedCost = annualCosts * BilledFTE * fractionOfYear;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
