// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.Models
{
    /// <summary>
    /// Class to hold information about request clock details for a given user to plot on the request summary chart.
    /// </summary>
    public class RequestClockSummary
    {
        /// <summary>
        /// Which manager this is a summary for
        /// </summary>
        public string RequestOwner { get; }

        /// <summary>
        /// Number of requests that have breached the duration limit
        /// </summary>
        public int RedCount { get; }

        /// <summary>
        /// Number of requests that are approaching the duration limit
        /// </summary>
        public int AmberCount { get; }

        /// <summary>
        /// Number of requests that are within the duration limit
        /// </summary>
        public int GreenCount { get; }

        /// <summary>
        /// Total number of requests for this manager
        /// </summary>
        public int TotalCount => RedCount + AmberCount + GreenCount;

        public RequestClockSummary(
            string requestOwner,
            int redCount,
            int amberCount,
            int greenCount
        )
        {
            RequestOwner = requestOwner;
            RedCount = redCount;
            AmberCount = amberCount;
            GreenCount = greenCount;
        }
    }
}
