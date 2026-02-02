using System.ComponentModel.DataAnnotations;
using DocumentFormat.OpenXml.Spreadsheet;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an institution or faculty which owns a number of <see cref="School"/>
    /// </summary>
    public class Faculty : ILoggableClass
    {
        public int FacultyId { get; set; }

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
        /// Navigation property for the schools or departments within this faculty
        /// </summary>
        [Required]
        public virtual ICollection<School> Schools { get; set; } = new List<School>();
    }
}
