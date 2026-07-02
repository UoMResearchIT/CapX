// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;

namespace PPMTool.Data.Entities
{
    public class Feature
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int FeatureId { get; set; }

        /// <summary>
        /// Compile time reference to the feature in the system
        /// </summary>
        [Required]
        public FeatureType FeatureType { get; set; }

        /// <summary>
        /// Name of the feature
        /// </summary>
        [Required]
        public string Name { get; set; } = null!;

        /// <summary>
        /// Description of the feature
        /// </summary>
        [Required]
        public string Description { get; set; } = null!;

        /// <summary>
        /// State of the feature (enabled/disabled)
        /// </summary>
        [Required]
        public bool Enabled { get; set; }

        /// <summary>
        /// This indicates if a feature cannot be turned off as it is so fundamental to how the app works
        /// </summary>
        [Required]
        public bool MustAlwaysBeEnabled { get; set; }
    }
}
