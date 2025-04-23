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

        public double SalaryCostEstimate { get; set; }

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

        public AssignmentChunk()
        {

        }

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
        }

    }
}
