using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an RSE
    /// </summary>
    public class Person
    {
        public int PersonId { get; set; }

        private string name;
        [Required]
        public string Name
        {
            get => name;
            set { name = value; ShortName = GetInitials(value); }
        }


        public string ShortName { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        public double HourlyRate { get; set; } = 35.72;

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Now.Date;

        [Required]
        public double AvailabilityFTE { get; set; } = 1.0;

        public ICollection<SkillTag> SkillTags { get; set; }

        static string GetInitials(string name)
        {
            string[] nameSplit = name.Split(new string[] { ",", " " }, StringSplitOptions.RemoveEmptyEntries);
            string initials = "";
            foreach (string item in nameSplit)
            {
                initials += item.Substring(0, 1).ToUpper();
            }
            return initials;
        }
    }
}
