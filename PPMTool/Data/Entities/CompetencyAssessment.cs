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
        /// ID of the person who is authoring this assessment
        /// </summary>
        [Required]
        public int PersonId { get; set; }

        /// <summary>
        /// Copy all editable property values from another assessment onto this instance.
        /// Used when applying changes from a detached editing clone back onto a tracked entity,
        /// and when creating a detached clone for editing.
        /// </summary>
        /// <param name="source">The assessment to copy values from.</param>
        public void CopyValuesFrom(CompetencyAssessment source)
        {
            Evidence = source.Evidence;
            DateCreated = source.DateCreated;
            Status = source.Status;
            CompetencyRevision = source.CompetencyRevision;
            CompetencyDescription = source.CompetencyDescription;
            CompetencyObjective = source.CompetencyObjective;
            CompetencyId = source.CompetencyId;
            PersonId = source.PersonId;
        }

        /// <summary>
        /// Create a detached copy of this assessment for editing without affecting EF Core tracking.
        /// </summary>
        /// <returns>A new <see cref="CompetencyAssessment"/> with the same property values.</returns>
        public CompetencyAssessment Clone()
        {
            var clone = new CompetencyAssessment
            {
                CompetencyAssessmentId = CompetencyAssessmentId
            };
            clone.CopyValuesFrom(this);
            return clone;
        }

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
