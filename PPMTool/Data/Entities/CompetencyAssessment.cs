using System;
using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    public class CompetencyAssessment
    {
        public int CompetencyAssessmentId { get; set; }

        /// <summary>
        /// HTML string detailing the evidence supporting the assessment.
        /// </summary>
        [Required]
        public string Evidence { get; set; }

        /// <summary>
        /// Date the assessment was created.
        /// </summary>
        [Required]
        public string DateCreated { get; set; } = DateTime.Today.ToString("R");

        /// <summary>
        /// Status of the competency based on this assessment.
        /// </summary>
        public AssessmentStatus Status { get; set; }

        /// <summary>
        /// Revision of the competency this assessment is associated with.
        /// </summary>
        [Required]
        public int CompetencyRevision { get; set; }

        /// <summary>
        /// A reference to the competency this assessment relates to
        /// </summary>
        public Competency AssociatedCompetency { get; set; }

        /// <summary>
        /// A reference to the person who is authoring this assessment
        /// </summary>
        public Person Person { get; set; }
    }
}
