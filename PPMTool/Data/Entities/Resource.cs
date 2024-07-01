using System.ComponentModel.DataAnnotations;
using PPMTool.Pages;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource : ILoggableClass, IEntity
    {
        public int ResourceId { get; set; }

        public Person Person { get; set; }

        /// <summary>
        /// This is the day rate associated with this resource assignment.
        /// If this is null when creating the resource, it will be assigned based
        /// on the default day rate for the project.
        /// </summary>
        [DataType(DataType.Currency)]
        public double? DayRate { get; set; }

        public double AssignmentFTE { get; set; }

        public bool IsProvisional { get; set; }

        private bool useProjectDayRate = true;
        public bool UseProjectDayRate
        {
            get => useProjectDayRate;
            set
            {
                if (useProjectDayRate != value)
                {
                    useProjectDayRate = value;

                    // Set the day rate
                    if (!value) DayRate = 0;
                    else DayRate = null;

                }
            }
        }

        public string GetSensibleObjectName()
        {
            return $"{Person?.Name} (Resource)";
        }

        public int GetId()
        {
            return ResourceId;
        }
    }
}
