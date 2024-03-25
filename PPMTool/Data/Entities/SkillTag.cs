using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    public class SkillTag : ILoggableClass
    {
        public int SkillTagId { get; set; }

        public string Name { get; set; }

        public ICollection<Person> People { get; set; }

        public string GetSensibleObjectName()
        {
            return Name;
        }
    }
}
