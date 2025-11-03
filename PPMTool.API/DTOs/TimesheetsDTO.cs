namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A single row from a weekly timesheet, holding hours for each day.
    /// </summary>
    public sealed record TimesheetEntryDTO(
        int TimesheetEntryId,
        int InnateCodeTaskId,
        string InnateCode,
        string InnateCodeName,
        string TaskName,
        string Duty,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours
    );

    /// <summary>
    /// A weekly timesheet with metadata and nested entries.
    /// </summary>
    public sealed record TimesheetsDTO(
        int TimesheetId,
        int OwnerId,
        string OwnerName,
        DateTime CreatedDate,
        DateTime StartDate,
        string Status,
        DateTime DateStatusChanged,
        string? Info,
        IReadOnlyList<TimesheetEntryDTO> Entries
    );

    /// <summary>
    /// A single timesheet entry booking for a specific person, code/task combination.
    /// </summary>
    public sealed record TimesheetBookingEntryDTO(
        string PersonName,
        DateTime WeekStartDate,
        string TimesheetStatus,
        string InnateCode,
        string InnateCodeName,
        string TaskName,
        string Duty,
        double MondayHours,
        double TuesdayHours,
        double WednesdayHours,
        double ThursdayHours,
        double FridayHours,
        double SaturdayHours,
        double SundayHours,
        double TotalHours
    );

    /// <summary>
    /// Aggregated summary of hours booked by a person.
    /// </summary>
    public sealed record PersonBookingSummaryDTO(
        string PersonName,
        double TotalHours
    );

    /// <summary>
    /// Complete response for timesheet bookings by code/task query, including both detailed entries and aggregated summary.
    /// StartDate and EndDate are null when no date filtering was applied (returns all historical data).
    /// </summary>
    public sealed record TimesheetBookingsByCodeTaskDTO(
        string Code,
        string? TaskName,
        DateTime? StartDate,
        DateTime? EndDate,
        IReadOnlyList<TimesheetBookingEntryDTO> Entries,
        IReadOnlyList<PersonBookingSummaryDTO> Summary,
        double GrandTotalHours
    );
}