using System.ComponentModel.DataAnnotations;
using PPMTool.Data.Enums;
using PPMTool.Data.Interfaces;

namespace PPMTool.Data.Entities
{
    public class Setting : ILoggableObject
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

        /// <summary>
        /// Override of the method from ILoggableObject to provide a sensible name for the setting when it is logged in the logs.
        /// </summary>
        /// <returns></returns>
        public string GetSensibleObjectName()
        {
            return $"Setting: {SettingType} | Value: {SettingValue}";
        }
    }
}
