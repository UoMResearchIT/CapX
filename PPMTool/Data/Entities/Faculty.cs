using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an institution or faculty which owns a number of <see cref="School"/>
    /// </summary>
    public class Faculty : BaseOrgUnit
    {
        public int FacultyId { get; set; }

        /// <summary>
        /// Navigation property for the schools or departments within this faculty
        /// </summary>
        [Required]
        public virtual ICollection<School> Schools { get; set; } = new List<School>();
    }
}
