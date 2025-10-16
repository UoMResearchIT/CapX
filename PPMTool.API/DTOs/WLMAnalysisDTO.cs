namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Represents the complete WLM analysis data for a single person.
    /// </summary>
    public sealed record WLMAnalysisPersonDataDTO(
        string PersonName,
        List<WLMWeeklyAnalysisDTO> WeeklyData
    );

    /// <summary>
    /// Represents the analysis for a single week, containing the calculated values for each duty.
    /// </summary>
    public sealed record WLMWeeklyAnalysisDTO(
        DateTime WeekStart,
        string Units,
        Dictionary<string, float> Duties
    );
}