// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Helpers;
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
        /// The FTE of the resource that is billed to the project (including indirects if applicable)
        /// </summary>
        public double BilledFTE { get; set; }

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
        /// Updates the planned and actual cost of the resource given either a day rate a financial reference.
        /// Assumptions:
        /// 1. Ignores annual increments for people
        /// </summary>
        /// <param name="project"></param>
        /// <param name="subTask"></param>
        /// <param name="finrefs"></param>
        /// <returns>A list of assignment chunks that represent this resource assignment</returns>
        internal IEnumerable<AssignmentChunk> UpdateResourceCosts(
            Project project,
            SubTask subTask,
            IEnumerable<FinancialReference> finrefs)
        {
            // Costs to the department are always salary-based regardless of the recharge cost model (day-rate or salary based)
            // Therefore this computes the actual cost of the resource chosen over the full task (planned cost)
            // or the duration worked indicated by the number of hours booked (actual cost)
            IEnumerable<AssignmentChunk> chunks = new List<AssignmentChunk>();

            // Get durations in days over which the work is spread
            var durationDaysPlanned = PlannedWorkHours / 7f;
            var durationDaysActual = ActualWorkHours / 7f;
            var fundingSourceType = FundedFrom?.FundingSourceType;

            // Update the billed FTE value for the resource based on the cost model
            UpdateBilledFTE(project.CostModel);

            // If using the day rate model the planned cost is only day rate if we aren't recharging to DI funding sources which have to be salary costs
            if (project.CostModel == CostModel.DayRate && fundingSourceType != FundingSourceType.DI)
            {
                // Actual cost is hours converted to days multiplied by the day rate
                ActualCost = durationDaysActual * (UseProjectDayRate ? project.DayRate : DayRate ?? 0);

                // Planned cost is the hours work of the assignment converted to billable days and multiplied by the day rate
                PlannedCost = durationDaysPlanned * (UseProjectDayRate ? project.DayRate : DayRate ?? 0);
            }

            // If using the grade-based models or day rate but DI funding source
            else
            {
                // Convert to assignment chunks (do not generate extra leadership chunks)
                chunks = ExportHelper.GetAssignmentChunks(
                    Person,
                    new List<Project> { project },
                    finrefs,
                    subTask.StartDate,
                    subTask.EndDate,
                    new List<SubTask> { subTask },
                    true,
                    generateLeadershipTasks: GenerateLeadershipTaskLogic.None
                );

                // Planned costs (technical assignments only)
                PlannedCost = chunks.Sum(x => x.PlannedCost);

                // Actual costs are a proportion of the planned based on actuals recorded
                ActualCost = 0d;
                if (durationDaysPlanned > 0)
                {
                    var proportion = durationDaysActual / durationDaysPlanned;
                    ActualCost = PlannedCost * proportion;
                }
            }

            // The indirects only apply if the appropriate cost model is in place
            ActualIndirectCost = 0d;
            PlannedIndirectCost = 0d;
            if (project.CostModel.HasIndirects())
            {
                // Planned indirects are just proportion of the technical costs
                PlannedIndirectCost = (PlannedCost * GlobalDefaults.BAUTopSliceFractionDefault) / (1 + GlobalDefaults.BAUTopSliceFractionDefault);

                // Actual costs are also just proportion of the actual technical costs
                ActualIndirectCost = (ActualCost * GlobalDefaults.BAUTopSliceFractionDefault) / (1 + GlobalDefaults.BAUTopSliceFractionDefault);
            }

            return chunks;
        }

        /// <summary>
        /// Generates a unique resource key based on the project, subtask and resource combination
        /// </summary>
        /// <returns></returns>
        internal string GenerateUniqueResourceKey()
        {
            // Should be a unique set of information as leadership tasks without IDs don't overlap
            var composite = $"{SubTask.OwningProject.RTP}|{SubTask.SubTaskId}|{SubTask.StartDate:yyyyMMdd}|{SubTask.EndDate:yyyyMMdd}|{ResourceId}";

            // Get a unique hash and truncate so not too long
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(composite);
                var hash = sha.ComputeHash(bytes);
                return BitConverter.ToString(hash).Replace("-", "").Substring(0, 12);
            }
        }

        /// <summary>
        /// Method to update the billed FTE based on the indirects rate if the project cost model requires it
        /// </summary>
        /// <param name="model"></param>
        internal void UpdateBilledFTE(CostModel model)
        {
            BilledFTE = model.HasIndirects() ? AssignmentFTE * (1 + GlobalDefaults.BAUTopSliceFractionDefault) : AssignmentFTE;
        }
    }
}
