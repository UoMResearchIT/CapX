// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Entities;
using PPMTool.Enums;

namespace PPMTool.Data
{
    /// <summary>
    /// Represent a resource assignment with how much of the planned costs is in budget based on funding sources
    /// </summary>
    public class AssignmentBudgetDetail
    {
        /// <summary>
        /// The resource assignment details
        /// </summary>
        public Resource Resource { get; set; }

        /// <summary>
        /// Budget status of the assignment
        /// </summary>
        public BudgetStatus Status { get; set; }

        /// <summary>
        /// How much of the costs are in budget based on the funding source and its use across the project
        /// </summary>
        public double InBudget { get; set; }

        /// <summary>
        /// The daily cost of this assignment based on the planned cost
        /// </summary>
        public double DailyCost { get; set; }

        /// <summary>
        /// The date on which the funding source expired if partially funded.
        /// </summary>
        public DateTime? FundingSourceExpired { get; set; }
    }
}
