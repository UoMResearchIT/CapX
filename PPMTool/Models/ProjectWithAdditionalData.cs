// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Entities;

namespace PPMTool.Models
{
    /// <summary>
    /// Represents a Project with Skills and Assigned RSEs resolved
    /// </summary>
    public class ProjectWithAdditionalData
    {
        /// <summary>
        ///  The project entity
        /// </summary>
        public Project Project { get; set; } = null!;

        /// <summary>
        /// List of skills entities
        /// </summary>
        public IEnumerable<SkillTag> Skills { get; set; }

        /// <summary>
        /// List of names of people who are assigned to the project
        /// </summary>
        public IEnumerable<string> AssignedResourceNames { get; set; }
    }
}
