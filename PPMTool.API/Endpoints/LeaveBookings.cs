using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Leave Bookings endpoint methods
/// </summary>
public static class LeaveBookings
{
    /// <summary>
    /// Get all bookings for the year for the staff of the user (inc the user)
    /// Access: superuser, the person, or their line manager.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="configuration"></param>
    /// <param name="http"></param>
    /// <param name="context"></param>
    /// <param name="year">The year to query</param>
    /// <param name="username">The username of the person with spaces replaced with underscores. If not present defaults to the API key owner.</param>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetStaffBookingsForYearAsync(
        ILogger logger,
        IConfiguration configuration,
        HttpContext http,
        PPMToolContext context,
        [FromQuery] int year,
        [FromQuery] string? username = null)
    {
        try
        {
            // Validate year
            if (year <= 1900 || year >= 3000)
            {
                logger.LogWarning("LeaveBookings: Invalid year {Year}", year);
                return Results.BadRequest("A valid year must be supplied.");
            }

            // Resolve username
            var resolvedUsername = ResolveUsername(username, http);
            if (resolvedUsername == null)
            {
                logger.LogWarning("LeaveBookings: Username could not be resolved.");
                return Results.BadRequest("A valid username must be supplied.");
            }

            // Authorise access
            if (!await AuthoriseAccessAsync(context, http, resolvedUsername))
            {
                logger.LogWarning("LeaveBookings: Unauthorized access attempt to {TargetUsername}", resolvedUsername);
                return Results.Unauthorized();
            }

            // Connect to Leave Bookings database
            var connectionString = configuration["LeaveBookings:ConnectionString"];
            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            // Get staff records
            var staffLookup = await LoadStaffAsync(connection, resolvedUsername);
            if (!staffLookup.Any())
            {
                logger.LogWarning("LeaveBookings: No staff records found for {Username}", resolvedUsername);
                return Results.NotFound();
            }

            // Get employee IDs
            var employeeIdList = staffLookup.Keys.ToList();
            await PopulateAllowancesAsync(connection, staffLookup, employeeIdList, year);
            await PopulateBookingsAsync(connection, staffLookup, employeeIdList, year);

            // Convert to DTOs
            var dtos = staffLookup.Values
                .Select(accumulator => accumulator.ToDto())
                .OrderBy(dto => dto.LastName)
                .ThenBy(dto => dto.FirstName)
                .ToList();

            logger.LogInformation("LeaveBookings: Returned {Count} staff records for {Username} in {Year}", dtos.Count, resolvedUsername, year);
            return Results.Json(dtos);
        }
        catch (MySqlException ex)
        {
            logger.LogError(ex, "LeaveBookings: Database error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "LeaveBookings: Unexpected error");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Resolve the username to use
    /// </summary>
    /// <param name="username">The supplied username</param>
    /// <param name="http">The HTTP context</param>
    /// <returns>>The resolved username or null if it cannot be determined</returns>
    private static string? ResolveUsername(string? username, HttpContext http)
    {
        // If no username is supplied, use the API key owner's username
        var candidate = string.IsNullOrWhiteSpace(username)
            ? Helpers.GetCurrentUser(http).CASUserName
            : username;

        candidate = candidate?.Trim();
        return string.IsNullOrEmpty(candidate) ? null : candidate;
    }

    /// <summary>
    /// Authorise access to the requested username's data
    /// </summary>
    /// <param name="context">DB context</param>
    /// <param name="http">The HTTP context</param>
    /// <param name="requestedUsername">The username to authorise</param>
    /// <returns>True if access is authorised, otherwise false</returns>
    private static async Task<bool> AuthoriseAccessAsync(
        PPMToolContext context,
        HttpContext http,
        string requestedUsername)
    {
        var caller = Helpers.GetCurrentUser(http);
        var callerUsername = caller.CASUserName?.Trim();
        if (!string.IsNullOrEmpty(callerUsername) &&
            string.Equals(callerUsername, requestedUsername, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Helpers.IsSuperUser(caller))
        {
            return true;
        }

        var targetUser = await context.Users
            .Include(u => u.Person)
                .ThenInclude(p => p.LineManager)
            .FirstOrDefaultAsync(u => u.CASUserName.ToLower() == requestedUsername.ToLower());

        if (targetUser?.Person == null)
        {
            return false;
        }

        return Helpers.IsSuperUserOrLineManagerOrSelf(context, http, targetUser.Person);
    }

    /// <summary>
    /// Load the staff records
    /// </summary>
    /// <param name="connection">The database connection</param>
    /// <param name="username">The username to query</param>
    /// <returns> A task representing the asynchronous operation </returns>
    private static async Task<Dictionary<string, LeaveBookingAccumulator>> LoadStaffAsync(
        MySqlConnection connection,
        string username)
    {
        // Initialize lookup dictionary
        var lookup = new Dictionary<string, LeaveBookingAccumulator>(StringComparer.OrdinalIgnoreCase);

        // SQL to get staff records
        const string staffSql = @"
            SELECT emp_id, sup_id, username, fname, lname
            FROM epshol2.vf_employee
            WHERE ((sup_id = (SELECT emp_id FROM epshol2.vf_employee WHERE username = @username) AND enabled = 'Y')
                   OR username = @username);";

        // Execute query
        await using var command = new MySqlCommand(staffSql, connection);
        // Add in the username parameter.
        command.Parameters.Add("@username", MySqlDbType.VarChar).Value = username;

        // Read results
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Get the necessary fields
            var employeeId = reader.GetString("emp_id");
            var supervisorIdOrdinal = reader.GetOrdinal("sup_id");
            var supervisorId = reader.IsDBNull(supervisorIdOrdinal) ? 0 : reader.GetInt32(supervisorIdOrdinal);
            var usernameOrdinal = reader.GetOrdinal("username");
            var firstNameOrdinal = reader.GetOrdinal("fname");
            var lastNameOrdinal = reader.GetOrdinal("lname");

            var employeeUsername = reader.IsDBNull(usernameOrdinal) ? string.Empty : reader.GetString(usernameOrdinal);
            var firstName = reader.IsDBNull(firstNameOrdinal) ? string.Empty : reader.GetString(firstNameOrdinal);
            var lastName = reader.IsDBNull(lastNameOrdinal) ? string.Empty : reader.GetString(lastNameOrdinal);

            // Add to lookup. This accumulator is used to gather data from multiple queries later.
            lookup[employeeId] = new LeaveBookingAccumulator(employeeId, supervisorId, employeeUsername, firstName, lastName);
        }

        return lookup;
    }

    /// <summary>
    /// Populate the allowances
    /// </summary>
    /// <param name="connection">The database connection</param>
    /// <param name="lookup">The staff lookup</param>
    /// <param name="employeeIds">The employee IDs to query</param>
    /// <param name="year">The year to query</param>
    /// <returns> A task representing the asynchronous operation </returns>
    private static async Task PopulateAllowancesAsync(
        MySqlConnection connection,
        IDictionary<string, LeaveBookingAccumulator> lookup,
        IReadOnlyList<string> employeeIds,
        int year)
    {
        // If no employee IDs are provided, there's nothing to do
        if (employeeIds.Count == 0)
        {
            return;
        }

        // Build parameterized query
        var parameterNames = employeeIds.Select((_, index) => $"@emp{index}").ToArray();
        var sql = $@"
            SELECT emp_id, hours, hours_carried
            FROM epshol2.vf_emp_to_hours
            WHERE emp_id IN ({string.Join(", ", parameterNames)})
              AND year = @year;";

        // Execute query
        await using var command = new MySqlCommand(sql, connection);

        // Add parameterNames that was built earlier. This should make it safe from any sort of attack.
        for (var i = 0; i < employeeIds.Count; i++)
        {
            command.Parameters.Add(parameterNames[i], MySqlDbType.VarChar).Value = employeeIds[i];
        }

        // Add in the year parameter last.
        command.Parameters.Add("@year", MySqlDbType.Int32).Value = year;

        // Read results
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Get employee ID
            var employeeId = reader.GetString("emp_id");

            // Check if we have this employee in our lookup
            if (!lookup.TryGetValue(employeeId, out var accumulator))
            {
                continue;
            }

            // Get allowance values
            var coreAllowanceOrdinal = reader.GetOrdinal("hours");
            var adjustmentOrdinal = reader.GetOrdinal("hours_carried");

            // Update accumulator. Accumulartor is used to gather data from multiple queries before converting to DTO.
            accumulator.CoreAllowance = reader.IsDBNull(coreAllowanceOrdinal)
                ? 0d
                : reader.GetDouble(coreAllowanceOrdinal);
            accumulator.Adjustment = reader.IsDBNull(adjustmentOrdinal)
                ? 0d
                : reader.GetDouble(adjustmentOrdinal);
        }
    }

    /// <summary>
    /// Populate the bookings
    /// </summary>
    /// <param name="connection">The database connection</param>
    /// <param name="lookup">The staff lookup</param>
    /// <param name="employeeIds">The employee IDs to query</param>
    /// <param name="year">The year to query</param>
    /// <returns> A task representing the asynchronous operation</returns>
    private static async Task PopulateBookingsAsync(
        MySqlConnection connection,
        IDictionary<string, LeaveBookingAccumulator> lookup,
        IReadOnlyList<string> employeeIds,
        int year)
    {
        // If no employee IDs are provided, there's nothing to do.
        if (employeeIds.Count == 0)
        {
            return;
        }
        // Build parameterized query.
        var parameterNames = employeeIds.Select((_, index) => $"@emp{index}").ToArray();
        var sql = $@"
            SELECT emp_id, date, ampm
            FROM epshol2.vf_vacation
            WHERE emp_id IN ({string.Join(", ", parameterNames)})
              AND deny <> 'Y'
              AND year = @year;";
        // Execute query.
        await using var command = new MySqlCommand(sql, connection);
        // Add parameterNames that was built earlier. This should make it safe from any sort of attack.
        for (var i = 0; i < employeeIds.Count; i++)
        {
            command.Parameters.Add(parameterNames[i], MySqlDbType.VarChar).Value = employeeIds[i];
        }
        // Add in the year parameter last.
        command.Parameters.Add("@year", MySqlDbType.Int32).Value = year;

        // Read results.
        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            // Get employee ID.
            var employeeId = reader.GetString("emp_id");

            // Check if we have this employee in our lookup.
            if (!lookup.TryGetValue(employeeId, out var accumulator))
            {
                continue;
            }
            // Get booking values.
            var date = reader.GetDateTime("date");
            var ampmOrdinal = reader.GetOrdinal("ampm");
            var ampm = reader.IsDBNull(ampmOrdinal)
                ? string.Empty
                : reader.GetString(ampmOrdinal);
            // Add booking to accumulator. Accumulator is used to gather data from multiple queries before converting to DTO.
            accumulator.Bookings.Add(new BookingDTO(date, ampm));
        }
    }

    /// <summary>
    /// Accumulator for building up leave booking data before converting to DTO.
    /// Used to gather data from multiple queries. Then converted to DTO at the end.
    /// </summary>
    private sealed class LeaveBookingAccumulator
    {
        internal LeaveBookingAccumulator(
            string employeeId,
            int supervisorId,
            string username,
            string firstName,
            string lastName)
        {
            EmployeeId = employeeId;
            SupervisorId = supervisorId;
            Username = username;
            FirstName = firstName;
            LastName = lastName;
        }

        internal string EmployeeId { get; }
        internal int SupervisorId { get; }
        internal string Username { get; }
        internal string FirstName { get; }
        internal string LastName { get; }
        internal double CoreAllowance { get; set; }
        internal double Adjustment { get; set; }
        internal List<BookingDTO> Bookings { get; } = new();

        /// <summary>
        /// Convert to DTO
        /// </summary>
        /// <returns> The DTO </returns>
        internal LeaveBookingsDTO ToDto() =>
            new(
                EmployeeId,
                SupervisorId,
                Username,
                FirstName,
                LastName,
                CoreAllowance,
                Adjustment,
                Bookings
                    .OrderBy(b => b.Date)
                    .ToList()
            );
    }
}
