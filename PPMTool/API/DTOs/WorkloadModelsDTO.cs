// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

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
    /// <param name="PersonId">The ID of the person to whom the workload model applies.</param>
    /// <param name="ChangeDate">The date on which the workload model came into effect.</param>
    /// <param name="Grade">The person's grade.</param>
    /// <param name="ProjectWorkFTE">The FTE allocated to project work.</param>
    /// <param name="StaffManagementFTE">The FTE allocated to staff management.</param>
    /// <param name="ProjectManagementFTE">The FTE allocated to project management.</param>
    /// <param name="ServiceManagementFTE">The FTE allocated to service management.</param>
    /// <param name="BusinessAsUsualFTE">The FTE allocated to business-as-usual work.</param>
    /// <param name="PersonalDevelopmentFTE">The FTE allocated to personal development.</param>
    /// <param name="ArchitectureFTE">The FTE allocated to architecture work.</param>
    public sealed record WorkloadModelChangeDTO(
        int PersonId,
        DateOnly ChangeDate,
        int Grade,
        double ProjectWorkFTE,
        double StaffManagementFTE,
        double ProjectManagementFTE,
        double ServiceManagementFTE,
        double BusinessAsUsualFTE,
        double PersonalDevelopmentFTE,
        double ArchitectureFTE
    );
}