// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

// TaskResourceDTO.Username is genuinely nullable (a Person can have no
// linked User), same as PersonDTO.Username -- matching ImportDTO.cs,
// which enables this file-scoped rather than leaving a CS8632 behind.
#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// One SubTask on a Project. Read-only summary matching the same
    /// fixed-duration, no-predecessor shape this API's write side
    /// supports (see ImportTaskDTO remarks in ImportDTO.cs) -- doesn't
    /// expose TaskType, Predecessor/Lag, or SkillsRequired, since this
    /// API doesn't let you set those either.
    /// </summary>
    /// <param name="SubTaskId"></param>
    /// <param name="RTP"></param>
    /// <param name="Name"></param>
    /// <param name="TaskDuty"></param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate"></param>
    /// <param name="Demand">Current FTE demand</param>
    /// <param name="OriginalDemand">FTE demand when the task was first created -- doesn't change on a later Demand update</param>
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
    /// One person's resourcing assignment to one SubTask, as returned by
    /// GET /api/tasks/resourcing/getAll.
    ///
    /// A Resource carries no dates of its own -- the assignment's period
    /// is its SubTask's period (see the Resource entity, which has no
    /// StartDate/EndDate). StartDate/EndDate are repeated here from the
    /// owning task so a caller can reconcile an assignment against a
    /// dated allocation without a second lookup, and so the consequence
    /// of that modelling is visible rather than surprising: an assignment
    /// covering a different period is a different task.
    /// </summary>
    /// <param name="ResourceId">Identifies this assignment for PUT /api/tasks/resourcing/update</param>
    /// <param name="SubTaskId"></param>
    /// <param name="RTP">RTP of the Project owning the task</param>
    /// <param name="TaskName"></param>
    /// <param name="TaskDuty"></param>
    /// <param name="StartDate">The owning task's start -- see remarks</param>
    /// <param name="EndDate">The owning task's end -- see remarks</param>
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
