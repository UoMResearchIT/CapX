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
    /// <param name="PersonId">The ID of the person.</param>
    /// <param name="PersonName">The name of the person.</param>
    /// <param name="Grade">Grade of the person.</param>
    /// <param name="FTE">FTE of the assignment.</param>
    /// <param name="TaskId">The ID of the task.</param>
    /// <param name="TaskName">The name of the task.</param>
    /// <param name="StartDate">The start date of the assignment.</param>
    /// <param name="EndDate">The end date of the assignment.</param>
    /// <param name="AssignmentType">The duty of the assignment.</param>
    public sealed record AssignmentDTO(
        int ProjectId,
        string ProjectName,
        string ProjectStatus,
        int PersonId,
        string PersonName,
        int Grade,
        double FTE,
        int TaskId,
        string TaskName,
        DateTime StartDate,
        DateTime EndDate,
        string AssignmentType);
}