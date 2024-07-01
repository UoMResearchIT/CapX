using System.Collections.Generic;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class SkillTag : ILoggableClass, IEntity
    {
        public int SkillTagId { get; set; }

        public string Name { get; set; }

        public ICollection<Person> People { get; set; }

        public int GetId()
        {
            return SkillTagId;
        }

        public string GetSensibleObjectName()
        {
            return Name;
        }
    }
}
