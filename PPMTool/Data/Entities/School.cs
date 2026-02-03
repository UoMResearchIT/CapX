using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a school or department within a <see cref="Data.Entities.Faculty" />
    /// </summary>
    public class School : BaseOrgUnit
    {
        public int SchoolId { get; set; }

        /// <summary>
        /// The faculty to which this school or department belongs
        /// </summary>
        [Required]
        public Faculty Faculty { get; set; }
    }
}
