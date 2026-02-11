// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace PPMTool.API.Endpoints
{
    /// <summary>
    /// Provides general extensions methods for common repeatedable actions in minimal API endpoints.
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Returns the date of the Monday of the week for the given date.
        /// </summary>
        public static DateTime StartOfWeek(this DateTime dt)
        {
            int diff = (7 + (dt.DayOfWeek - DayOfWeek.Monday)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }
    }
}