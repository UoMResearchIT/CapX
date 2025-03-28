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
        /// The names of the skills only (useful for filtering in datagrids)
        /// </summary>
        public IEnumerable<string> SkillNames { get; set; }
    }
}
