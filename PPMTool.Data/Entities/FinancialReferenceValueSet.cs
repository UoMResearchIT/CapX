// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Stable identifier for a financial reference value set concept that persists across financial years.
    /// </summary>
    public class FinancialReferenceValueSet : ILoggableObject
    {
        public int FinancialReferenceValueSetId { get; set; }

        [Required]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        public string GetSensibleObjectName()
        {
            return $"Financial Reference Value Set [{FinancialReferenceValueSetId}] - {Name}";
        }
    }
}
