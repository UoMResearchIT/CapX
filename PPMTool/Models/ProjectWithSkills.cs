// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Entities;

namespace PPMTool.Models
{
    /// <summary>
    /// Represents a Project with Associated Skills
    /// </summary>
    public class ProjectWithSkills
    {
        /// <summary>
        ///  The project entity
        /// </summary>
        public Project Project { get; set; } = null!;

        /// <summary>
        /// List of skills entities
        /// </summary>
        public IEnumerable<SkillTag> Skills { get; set; }
    }
}
