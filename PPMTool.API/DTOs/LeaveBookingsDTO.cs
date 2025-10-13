namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A Staff entry from the central system.
    /// </summary>
    public class LeaveBookingsDTO
    {
        /// <summary>
        /// Employeed Id : UoM staff number (or an actual string in the case of the Admin account)
        /// </summary>
        public string EmployeeId { get; set; }

        /// <summary>
        /// If of the user's Line Managaer
        /// </summary>
        public int SupervisorId { get; set; }

        /// <summary>
        /// UoM username
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// User's forename
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// User's surname
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Base Leave allowance for a specified year
        /// </summary>
        public double CoreAllowance { get; set; } = 0.0;

        /// <summary>
        /// Allowance adjustment for the year of concern
        /// </summary>
        public double Adjustment { get; set; } = 0.0;

        /// <summary>
        /// The user's bookings for the year of concern
        /// </summary>
        public List<BookingDTO> Bookings { get; set; } = new();

        /// <summary>
        /// Class constructor : only sets the detail known on first use
        /// </summary>
        /// <param name="employeeId"></param>
        /// <param name="supervisorId"></param>
        /// <param name="username"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        public LeaveBookingsDTO(string employeeId, int supervisorId, string username, string firstName, string lastName)
        {
            EmployeeId = employeeId;
            SupervisorId = supervisorId;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
        }
    }

    /// <summary>
    /// A Leave Booking entry
    /// </summary>
    public record BookingDTO(
        DateTime Date,
        string AmPm
    );
}