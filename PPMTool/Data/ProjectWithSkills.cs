using System.Collections.Generic;
using PPMTool.Data.Entities;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents a Project with Associated Skills
    /// </summary>
    public class ProjectWithSkills
    {
        public Project Project { get; set; }
        public IEnumerable<SkillTag> Skills { get; set; }
    }
}
