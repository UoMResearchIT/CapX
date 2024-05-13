using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an RSE available for project work
    /// </summary>
    public class Person : ObjectWithStatusMessages
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
        public double DayRate { get; set; }

        [Required]
        public DateTime StartDate { get; set; } = DateTime.Today;

        public DateTime? EndDate { get; set; }

        [Required]
        public double FTE { get; set; } = 1.0;

        /// <summary>
        /// Any changes to their availability which includes the undertaking of baseline activities
        /// </summary>
        public ICollection<AvailabilityChange> AvailabilityChanges { get; set; } = new List<AvailabilityChange>();

        /// <summary>
        /// Collection of skills
        /// </summary>
        public ICollection<SkillTag> SkillTags { get; set; }

        /// <summary>
        /// Collection of absences
        /// </summary>
        public ICollection<Absence> Absences { get; set; } = new List<Absence>();

        public Person()
        {
            // Generate status messages to be maintained against a project
            statusMessages = new List<StatusMessage>
            {
                new StatusMessage("This person is currently absent.", StatusMessage.MessageType.Info, () => Absences.Any(x => x.StartDate <= DateTime.Today && (x.EndDate == null || x.EndDate >= DateTime.Today)))
            };
        }

        /// <summary>
        /// Updates the initials of the person.
        /// </summary>
        /// <param name="name">Full name</param>
        /// <returns></returns>
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

        /// <summary>
        /// Get the availability of the person on the current date from their availability changes profile
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        internal double GetAvailabilityOnDate(DateTime date)
        {
            // Set as post availability initially
            var availability = FTE;

            // If there are changes then check them
            if (AvailabilityChanges.Count > 0)
            {
                // Get availability based on the most recent change before the date provided
                var latestChange = AvailabilityChanges.Where(x => x.ChangeDate <= date).OrderByDescending(x => x.ChangeDate).FirstOrDefault();
                if (latestChange != null) availability = latestChange.AvailabilityFTE;
            }

            return availability;
        }
    }
}
