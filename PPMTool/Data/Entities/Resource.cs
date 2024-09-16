using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PPMTool.Enums;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource : CostedItem, ILoggableClass
    {
        public int ResourceId { get; set; }

        public Person Person { get; set; }

        /// <summary>
        /// This is the day rate associated with this resource assignment.
        /// If this is null when using the project day rate.
        /// </summary>
        [DataType(DataType.Currency)]
        public double? DayRate { get; set; }

        public double AssignmentFTE { get; set; }

        public bool IsProvisional { get; set; }

        private bool useProjectDayRate = true;
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

        public string GetSensibleObjectName()
        {
            return $"{Person?.Name} (Resource)";
        }

        /// <summary>
        /// Updates the planned and actual cost of the resource given either a day rate a financial reference
        /// Assumptions:
        /// 1. Ignores grade-changes mid-task
        /// 2. Ignores financial reference changes year on year (since actuals don't support this)
        /// </summary>
        /// <param name="costModel"></param>
        /// <param name="taskStart"></param>
        /// <param name="taskEnd"></param>
        /// <param name="financialReference"></param>
        /// <param name="projectDayRate"></param>
        internal void UpdateResourceCosts(CostModel costModel, DateTime taskStart, DateTime taskEnd, double? projectDayRate, FinancialReference financialReference = null)
        {
            // If using the day rate model then calculation is simple
            if (costModel == CostModel.DayRate)
            {
                // Actual cost is hours converted to days multiplied by the day rate
                ActualCost = (ActualWorkHours / 7f) * (UseProjectDayRate ? projectDayRate ?? 0 : DayRate ?? 0);

                // Planned cost is the hours work of the assignment converted to billable days and multiplied by the day rate
                PlannedCost = (PlannedWorkHours / 7f) * (UseProjectDayRate ? projectDayRate ?? 0 : DayRate ?? 0);
            }

            // If using the standard and junior rates (grade-based models)
            else
            {
                // Use a financial reference and the standard or junior rate to compute the cost
                // assuming it persists throughout the project

                // Get WLM active at start of task (should never be null as person has to have started to be assigned to the task)
                var startWLM = Person.WorkloadModelChanges.Where(x => x.ChangeDate <= taskStart).OrderByDescending(x => x.ChangeDate).First();

                // Get the annual salary costs for individual
                var annualCostPerBillableDay = financialReference.GetJuniorOrStandardAnnualCosts(startWLM.Grade) / 220;

                // Update the actuals
                ActualCost = (ActualWorkHours / 7f) * annualCostPerBillableDay;

                // Update the planned
                PlannedCost = (PlannedWorkHours / 7f) * annualCostPerBillableDay;
            }


            // If we wanted to include the year to year variation based on financial references then we could do it like bellow.
            // However, actuals would need to be record year on year to be able to match the planned cost algorithm

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
