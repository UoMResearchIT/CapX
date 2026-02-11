// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

namespace PPMTool.Data
{
    /// <summary>
    /// Stores app-wide default values
    /// </summary>
    public static class GlobalDefaults
    {
        /// <summary>
        /// Default amount of time the project management tasks take up in FTE per project
        /// </summary>
        public static readonly float ProjectManagementDefaultFTE = 0.05f;

        /// <summary>
        /// Default amount of time the staff management tasks take up in FTE per person
        /// </summary>
        public static readonly float StaffManagementDefaultFTE = 0.05f;

        /// <summary>
        /// Default amount of technical leadership required in FTE per project
        /// </summary>
        public static readonly float TechnicalLeadershipDefaultFTE = 0.05f;

        /// <summary>
        /// Default staff in the team assumed to be line managed by the head of the team and hence not factored into the staff management FTE demand
        /// </summary>
        public static readonly int NumberOfStaffManagedByHeadDefault = 5;

        /// <summary>
        /// Default day rate for day rate based projects
        /// </summary>
        public static readonly float DayRateDefault = 300;

        /// <summary>
        /// The default "indirect" rate for assignments.
        /// This represents the proportion of an assignment that should be billed over and above the value of the assignment.
        /// Another way of thinking about it is the amount of budget that should be skimmed off the top to cover BAU activities.
        /// </summary>
        public static readonly float BAUTopSliceFractionDefault = 0.125f;

    }
}
