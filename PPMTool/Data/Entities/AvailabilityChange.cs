using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a stretch of time with an associated availability for project work in FTE.
    /// </summary>
    public class AvailabilityChange : PersonProperty
    {
        public int AvailabilityChangeId { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public double AvailabilityFTE { get; set; }

        /// <summary>
        /// Notes on their baseline activities or whether they are part time to explain the change
        /// </summary>
        public string BaselineActivities { get; set; }

        public override int GetId()
        {
            return AvailabilityChangeId;
        }
    }
}
