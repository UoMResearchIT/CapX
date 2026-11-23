// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a sub-task suitable for API data exports.
    /// </summary>
    /// <param name="SubTaskId">The unique sub-task ID.</param>
    /// <param name="OwningProjectProjectId">The ID of the project that owns the sub-task.</param>
    /// <param name="Name">The name of the sub-task.</param>
    /// <param name="StartDate">The start date of the sub-task.</param>
    /// <param name="EndDate">The end date of the sub-task.</param>
    /// <param name="TaskDuty">The task duty, as its description. The write endpoints accept this or the enum name.</param>
    /// <param name="Demand">The FTE the task needs.</param>
    /// <param name="OriginalDemand">The demand when the task was created.</param>
    /// <param name="UnmetDemand">Demand not yet covered by assignments.</param>
    public sealed record SubTaskDTO(
        int SubTaskId,
        int OwningProjectProjectId,
        string Name,
        DateOnly StartDate,
        DateOnly EndDate,
        string TaskDuty,
        double Demand,
        double OriginalDemand,
        double UnmetDemand
    );

    /// <summary>
    /// Request body for POST /api/subtasks/add. Creates a fixed-duration Task
    /// with a fixed start and end and no predecessor, the shape CapX schedules
    /// without moving its dates.
    /// Kept separate from <see cref="SubTaskDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
    /// </summary>
    /// <param name="CapXProjectId">CapX project id of the Project</param>
    /// <param name="Name">Unique within the Project, except for Leadership (ProjectAndServiceMgmt) tasks, as in the UI</param>
    /// <param name="TaskDuty">A <see cref="PPMTool.Data.Enums.Duty"/> name, or its description as task reads return it</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after StartDate</param>
    /// <param name="Demand">FTE needed; sets both Demand and OriginalDemand. Greater than zero, at most 3 decimal places.</param>
    /// <param name="Resourcing">Assignments to create on the Task, if any. Each assignee must start by the Task's start, and only managers can go on a Leadership task.</param>
    public sealed record WriteSubTaskDTO(
        int CapXProjectId,
        string Name,
        string TaskDuty,
        DateTime StartDate,
        DateTime EndDate,
        double Demand,
        IReadOnlyList<WriteSubTaskResourcingDTO>? Resourcing
    );

    /// <summary>Response for a created Task.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record WriteSubTaskResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/subtasks/update. Identifies the Task by
    /// SubTaskId and changes the fields supplied. Doesn't change assignments:
    /// see PUT /api/resources/update. Dates can only be changed on the
    /// shape POST /api/subtasks/add creates; on any other Task, CapX would
    /// recalculate them, so change them in the UI. A Project's last
    /// Leadership task can't be given another duty.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/subtasks/getAll</param>
    /// <param name="Name">Unique within the Project, as on creation</param>
    /// <param name="TaskDuty">A <see cref="PPMTool.Data.Enums.Duty"/> name, or its description as task reads return it</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after the resulting StartDate</param>
    /// <param name="Demand">FTE needed, at most 3 decimal places. Changes the current Demand; OriginalDemand keeps what was asked for at creation.</param>
    public sealed record UpdateSubTaskRequestDTO(
        int SubTaskId,
        string? Name,
        string? TaskDuty,
        DateTime? StartDate,
        DateTime? EndDate,
        double? Demand
    );

    /// <summary>Response for an updated Task.</summary>
    /// <param name="SubTaskId"></param>
    public sealed record UpdateSubTaskResponseDTO(
        int SubTaskId
    );

    /// <summary>
    /// One assignment created with a new Project, or with a new Task, on that
    /// Task. The person is identified by username.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="StartDate">Not used: an assignment's period is its Task's</param>
    /// <param name="EndDate">Not used: an assignment's period is its Task's</param>
    public sealed record WriteSubTaskResourcingDTO(
        string Username,
        double AssignmentFTE,
        DateTime StartDate,
        DateTime EndDate
    );
}
