// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

namespace PPMTool.Data
{
    public interface IWithin
    {
        /// <summary>
        /// Method to determine whether a date is in the range [task.startDate task.endDate].
        /// If end date and start date are the same evaluates against start date.
        /// </summary>
        /// <param name="testDate">Date to test</param>
        /// <returns></returns>
        public abstract bool IsWithin(DateTime testDate);

        /// <summary>
        /// Method to determine whether any part of the task runs within a date range [startDate endDate].
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public abstract bool IsWithin(DateTime startDate, DateTime endDate);
    }
}
