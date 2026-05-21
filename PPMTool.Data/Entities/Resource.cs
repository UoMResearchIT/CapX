// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;
using PPMTool.Data.Helpers;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource : CostedItem, ILoggableObject
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int ResourceId { get; set; }

        /// <summary>
        /// The person associated with this resource
        /// </summary>
        [Required]
        public virtual Person Person { get; set; } = null!;

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
        public virtual FundingSource? FundedFrom { get; set; }

        /// <summary>
        /// The task on the project this resource is assigned to
        /// </summary>
        [Required]
        public virtual SubTask SubTask { get; set; } = null!;

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
        /// <param name="indirectsPercentage"></param>
        /// <returns>A list of assignment chunks that represent this resource assignment</returns>
        internal IEnumerable<AssignmentChunk> UpdateResourceCosts(
            Project project,
            SubTask subTask,
            IEnumerable<FinancialReference> finrefs,
            float indirectsPercentage)
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
            UpdateBilledFTE(project.CostModel, indirectsPercentage);

            // If using the day rate model the planned cost is only day rate
            if (project.CostModel == CostModel.DayRate)
            {
                // Actual cost is hours converted to days multiplied by the day rate
                ActualCost = durationDaysActual * (UseProjectDayRate ? project.DayRate : DayRate ?? 0);

                // Planned cost is the hours work of the assignment converted to billable days and multiplied by the day rate
                PlannedCost = durationDaysPlanned * (UseProjectDayRate ? project.DayRate : DayRate ?? 0);
            }

            // If using the grade-based models
            else
            {
                // Convert to assignment chunks and recompute the costs of the chunks
                chunks = AssignmentHelper.GetAssignmentChunks(
                    Person,
                    new List<Project> { project },
                    finrefs,
                    subTask.StartDate,
                    subTask.EndDate,
                    new List<SubTask> { subTask },
                    true
                );

                // Planned costs of the resource (leadership tasks are zero if not a leadership cost model)
                PlannedCost = (!project.CostModel.HasLeadership() && subTask.IsLeadershipTask) ?
                    0 :
                    chunks.Sum(x => x.PlannedCost);

                // Actual costs are a proportion of the planned based on actuals recorded
                ActualCost = 0d;
                if (durationDaysPlanned > 0)
                {
                    var proportion = durationDaysActual / durationDaysPlanned;
                    ActualCost = PlannedCost * proportion;
                }
            }

            // The indirects only apply if the appropriate cost model is in place and it is not a leadership task
            ActualIndirectCost = 0d;
            PlannedIndirectCost = 0d;
            if (project.CostModel.HasIndirects() && !subTask.IsLeadershipTask)
            {
                // Planned indirects are just proportion of the technical costs as they were computed using BilledFTE
                PlannedIndirectCost = (PlannedCost * indirectsPercentage) / (1 + indirectsPercentage);

                // Actual costs are also just proportion of the actual technical costs as they were computed using BilledFTE
                ActualIndirectCost = (ActualCost * indirectsPercentage) / (1 + indirectsPercentage);
            }

            return chunks;
        }

        /// <summary>
        /// Generates a unique resource key based on the project, subtask and resource combination
        /// </summary>
        /// <returns></returns>
        public string GenerateUniqueResourceKey()
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
        /// Method to update the billed FTE based on the indirects rate if the project cost model requires it.
        /// Does not apply to leadership assignments.
        /// </summary>
        /// <param name="model"></param>
        /// <param name="indirectsPercentage"></param>
        public void UpdateBilledFTE(CostModel model, float indirectsPercentage)
        {
            // Do not apply indirects to leadership assignments
            BilledFTE = (model.HasIndirects() && !SubTask.IsLeadershipTask) ? AssignmentFTE * (1 + indirectsPercentage) : AssignmentFTE;
        }
    }
}
