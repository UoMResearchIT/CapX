using System;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    /// <summary>
    /// Represents the workload model of a person and the date when it came into effect
    /// </summary>
    public class WorkloadModelChange : PersonProperty
    {
        public int WorkloadModelChangeId { get; set; }

        [Required]
        public DateTime ChangeDate { get; set; }

        [Required]
        public double ProjectWorkFTE { get; set; }

        [Required]
        public double BusinessAsUsualFTE { get; set; }

        [Required]
        public double PersonalDevelopmentFTE { get; set; }

        [Required]
        public double StaffManagementFTE { get; set; }

        [Required]
        public double ProjectAndServiceManagementFTE { get; set; }

        [Required]
        public double ArchitectureFTE { get; set; }


        /// <summary>
        /// Optional notes to explain anything about the change
        /// </summary>
        public string BaselineActivities { get; set; }
    }
}
