// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// A generic finance item
    /// </summary>
    public abstract class FinanceItem : BaseFinanceItem
    {
        /// <summary>
        /// A key date associated with the item (e.g. invoice raised or payment received)
        /// </summary>
        [Required]
        public DateTime KeyDate { get; set; } = DateTime.Today;

        /// <summary>
        /// Value of the item
        /// </summary>
        [Required]
        public double Value { get; set; }

        /// <summary>
        /// Details of the item as free text not captured by other fields
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Method to get a suitable description of the item for posting to a note
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            return Description;
        }
    }
}
