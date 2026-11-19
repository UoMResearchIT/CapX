// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Request body for POST /api/workloadmodels/add. A person's duty split,
    /// in effect from ChangeDate until their next change, with the same
    /// ranges the UI enforces. ProjectAndServiceManagementFTE is derived from
    /// ProjectManagementFTE and ServiceManagementFTE. A change on the same
    /// date as an existing one updates it.
    /// </summary>
    /// <param name="Username">Username of a User with a linked Person</param>
    /// <param name="ChangeDate">With no time of day</param>
    /// <param name="Grade">4-9</param>
    /// <param name="ProjectWorkFTE">0.0-1.0</param>
    /// <param name="BusinessAsUsualFTE">0.0-1.0</param>
    /// <param name="PersonalDevelopmentFTE">0.0-1.0</param>
    /// <param name="StaffManagementFTE">0.0-1.0</param>
    /// <param name="ArchitectureFTE">0.0-1.0</param>
    /// <param name="ServiceManagementFTE">0.0-1.0</param>
    /// <param name="ProjectManagementFTE">0.0-1.0</param>
    /// <param name="Notes"></param>
    /// <param name="CostValueKey">Cost key: the name of the financial reference value set that prices the person's time (e.g. "Grade65Costs"), as GET /api/wlm/getAll returns it. Without one, assignments under this change are costed at zero, as in the UI. Null leaves an existing change's key unchanged.</param>
    public sealed record ImportWorkloadModelChangeDTO(
        string Username,
        DateTime ChangeDate,
        int Grade,
        double ProjectWorkFTE,
        double BusinessAsUsualFTE,
        double PersonalDevelopmentFTE,
        double StaffManagementFTE,
        double ArchitectureFTE,
        double ServiceManagementFTE,
        double ProjectManagementFTE,
        string? Notes,
        string? CostValueKey = null
    );

    /// <summary>Response for a written workload model change.</summary>
    /// <param name="WorkloadModelChangeId"></param>
    /// <param name="Created">False if an existing change on that date was updated</param>
    public sealed record ImportWorkloadModelChangeResponseDTO(
        int WorkloadModelChangeId,
        bool Created
    );
}
