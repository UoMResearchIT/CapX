// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents the numeric value of a stable financial reference value set for a specific financial year set.
    /// </summary>
    public class FinancialReferenceValue : ILoggableObject
    {
        public int FinancialReferenceValueId { get; set; }

        /// <summary>
        /// The financial reference value set that this value represents.
        /// </summary>
        [Required]
        public virtual FinancialReferenceValueSet FinancialReferenceValueSet { get; set; } = null!;

        public int FinancialReferenceValueSetId { get; set; }

        public float Value { get; set; }

        /// <summary>
        /// Foreign key to the financial reference set that this value belongs to.
        /// </summary>
        [Required]
        public virtual FinancialReference FinancialReference { get; set; } = null!;

        public int FinancialReferenceId { get; set; }

        /// <summary>
        /// Gets a sensible name for the object.
        /// </summary>
        /// <returns>The sensible name.</returns>
        public string GetSensibleObjectName()
        {
            return $"{FinancialReference?.GetSensibleObjectName()} | {FinancialReferenceValueSet?.Name}";
        }
    }
}
