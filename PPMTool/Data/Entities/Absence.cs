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

        public override int GetId()
        {
            return AbsenceId;
        }
    }
}
