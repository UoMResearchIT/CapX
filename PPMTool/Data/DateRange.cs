using System;

namespace PPMTool.Data
{
    /// <summary>
    /// A helper class to assist with finding how much of the tasks run during a financial year
    /// </summary>
    public class DateRange
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Method to determine whether a testdate is in the date range [startDate endDate].
        /// If end date and start date are the same evaluates against start date.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        static public bool IsWithin(DateTime testDate, DateTime startDate, DateTime endDate)
        {
            return startDate.Date == endDate.Date ? testDate.Date == startDate.Date : testDate.Date >= startDate.Date && testDate.Date <= endDate.Date;
        }

        /// <summary>
        /// Method to determine whether any part of the test range [testStart testEnd] intersects with a date range [startDate endDate].
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="testStart"></param>
        /// <param name="testEnd"></param>
        /// <returns></returns>
        static public bool IsWithin(DateTime testStart, DateTime testEnd, DateTime startDate, DateTime endDate)
        {
            return startDate.Date <= testEnd.Date && endDate.Date >= testStart.Date;
        }
    }
}