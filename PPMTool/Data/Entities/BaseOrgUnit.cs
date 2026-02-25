using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an organisational unit (faculty, school, department, etc)
    /// </summary>
    public class BaseOrgUnit : ILoggableClass
    {
        /// <summary>
        /// Name of the school or department
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Abbreviated name or code of the organisational unit.
        /// </summary>
        [Required]
        public string Code { get; set; }

        /// <summary>
        /// For soft deletion/visibility toggling
        /// </summary>
        [Required]

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Get a sensible name for this object
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return (Code != null && Code.Length > 0 ? $"{Name} ({Code})" : $"{Name}");
        }

        [NotMapped]
        public bool InEditMode { get; set; } = false;
    }
}
