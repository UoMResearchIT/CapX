using System.ComponentModel.DataAnnotations;
using PPMTool.Enums;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an assessment against a competency
    /// </summary>
    public class CompetencyAssessment : ILoggableClass
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
        public string DateCreated { get; set; } = DateTime.Now.ToString("R");

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
        /// Description of the revision of the competency this assessment is associated with.
        /// </summary>
        [Required]
        public string CompetencyDescription { get; set; }

        /// <summary>
        /// Objective of the revision of the competency this assessment is associated with.
        /// </summary>
        [Required]
        public string CompetencyObjective { get; set; }

        /// <summary>
        /// ID of the competency this assessment relates to
        /// </summary>
        [Required]
        public int CompetencyId { get; set; }

        /// <summary>
        /// A reference to the person who is authoring this assessment
        /// </summary>
        public Person Person { get; set; }

        /// <summary>
        /// A sensible object name for logging purposes
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"{Person?.Name} | {Evidence} | Competency Id {CompetencyId} | Rev {CompetencyRevision}";
        }
    }
}
