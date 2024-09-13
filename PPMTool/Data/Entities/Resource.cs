using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using PPMTool.Enums;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource : ILoggableClass
    {
        public int ResourceId { get; set; }

        public Person Person { get; set; }

        /// <summary>
        /// This is the day rate associated with this resource assignment.
        /// If this is null when creating the resource, it will be assigned based
        /// on the default day rate for the project.
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

        public double PlannedWorkHours { get; set; }

        public double ActualWorkHours { get; set; }

        public double PlannedCost { get; set; }

        public double ActualCost { get; set; }

        public string GetSensibleObjectName()
        {
            return $"{Person?.Name} (Resource)";
        }

        internal void UpdateResourceCosts(bool actualCosts, CostModel costModel, DateTime taskStart, DateTime taskEnd, IList<FinancialReference> financialReferences = null, double? dayRate = null)
        {
            // Get WLM active at start of task (should never be null as person has to have started to be assigned to the task)
            var startWLM = Person.WorkloadModelChanges.Where(x => x.ChangeDate <= taskStart).OrderByDescending(x => x.ChangeDate).First();

            // Compute start and end FY for task
            var startFY = FinancialReference.GetFinancialYear(taskStart);
            var endFY = FinancialReference.GetFinancialYear(taskEnd);
            var currentFY = FinancialReference.GetFinancialYear(DateTime.Today);

            if (actualCosts)
            {
                // TODO: Actual costs - calculated on a per resource basis based on the hours recorded for that person
                ActualCost = 0;

                // Actuals per year?

                // Convert hours to billable days


            }
            else
            {
                // Cost of each resource -- note that these are committed costs, cost of unmet demand is not included as it is not a planned cost.
                // Ignores WLM changes mid-assignment as too complicated to work out.
                PlannedCost = 0;

                // First period is partial year from start of task to end of the FY
                var finref = financialReferences.GetSuitableFinancialReference(startFY);
                var billableDays = SubTask.GetNumberOfBillableDays(taskStart, new DateTime(startFY + 1, 7, 31)) * AssignmentFTE;
                PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade) * (billableDays / 220);

                // Compute cost for each complete FY
                for (int fy = startFY + 1; fy < endFY; ++fy)
                {
                    finref = financialReferences.GetSuitableFinancialReference(fy);
                    billableDays = 220 * AssignmentFTE;
                    PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade);
                }

                // Final period is partial year again from start of FY to end of task
                finref = financialReferences.GetSuitableFinancialReference(endFY);
                billableDays = SubTask.GetNumberOfBillableDays(new DateTime(endFY, 8, 1), taskEnd) * AssignmentFTE;
                PlannedCost += finref.GetSuitableCostForGrade(startWLM.Grade) * (billableDays / 220);
            }
        }
    }
}
