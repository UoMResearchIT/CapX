using System.Collections.Generic;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a Project with Associated Skills
    /// </summary>
    public class ProjectWithSkills
    {
        /// <summary>
        ///  The project entity
        /// </summary>
        public Project Project { get; set; }

        /// <summary>
        /// List of skills entities
        /// </summary>
        public IEnumerable<SkillTag> Skills { get; set; }

        /// <summary>
        /// Comma separate skill names (for data grids)
        /// </summary>
        public string SkillNames { get; set; }
    }
}
