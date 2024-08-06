using System;

namespace PPMTool.Data
{
    public class DutyChartItem : BaseDemandChartItem
    {
        /// <summary>
        /// Helper method to compute an average update
        /// </summary>
        /// <param name="value"></param>
        /// <param name="numberOfWeeks"></param>
        /// <returns></returns>
        internal float UpdateAverage(float value, int numberOfWeeks)
        {
            return (float)Math.Round(value * (numberOfWeeks - 1) / numberOfWeeks, 2);
        }

        internal void UpdateMinMax()
        {
            Min = Math.Min(StaffManagementShortfall, PSManagementShortfall);
            Min = Math.Min(Min, RSAShortfall);
            Min = Math.Min(Min, ProjectShortfall);
            Min = (float)Math.Floor(Min);

            Max = Math.Max(StaffManagementShortfall, PSManagementShortfall);
            Max = Math.Max(Max, RSAShortfall);
            Max = Math.Max(Max, ProjectShortfall);
            Max = (float)Math.Ceiling(Max);
        }

        /// <summary>
        /// Shortfall between supply and demand for staff management
        /// </summary>
        public float StaffManagementShortfall { get; set; }

        /// <summary>
        /// Shortfall between supply and demand for PSM
        /// </summary>
        public float PSManagementShortfall { get; set; }

        /// <summary>
        /// Shortfall between supply and demand for RSA
        /// </summary>
        public float RSAShortfall { get; set; }

        /// <summary>
        /// Shortfall between supply and demand for projects
        /// </summary>
        public float ProjectShortfall { get; set; }

        /// <summary>
        /// Minimum shortfall of all of them
        /// </summary>
        public float Min { get; private set; }

        /// <summary>
        /// Maximum shortfall of all of them
        /// </summary>
        public float Max { get; private set; }
    }
}
