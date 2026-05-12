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

        [Description("How any external links to more granular project management tools should be referenced.")]
        [DefaultSettingValue("GitHub Project Board")]
        ProjectBoardName = 1,

        [Description("Default amount of time the project management tasks take up in FTE per project.")]
        [DefaultSettingValue("0.05")]
        ProjectManagementDefaultFTE = 2,

        [Description("Default amount of time the staff management tasks take up in FTE per person")]
        [DefaultSettingValue("0.05")]
        StaffManagementDefaultFTE = 3,

        [Description("Default amount of technical leadership required in FTE per project")]
        [DefaultSettingValue("0.05")]
        TechnicalLeadershipDefaultFTE = 4,

        [Description("Default staff in the team assumed to be line managed by the head of the team and hence not factored into the staff management FTE demand")]
        [DefaultSettingValue("5")]
        NumberOfStaffManagedByHeadDefault = 5,

        [Description("Default day rate for day rate based projects in £")]
        [DefaultSettingValue("300")]
        DayRateDefault = 6,

        [Description("The default \"indirect\" rate for assignments. This represents the proportion of an assignment that should be billed over and above the value of the assignment. Another way of thinking about it is the amount of budget that should be skimmed off the top to cover BAU activities.")]
        [DefaultSettingValue("0.125")]
        BAUTopSliceFractionDefault = 7
    }
}
