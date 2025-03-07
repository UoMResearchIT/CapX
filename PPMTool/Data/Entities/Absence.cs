using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an absence.
    /// </summary>
    public class Absence : PersonProperty
    {
        public int AbsenceId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public override string GetSensibleObjectName()
        {
            return $"Absence entry for {Person?.Name}";
        }

        /// <summary>
        /// Checks whether this absence indicates that someone is absent right now based on the current date
        /// </summary>
        /// <returns></returns>
        public bool IsCurrentAbsence()
        {
            return StartDate <= DateTime.Today && (EndDate == null || EndDate >= DateTime.Today);
        }
    }
}
