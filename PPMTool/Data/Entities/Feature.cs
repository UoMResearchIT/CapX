using System.ComponentModel.DataAnnotations;

namespace PPMTool.Data.Entities
{
    public class Feature
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int FeatureId { get; set; }

        /// <summary>
        /// Name of the feature
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Description of the feature
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// State of the feature (enabled/disabled)
        /// </summary>
        [Required]
        public bool Enabled { get; set; }

        /// <summary>
        /// This indicates if a feature cannot be turned off as it is so fundamental to how the app works
        /// </summary>
        [Required]
        public bool MustAlwaysBeEnabled { get; set; }
    }
}
