using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a school or department within a <see cref="Data.Entities.Faculty" />
    /// </summary>
    public class School : ILoggableClass
    {
        public int SchoolId { get; set; }

        /// <summary>
        /// Name of the organisational unit.
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Abbreviated name or code of the organisational unit.
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// Description to help identify it to admins
        /// </summary>
        [Required]
        public string Description { get; set; }

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

        public string GetSensibleObjectName()
        {
            return (Code != null && Code.Length > 0 ? $"{Name} ({Code})" : $"{Name}");
        }

        /// <summary>
        /// The faculty to which this school or department belongs
        /// </summary>
        [Required]
        public Faculty Faculty { get; set; }
    }
}
