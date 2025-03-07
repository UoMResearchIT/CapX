// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// A generic finance item
    /// </summary>
    public abstract class FinanceItem
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
        /// Project associated with this item
        /// </summary>
        [Required]
        public Project Project { get; set; }
    }
}
