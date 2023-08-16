using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents a person as a resource to be assigned to a subtask
    /// </summary>
    public class Resource
    {
        public int ResourceId { get; set; }

        public Person Person { get; set; }

        /// <summary>
        /// This is the day rate associated with this resource assignment.
        /// If this is null when creating the resource, it will be assigned based
        /// on the default day rate for the person.
        /// </summary>
        [DataType(DataType.Currency)]
        public double? DayRate { get; set; }

        public double Percentage { get; set; }

        public bool IsProvisional { get; set; }

        private bool useDefaultDayRate = true;
        public bool UseDefaultDayRate
        {
            get => useDefaultDayRate;
            set
            {
                if (useDefaultDayRate != value)
                {
                    useDefaultDayRate = value;

                    // Set the day rate
                    if (!value) DayRate = 0;
                    else DayRate = null;

                }
            }
        }
    }
}
