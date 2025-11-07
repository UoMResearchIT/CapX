namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A single row from a weekly timesheet, holding hours for each day.
    /// </summary>
    /// <param name="TimesheetEntryId"></param>
    /// <param name="InnateCodeTaskId"></param>
    /// <param name="InnateCode"></param>
    /// <param name="InnateCodeName"></param>
    /// <param name="TaskName"></param>
    /// <param name="Duty"></param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
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
    /// <param name="TimesheetId"></param>
    /// <param name="OwnerId"></param>
    /// <param name="OwnerName"></param>
    /// <param name="CreatedDate"></param>
    /// <param name="StartDate"></param>
    /// <param name="Status"></param>
    /// <param name="DateStatusChanged"></param>
    /// <param name="Info"></param>
    /// <param name="Entries"></param>
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
    /// Aggregated summary of hours booked by a person for a specific code/task.
    /// </summary>
    /// <param name="PersonName"></param>
    /// <param name="TotalHours"></param>
    public sealed record PersonHoursSummaryDTO(
        string PersonName,
        double TotalHours
    );

    /// <summary>
    /// Response for timesheet bookings by code/task query.
    /// Reuses existing TimesheetsDTO structure and adds aggregated summary for capacity analysis.
    /// </summary>
    /// <param name="Timesheets"></param>
    /// <param name="Summary"></param>
    /// <param name="GrandTotalHours"></param>
    public sealed record TimesheetsByCodeTaskResponseDTO(
        IReadOnlyList<TimesheetsDTO> Timesheets,
        IReadOnlyList<PersonHoursSummaryDTO> Summary,
        double GrandTotalHours
    );

    /// <summary>
    /// Represents a row of the CSV file export
    /// </summary>
    /// <param name="PersonName"></param>
    /// <param name="TimesheetWeekStart"></param>
    /// <param name="TimesheetStatus"></param>
    /// <param name="TimesheetInfo"></param>
    /// <param name="InnateCode"></param>
    /// <param name="InnateCodeName"></param>
    /// <param name="TaskName"></param>
    /// <param name="Duty"></param>
    /// <param name="MondayHours"></param>
    /// <param name="TuesdayHours"></param>
    /// <param name="WednesdayHours"></param>
    /// <param name="ThursdayHours"></param>
    /// <param name="FridayHours"></param>
    /// <param name="SaturdayHours"></param>
    /// <param name="SundayHours"></param>
    /// <param name="TotalHoursForWeek"></param>
    public sealed record TimesheetCSVRowData(
        string PersonName,
        string TimesheetWeekStart,
        string TimesheetStatus,
        string TimesheetInfo,
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
        double TotalHoursForWeek
    );
}