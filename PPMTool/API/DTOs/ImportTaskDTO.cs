// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// One assignment created with a new Project, or with a new Task, on that
    /// Task. The person is identified by username.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="StartDate">Not used: an assignment's period is its Task's</param>
    /// <param name="EndDate">Not used: an assignment's period is its Task's</param>
    public sealed record ImportResourcingDTO(
        string Username,
        double AssignmentFTE,
        DateTime StartDate,
        DateTime EndDate
    );

    /// <summary>
    /// Request body for POST /api/tasks/add. Creates a fixed-duration Task
    /// with a fixed start and end and no predecessor, the shape CapX schedules
    /// without moving its dates.
    /// </summary>
    /// <param name="RTP">RTP of the Project</param>
    /// <param name="Name">Unique within the Project, except for Leadership (ProjectAndServiceMgmt) tasks, as in the UI</param>
    /// <param name="TaskDuty">A <see cref="PPMTool.Data.Enums.Duty"/> name</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after StartDate</param>
    /// <param name="Demand">FTE needed; sets both Demand and OriginalDemand. Greater than zero, at most 3 decimal places.</param>
    /// <param name="Resourcing">Assignments to create on the Task, if any. Each assignee must start by the Task's start, and only managers can go on a Leadership task.</param>
    public sealed record ImportTaskDTO(
        int RTP,
        string Name,
        string TaskDuty,
        DateTime StartDate,
        DateTime EndDate,
        double Demand,
        IReadOnlyList<ImportResourcingDTO>? Resourcing
    );

    /// <summary>Response for a created Task.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record ImportTaskResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/tasks/update. Identifies the Task by
    /// SubTaskId and changes the fields supplied. Doesn't change assignments:
    /// see PUT /api/tasks/resourcing/update. Dates can only be changed on the
    /// shape POST /api/tasks/add creates; on any other Task, CapX would
    /// recalculate them, so change them in the UI. A Project's last
    /// Leadership task can't be given another duty.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/tasks/getAll</param>
    /// <param name="Name">Unique within the Project, as on creation</param>
    /// <param name="TaskDuty">A <see cref="PPMTool.Data.Enums.Duty"/> name</param>
    /// <param name="StartDate"></param>
    /// <param name="EndDate">On or after the resulting StartDate</param>
    /// <param name="Demand">FTE needed, at most 3 decimal places. Changes the current Demand; OriginalDemand keeps what was asked for at creation.</param>
    public sealed record UpdateTaskRequestDTO(
        int SubTaskId,
        string? Name,
        string? TaskDuty,
        DateTime? StartDate,
        DateTime? EndDate,
        double? Demand
    );

    /// <summary>Response for an updated Task.</summary>
    /// <param name="SubTaskId"></param>
    public sealed record UpdateTaskResponseDTO(
        int SubTaskId
    );

    /// <summary>
    /// One assignment to add to an existing Task. Name the person by exactly
    /// one of Username or PersonId; PersonId reaches a Person with no User,
    /// such as one created by POST /api/people/add. An assignment has no dates
    /// of its own: its period is its Task's.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="PersonId">Id of a Person, with or without a User</param>
    /// <param name="AssignmentFTE">Greater than zero, at most 3 decimal places</param>
    /// <param name="IsProvisional">Defaults to true</param>
    public sealed record ImportResourceAssignmentDTO(
        string? Username,
        int? PersonId,
        double AssignmentFTE,
        bool? IsProvisional
    );

    /// <summary>
    /// Request body for POST /api/tasks/resourcing/add. Adds assignments to an
    /// existing Task. A person already assigned to the Task is rejected rather
    /// than duplicated: use PUT /api/tasks/resourcing/update to change their
    /// assignment. Not allowed on a fixed-work Task, whose dates CapX
    /// recalculates from the FTE assigned to it.
    /// </summary>
    /// <param name="SubTaskId">From GET /api/tasks/getAll</param>
    /// <param name="Resourcing">At least one assignment</param>
    public sealed record ImportTaskResourcingRequestDTO(
        int SubTaskId,
        IReadOnlyList<ImportResourceAssignmentDTO> Resourcing
    );

    /// <summary>Response for added assignments.</summary>
    /// <param name="SubTaskId"></param>
    /// <param name="ResourcesCreated"></param>
    public sealed record ImportTaskResourcingResponseDTO(
        int SubTaskId,
        int ResourcesCreated
    );

    /// <summary>
    /// Request body for PUT /api/tasks/resourcing/update. Identifies the
    /// assignment by ResourceId and changes the fields supplied. Setting
    /// AssignmentFTE to zero retracts the assignment; there is no DELETE.
    /// Moving it to another Task (NewSubTaskId) is how its period changes.
    /// Not allowed when either Task is fixed-work.
    /// </summary>
    /// <param name="ResourceId">From GET /api/tasks/resourcing/getAll</param>
    /// <param name="AssignmentFTE">Zero or more, at most 3 decimal places</param>
    /// <param name="NewSubTaskId">Task to move the assignment to</param>
    /// <param name="IsProvisional"></param>
    public sealed record UpdateTaskResourcingRequestDTO(
        int ResourceId,
        double? AssignmentFTE,
        int? NewSubTaskId,
        bool? IsProvisional
    );

    /// <summary>Response for an updated assignment.</summary>
    /// <param name="ResourceId"></param>
    /// <param name="SubTaskId">The Task the assignment is now on</param>
    public sealed record UpdateTaskResourcingResponseDTO(
        int ResourceId,
        int SubTaskId
    );
}
