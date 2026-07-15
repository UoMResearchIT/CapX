// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Helpers
{
    /// <summary>
    /// Class to hold all helper methods for assignments endpoints.
    /// </summary>
    internal class AssignmentsHelper
    {
        /// <summary>
        /// Uses the methods used by the finance export to construct assignments for all people in the system in the date range.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        internal static async Task<IList<AssignmentDTO>> GetAssignmentChunksAsync(PPMToolContext context, DateTime? start, DateTime? end)
        {
            // TODO
            return new List<AssignmentDTO>();
        }
    }
}
