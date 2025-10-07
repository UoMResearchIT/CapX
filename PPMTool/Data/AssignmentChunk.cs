using System.Diagnostics;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a chunk of an assignemnt with a constant grade and financial year
    /// </summary>
    public class AssignmentChunk
    {
        public string PostNumber { get; set; }

        public string EmployeeName { get; set; }

        public int Grade { get; set; }

        public double FTE { get; set; }

        public string Project { get; set; }

        public string LeadRSE { get; set; }

        public string Task { get; set; }

        public string PI { get; set; }

        public string Faculty { get; set; }

        public string School { get; set; }

        /// <summary>
        /// This is the estiamted salary cost of the individual resource based on their grade and FTE
        /// </summary>
        public double SalaryCostEstimate { get; set; }

        /// <summary>
        /// This is the planned cost of the resource for the assignment chunk as lifted from the task itself -- doesn't take into account grade changes or increments?
        /// </summary>
        public double PlannedCost { get; set; }

        private DateTime startDate;
        public DateTime StartDate
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

        public DateTime EndDate { get; set; }

        public int FinancialYear { get; set; }

        public string AccountCode { get; set; }

        public string FundingSourceType { get; set; }

        public double FundingSourceAmount { get; set; }

        public string FundingSourceDescription { get; set; }

        public bool IsLeadershipAssignment { get; set; }

        public AssignmentChunk()
        {

        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="taskToCopy"></param>
        public AssignmentChunk(AssignmentChunk taskToCopy)
        {
            PostNumber = taskToCopy.PostNumber;
            EmployeeName = taskToCopy.EmployeeName;
            Grade = taskToCopy.Grade;
            FTE = taskToCopy.FTE;
            Project = taskToCopy.Project;
            LeadRSE = taskToCopy.LeadRSE;
            Faculty = taskToCopy.Faculty;
            School = taskToCopy.School;
            PI = taskToCopy.PI;
            Task = taskToCopy.Task;
            StartDate = new DateTime(taskToCopy.StartDate.Ticks);
            EndDate = new DateTime(taskToCopy.EndDate.Ticks);
            FinancialYear = taskToCopy.FinancialYear;
            AccountCode = taskToCopy.AccountCode;
            FundingSourceType = taskToCopy.FundingSourceType;
            FundingSourceDescription = taskToCopy.FundingSourceDescription;
            FundingSourceAmount = taskToCopy.FundingSourceAmount;
            SalaryCostEstimate = taskToCopy.SalaryCostEstimate;
            PlannedCost = taskToCopy.PlannedCost;
            IsLeadershipAssignment = taskToCopy.IsLeadershipAssignment;
        }

        /// <summary>
        /// Based on available financial references, updates the estimated salary cost of the assignment based on the mid-grade costs of the assignee
        /// </summary>
        /// <param name="finrefs"></param>
        internal void UpdateEstimatedSalaryCost(IEnumerable<FinancialReference> finrefs)
        {
            try
            {
                var annualCosts = finrefs.GetSuitableFinancialReference(FinancialYear).GetMidGradeCosts(Grade);
                var fractionOfYear = (EndDate.Date.Subtract(StartDate.Date).TotalDays + 1) / 365d;
                SalaryCostEstimate = annualCosts * FTE * fractionOfYear;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}
