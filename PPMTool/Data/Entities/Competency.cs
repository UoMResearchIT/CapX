using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class Competency : ILoggableClass
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
        public string CreatedDate { get; set; } = DateTime.Now.ToString("R");

        /// <summary>
        /// Date of the latest revision of the competency
        /// </summary>
        [Required]
        public string RevisionDate { get; set; } = DateTime.Now.ToString("R");

        /// <summary>
        /// Whether this competency is still active or has been retired
        /// </summary>
        [Required]
        public bool IsActive { get; set; }

        /// <summary>
        /// This is the ID of the competency if it existed in v1.8 of the paper version of the framework
        /// </summary>
        public string LegacyId { get; set; }

        /// <summary>
        /// List of assessments that relate to this competency
        /// </summary>
        public ICollection<CompetencyAssessment> Assessments { get; set; } = new List<CompetencyAssessment>();

        public string GetSensibleObjectName()
        {
            return $"{Description} (Rev {Revision})";
        }

        /// <summary>
        /// Get a suitable coded ID for the competency based on hierarchy
        /// </summary>
        /// <returns></returns>
        public string GetHierarchyId()
        {
            return $"{Grade - 4}.{(int)Category + 1}.{999}";
        }
    }
}
