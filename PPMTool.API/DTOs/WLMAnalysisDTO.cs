// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

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
}