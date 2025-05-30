using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a chunk of an assignemnt with a constant grade and financial year
    /// </summary>
    public class AssignmentChunk
    {
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
        public string SalaryCostEstimate { get; set; }

        /// <summary>
        /// This is the planned cost of the resource for the assignment chunk as lifted from the task itself -- doesn't take into account grade changes or increments?
        /// </summary>
        public string PlannedCost { get; set; }

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

        public string FundingSourceDescription { get; set; }

        public string FundingSourceAmount { get; set; }

        public AssignmentChunk()
        {

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
        }
    }
}
