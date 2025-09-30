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
}