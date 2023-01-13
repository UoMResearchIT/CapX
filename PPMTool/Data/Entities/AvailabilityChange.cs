using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a stretch of time with an associated availability for project work in FTE.
    /// </summary>
    public class AvailabilityChange
    {

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public double AvailabilityFTE { get; set; }
    }
}
