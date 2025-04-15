using PPMTool.Data.Entities;

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// DTO grouping person and their skills
    /// </summary>
    public class PersonSkills
    {
        /// <summary>
        /// Name of the person
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Skills owned by the person
        /// </summary>
        public IEnumerable<SkillTag> Skills { get; }

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="name"></param>
        /// <param name="skills"></param>
        public PersonSkills(string name, IEnumerable<SkillTag> skills)
        {
            Name = name;
            Skills = skills;
        }
    }
}
