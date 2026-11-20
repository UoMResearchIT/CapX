// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Represents the complete WLM analysis data for a single person.
    /// </summary>
    /// <param name="PersonName"></param>
    /// <param name="WeeklyData"></param>
    public sealed record WLMAnalysisPersonDataDTO(
        string PersonName,
        List<WLMWeeklyAnalysisDTO> WeeklyData
    );

    /// <summary>
    /// Represents the analysis for a single week, containing the calculated values for each duty.
    /// </summary>
    /// <param name="WeekStart"></param>
    /// <param name="Units"></param>
    /// <param name="Duties"></param>
    public sealed record WLMWeeklyAnalysisDTO(
        DateTime WeekStart,
        string Units,
        Dictionary<string, float> Duties
    );

    /// <summary>
    /// A flat representation of a workload model change suitable for
    /// API data exports.
    /// </summary>
    /// <param name="WorkloadModelChangeId">Primary key of the WLM change.</param>
    /// <param name="PersonId">The ID of the person to whom the workload model applies.</param>
    /// <param name="ChangeDate">The date on which the workload model came into effect.</param>
    /// <param name="Grade">The person's grade.</param>
    /// <param name="CostValueKey">Key value the financial reference value set to use for the costs.</param>
    /// <param name="ProjectWorkFTE">The FTE allocated to project work.</param>
    /// <param name="StaffManagementFTE">The FTE allocated to staff management.</param>
    /// <param name="ProjectManagementFTE">The FTE allocated to project management.</param>
    /// <param name="ServiceManagementFTE">The FTE allocated to service management.</param>
    /// <param name="BusinessAsUsualFTE">The FTE allocated to business-as-usual work.</param>
    /// <param name="PersonalDevelopmentFTE">The FTE allocated to personal development.</param>
    /// <param name="ArchitectureFTE">The FTE allocated to architecture work.</param>
    public sealed record WorkloadModelChangeDTO(
        int WorkloadModelChangeId,
        int PersonId,
        DateOnly ChangeDate,
        int Grade,
        string? CostValueKey,
        double ProjectWorkFTE,
        double StaffManagementFTE,
        double ProjectManagementFTE,
        double ServiceManagementFTE,
        double BusinessAsUsualFTE,
        double PersonalDevelopmentFTE,
        double ArchitectureFTE
    );

    /// <summary>
    /// Request body for POST /api/workloadmodels/add. A person's duty split,
    /// in effect from ChangeDate until their next change, with the same
    /// ranges the UI enforces. ProjectAndServiceManagementFTE is derived from
    /// ProjectManagementFTE and ServiceManagementFTE. A change on the same
    /// date as an existing one updates it.
    /// Kept separate from <see cref="WorkloadModelChangeDTO"/>: a request carries only what the caller
    /// supplies, and the read record adds the ids and fields CapX derives or links.
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
    public sealed record WriteWorkloadModelChangeDTO(
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
    public sealed record WriteWorkloadModelChangeResponseDTO(
        int WorkloadModelChangeId,
        bool Created
    );
}