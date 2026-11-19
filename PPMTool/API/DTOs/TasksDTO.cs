// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// One task on a Project, as GET /api/tasks/getAll returns it.
    /// </summary>
    /// <param name="SubTaskId"></param>
    /// <param name="RTP"></param>
    /// <param name="Name"></param>
    /// <param name="TaskDuty"></param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate"></param>
    /// <param name="Demand">Current FTE demand</param>
    /// <param name="OriginalDemand">FTE demand when the task was created; unchanged by later updates</param>
    /// <param name="UnmetDemand">Demand minus assigned resource FTE</param>
    public sealed record TaskDTO(
        int SubTaskId,
        int RTP,
        string Name,
        string TaskDuty,
        DateTime StartDate,
        DateTime EndDate,
        double Demand,
        double OriginalDemand,
        double UnmetDemand
    );

    /// <summary>
    /// One person's assignment to a task, as GET /api/tasks/resourcing/getAll
    /// returns it. An assignment has no dates of its own; StartDate and EndDate
    /// are its task's.
    /// </summary>
    /// <param name="ResourceId">Identifies this assignment for PUT /api/tasks/resourcing/update</param>
    /// <param name="SubTaskId"></param>
    /// <param name="RTP">RTP of the Project owning the task</param>
    /// <param name="TaskName"></param>
    /// <param name="TaskDuty"></param>
    /// <param name="StartDate">The task's start</param>
    /// <param name="EndDate">The task's end</param>
    /// <param name="PersonId"></param>
    /// <param name="PersonName"></param>
    /// <param name="Username">Access Control username of the person's linked User, or null for a Person with no login</param>
    /// <param name="AssignmentFTE"></param>
    /// <param name="IsProvisional">Whether the assignment is flagged provisional rather than confirmed</param>
    public sealed record TaskResourceDTO(
        int ResourceId,
        int SubTaskId,
        int RTP,
        string TaskName,
        string TaskDuty,
        DateTime StartDate,
        DateTime EndDate,
        int PersonId,
        string PersonName,
        string? Username,
        double AssignmentFTE,
        bool IsProvisional
    );
}
