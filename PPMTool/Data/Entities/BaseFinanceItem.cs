// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public abstract class BaseFinanceItem
    {
        /// <summary>
        /// Project associated with this item
        /// </summary>
        [Required]
        public virtual Project Project { get; set; }

        /// <summary>
        /// Method to get a suitable description of the item for posting to a note
        /// </summary>
        /// <returns></returns>
        public abstract string GetDescription();
    }
}
