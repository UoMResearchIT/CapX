using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NodaMoney;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an RSE
    /// </summary>
    public class Person
    {
        public int PersonId { get; set; }

        public string Name { get; set; }

        public string ShortName { get; private set; }

        public Money HourlyRate { get; set; }

        public double AvailabilityFTE { get; set; }

        /// <summary>
        /// When this person is next available for assigned calculated from their project assignments
        /// </summary>
        public DateTime NextAvailable { get; private set; }

        public IList<string> SkillTags { get; set; }
    }
}
