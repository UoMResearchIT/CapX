namespace PPMTool.API.DTOs
{
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
    public sealed record TimesheetCSVDTO(
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