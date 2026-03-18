using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents an organisational unit (faculty, school, department, etc)
    /// </summary>
    public abstract class BaseOrgUnit : ILoggableClass
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
            return $"{Name} ({Code})";
        }

        /// <summary>
        /// Method to check that the name and code have a value
        /// </summary>
        /// <returns></returns>
        internal bool Validate()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Code);
        }

        /// <summary>
        /// Return the ID of the entity
        /// </summary>
        /// <returns></returns>
        public abstract int GetId();
    }
}
