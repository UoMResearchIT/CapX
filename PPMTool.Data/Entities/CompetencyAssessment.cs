using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;

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
        public string Evidence { get; set; } = null!;

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
        public string CompetencyDescription { get; set; } = null!;

        /// <summary>
        /// Objective of the revision of the competency this assessment is associated with.
        /// </summary>
        [Required]
        public string CompetencyObjective { get; set; } = null!;

        /// <summary>
        /// ID of the competency this assessment relates to
        /// </summary>
        [Required]
        public int CompetencyId { get; set; }

        /// <summary>
        /// ID of the person who is authoring this assessment
        /// </summary>
        [Required]
        public int PersonId { get; set; }

        /// <summary>
        /// A sensible object name for logging purposes
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"Person Id: {PersonId} | Evidence: {Evidence} | Competency Id: {CompetencyId} | Rev: {CompetencyRevision}";
        }
    }
}
