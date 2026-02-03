using System.ComponentModel.DataAnnotations;

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
        /// Description to help identify it to admins
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Abbreviated name or code of the organisational unit.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// A value to be used to control the ordering in dropdowns.
        /// Defaults to zero and in that case the Id will be used instead.
        /// </summary>
        [Required]
        public int Order { get; set; } = 0;

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
    }
}
