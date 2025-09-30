using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource : CostedItem, ILoggableClass
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int ResourceId { get; set; }

        /// <summary>
        /// The person associated with this resource
        /// </summary>
        [Required]
        public virtual Person Person { get; set; }

        /// <summary>
        /// This is the day rate associated with this resource assignment.
        /// If this is null when using the project day rate.
        /// </summary>
        [DataType(DataType.Currency)]
        public double? DayRate { get; set; }

        /// <summary>
        /// FTE of the resource's assignment to a task
        /// </summary>
        public double AssignmentFTE { get; set; }

        /// <summary>
        /// Whether the assignment is provisional
        /// </summary>
        public bool IsProvisional { get; set; }

        private bool useProjectDayRate = true;
        /// <summary>
        /// Whether this resource should use the project day rate or its own
        /// </summary>
        public bool UseProjectDayRate
        {
            get => useProjectDayRate;
            set
            {
                if (useProjectDayRate != value)
                {
                    useProjectDayRate = value;

                    // Set the day rate
                    if (!value) DayRate = 0;
                    else DayRate = null;

                }
            }
        }

        /// <summary>
        /// The cost rate of this person based on RSE costing model
        /// </summary>
        [Required]
        public Rate Rate { get; set; }

        /// <summary>
        /// This represents where the resource is funded from in terms of known funding sources for the project.
        /// It is optional since it needs to be possible to associated resources with tasks before the funding sources are known.
        /// </summary>
        public virtual FundingSource FundedFrom { get; set; }

        /// <summary>
        /// The task on the project this resource is assigned to
        /// </summary>
        [Required]
        public virtual SubTask SubTask { get; set; }

        /// <inheritdoc/>
        public string GetSensibleObjectName()
        {
            return $"{Person?.Name} (Resource)";
        }

        /// <summary>
        /// Updates the planned and actual cost of the resource given either a day rate a financial reference
        /// Assumptions:
        /// 1. Ignores grade-changes mid-task
        /// 2. Ignores financial reference changes year on year
        /// </summary>
        /// <param name="costModel"></param>
        /// <param name="taskStart"></param>
        /// <param name="taskEnd"></param>
        /// <param name="financialReference"></param>
        /// <param name="projectDayRate"></param>
        internal void UpdateResourceCosts(CostModel costModel, DateTime taskStart, DateTime taskEnd, double? projectDayRate, FinancialReference financialReference)
        {
            // If using the day rate model then calculation is simple
            if (costModel == CostModel.DayRate)
            {
                // Actual cost is hours converted to days multiplied by the day rate
                ActualCost = (ActualWorkHours / 7f) * (UseProjectDayRate ? projectDayRate ?? 0 : DayRate ?? 0);

                // Planned cost is the hours work of the assignment converted to billable days and multiplied by the day rate
                PlannedCost = (PlannedWorkHours / 7f) * (UseProjectDayRate ? projectDayRate ?? 0 : DayRate ?? 0);
            }

            // If using the grade-based models
            else
            {
                // Use a financial reference and the standard or junior rate to compute the cost
                // assuming it persists throughout the project and doesn't increment year on year

                // Get the annual salary costs for resource based on rate
                var annualCostPerBillableDay = financialReference.GetJuniorOrStandardAnnualCosts(Rate) / 220;

                // Update the actuals
                ActualCost = (ActualWorkHours / 7f) * annualCostPerBillableDay;

                // Update the planned
                PlannedCost = (PlannedWorkHours / 7f) * annualCostPerBillableDay;
            }

            // If we wanted to include the year to year variation based on financial references then we could do it like below.
            // However, actuals would need to be recorded year on year to be able to match the planned cost algorithm
            // I guess this actually exists now but a job for another day

            //// Get WLM active at start of task
            //var startWLM = Person.WorkloadModelChanges
            //    .Where(x => x.ChangeDate <= taskStart)
            //    .OrderByDescending(x => x.ChangeDate)
            //    .FirstOrDefault();

            //// If they haven't got a WLM then use the G6 default
            //if (startWLM == null)
            //{
            //    startWLM = Person.GetWorkloadModelOnDateOrDefault(taskStart);
            //}

            //// Get WLM active at start of task (should never be null as person has to have started to be assigned to the task)
            //var startWLM = Person.WorkloadModelChanges.Where(x => x.ChangeDate <= taskStart).OrderByDescending(x => x.ChangeDate).First();

            //// Compute start and end FY for task
            //var startFY = FinancialReference.GetFinancialYear(taskStart);
            //var endFY = FinancialReference.GetFinancialYear(taskEnd);
            //var currentFY = FinancialReference.GetFinancialYear(DateTime.Today);

            //// Cost of each resource -- note that these are committed costs, cost of unmet demand is not included as it is not a planned cost.
            //// Ignores WLM changes mid-assignment as too complicated to work out.
            //PlannedCost = 0;

            //// First period is partial year from start of task to end of the FY
            //var finref = financialReference.GetSuitableFinancialReference(startFY);
            //var billableDays = SubTask.GetNumberOfBillableDays(taskStart, new DateTime(startFY + 1, 7, 31)) * AssignmentFTE;
            //PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade) * (billableDays / 220);

            //// Compute cost for each complete FY
            //for (int fy = startFY + 1; fy < endFY; ++fy)
            //{
            //    finref = financialReference.GetSuitableFinancialReference(fy);
            //    billableDays = 220 * AssignmentFTE;
            //    PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade);
            //}

            //// Final period is partial year again from start of FY to end of task
            //finref = financialReference.GetSuitableFinancialReference(endFY);
            //billableDays = SubTask.GetNumberOfBillableDays(new DateTime(endFY, 8, 1), taskEnd) * AssignmentFTE;
            //PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade) * (billableDays / 220);
        }
    }
}
