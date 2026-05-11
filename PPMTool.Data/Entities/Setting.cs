using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;

namespace PPMTool.Data.Entities
{
    public class Setting
    {
        /// <summary>
        /// Primary key
        /// </summary>
        public int SettingId { get; set; }

        /// <summary>
        /// Name of the setting as it will be referenced in the code.
        /// </summary>
        [Required]
        public SettingType SettingType { get; set; }

        /// <summary>
        /// Value of the setting as a string.
        /// </summary>  
        [Required]
        public string SettingValue { get; set; } = null!;

        /// <summary>
        /// Optional description of the setting to be used in the UI to explain the setting to users.
        /// </summary>
        public string Description { get; set; } = null!;
    }
}
