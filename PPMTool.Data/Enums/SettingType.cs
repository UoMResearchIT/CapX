using System.ComponentModel;
using PPMTool.Data.Enums.Attributes;

namespace PPMTool.Data.Enums
{
    /// <summary>
    /// Compile-time reference to a setting in the system.
    /// </summary>
    public enum SettingType
    {
        [Description("Name of the organisation to be used in the UI and communications.")]
        [DefaultSettingValue("The University of Manchester - Research IT")]
        OrganisationName = 0,
    }
}
