namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A Leave Booking entry from the central system.
    /// </summary>
    public sealed record LeaveBookingsDTO(
        int EmployeeId,
        int SupervisorId,
        string Username,
        string FirstName,
        string LastName,
        DateTime Date,
        string AmPm
    );
}