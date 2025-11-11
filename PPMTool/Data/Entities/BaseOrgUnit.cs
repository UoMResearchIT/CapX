using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an organisational unit (faculty, school, department, etc)
    /// </summary>
    public abstract class BaseOrgUnit
    {
        /// <summary>
        /// Name of the school or department
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Description to help identify it to admins
        /// </summary>
        [Required]
        public string Description { get; set; }
    }
}
