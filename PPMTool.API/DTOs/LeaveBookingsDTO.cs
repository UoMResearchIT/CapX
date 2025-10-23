namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A staff leave summary sourced from the central leave booking system.
    /// </summary>
    public sealed record LeaveBookingsDTO(
        string EmployeeId,
        int SupervisorId,
        string Username,
        string FirstName,
        string LastName,
        double CoreAllowance,
        double Adjustment,
        IReadOnlyList<BookingDTO> Bookings
    );

    /// <summary>
    /// A single leave booking entry.
    /// </summary>
    public sealed record BookingDTO(
        DateTime Date,
        string AmPm
    );
}
