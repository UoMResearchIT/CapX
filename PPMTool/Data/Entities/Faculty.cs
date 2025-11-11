namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an institution or faculty which owns a number of <see cref="School"/>
    /// </summary>
    public class Faculty : BaseOrgUnit
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int FacultyId { get; set; }

        /// <summary>
        /// Navigation property for the schools or departments within this faculty
        /// </summary>
        public virtual ICollection<School> Schools { get; set; } = new List<School>();
    }
}
