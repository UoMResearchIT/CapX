// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A single assignment block for a person.
    /// </summary>
    /// <param name="ProjectId">The ID of the project.</param>
    /// <param name="ProjectName">The name of the project.</param>
    /// <param name="ProjectStatus">The status of the project.</param>
    /// <param name="PersonName">The name of the person.</param>
    /// <param name="Grade">Grade of the person.</param>
    /// <param name="FTE">FTE of the assignment.</param>
    /// <param name="TaskName">The name of the task.</param>
    /// <param name="StartDate">The start date of the assignment.</param>
    /// <param name="EndDate">The end date of the assignment.</param>
    /// <param name="LeadershipTask">Whether this assignment is a leadership assignment.</param>
    /// 
    public sealed record AssignmentDTO(
        int ProjectId,
        string ProjectName,
        string ProjectStatus,
        string PersonName,
        int Grade,
        double FTE,
        string TaskName,
        DateTime StartDate,
        DateTime EndDate,
        bool LeadershipTask);
}