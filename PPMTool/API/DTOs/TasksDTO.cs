// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
}
