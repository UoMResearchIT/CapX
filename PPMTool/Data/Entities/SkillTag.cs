using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PPMTool.Data.Entities
{
    public class SkillTag : ILoggableClass
    {
        public int SkillTagId { get; set; }

        /// <summary>
        /// This is the name of the skill tag as visible to users
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// This is the controlled vocabulary name for the skill tag -- historical has come from Wikipedia main entries
        /// </summary>
        [Required]
        public string ControlledName { get; set; }

        /// <summary>
        /// The people who have this skill (not serialisable to avoid circular references)
        /// </summary>
        [JsonIgnore]
        public ICollection<Person> People { get; set; }

        /// <summary>
        /// Required override for logging identification
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"Skill Tag: {Name}";
        }
    }
}
