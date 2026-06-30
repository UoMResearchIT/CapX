// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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

        [Description("Default \"indirect\" rate for assignments where cost model allows it. This represents a \"tax\" that applies to the computed cost of an assignment to provide core funding for activities linked to team BAU such as training or maintenance.")]
        [DefaultSettingValue("0.125")]
        BAUTopSliceFractionDefault = 7,

        [Description("The abbreviation using to refer to projects.")]
        [DefaultSettingValue("RTP")]
        ProjectAbbreviation = 8,

        [Description("The term used for the upper level organisational unit e.g. Faculty or Institute.")]
        [DefaultSettingValue("Faculty")]
        OrgUnitUpper = 9,

        [Description("The term used for the lower level organisational unit e.g. School or Department.")]
        [DefaultSettingValue("School")]
        OrgUnitLower = 10,

        [Description("The hex code used as the primary colour in the app in light mode e.g. #660099 or #609.")]
        [DefaultSettingValue("#609")]
        AppPrimaryColourLight = 11,

        [Description("The hex code used as the primary colour in the app in dark mode e.g. #660099 or #609.")]
        [DefaultSettingValue("#bb86fc")]
        AppPrimaryColourDark = 12,

        [Description("Whether the header and footer of the app should be coloured as danger for development deployments of the system. Accepts \"true\" and \"false\".")]
        [DefaultSettingValue("true")]
        UseDevelopmentBannerColours = 13,

        [Description("Optional logo that is displayed in the header in light mode.")]
        [DefaultSettingValue("")]
        OrganisationLogoLight = 14,

        [Description("Optional logo that is displayed in the header in dark mode.")]
        [DefaultSettingValue("")]
        OrganisationLogoDark = 15,

        [Description("URL to documentation that provides help for the timesheet functionality.")]
        [DefaultSettingValue("")]
        TimesheetHelpUrl = 16
    }
}
