using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data
{
    /// <summary>
    /// Represents an RSE
    /// </summary>
    public class Person
    {
        public int PersonId { get; set; }

        [Required]
        public string Name { get; set; }

        public string ShortName { get; private set; }

        [Required]
        [DataType(DataType.Currency)]
        public double HourlyRate { get; set; } = 35.72;

        [Required]
        public double AvailabilityFTE { get; set; } = 1.0;

        /// <summary>
        /// When this person is next available for assigned calculated from their project assignments
        /// </summary>
        public DateTime NextAvailable { get; private set; }

        public IList<SkillTag> SkillTags { get; set; }
    }
}
