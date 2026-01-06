namespace PPMTool.Data
{
    /// <summary>
    /// Stores app-wide default values
    /// </summary>
    public static class GlobalDefaults
    {
        /// <summary>
        /// Default amount of time the project management tasks take up in FTE
        /// </summary>
        public static readonly float ProjectManagementDefaultFTE = 0.05f;

        /// <summary>
        /// Default day rate for day rate based projects
        /// </summary>
        public static readonly float DayRateDefault = 297;

        /// <summary>
        /// The default "indirect" rate for assignments.
        /// This represents the proportion of an assignment that should be billed over and above the value of the assignment.
        /// Another way of thinking about it is the amount of budget that should be skimmed off the top to cover BAU activities.
        /// </summary>
        public static readonly float BAUTopSliceFractionDefault = 0.125f;

    }
}
