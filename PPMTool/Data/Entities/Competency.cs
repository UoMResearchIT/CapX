using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class Competency
    {
        public int CompetencyId { get; set; }

        /// <summary>
        /// Description of the competency
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// HTML of how the competency can be demonstrated via a SMART objective
        /// </summary>
        [Required]
        public string Objective { get; set; }

        /// <summary>
        /// Grade to which this competency belongs
        /// </summary>
        [Required]
        public int Grade { get; set; }

        /// <summary>
        /// Category of the competency
        /// </summary>
        [Required]
        public CompetencyCategory Category { get; set; }

        /// <summary>
        /// Revision number of the competency. If edited, this will auto-increment.
        /// </summary>
        [Required]
        public int Revision { get; set; } = 0;

        /// <summary>
        /// When the competency was originally created (at revision 0)
        /// </summary>
        [Required]
        public string CreatedDate { get; set; } = DateTime.Today.ToString("R");

        /// <summary>
        /// Date of the latest revision of the competency
        /// </summary>
        [Required]
        public string RevisionDate { get; set; }

        /// <summary>
        /// Whether this competency is still active or has been retired
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// List of assessments that relate to this competency
        /// </summary>
        public ICollection<CompetencyAssessment> Assessments { get; set; } = new List<CompetencyAssessment>();

    }
}
