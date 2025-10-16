using System.Data;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using PPMTool.API.DTOs;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Leave Bookings endpoint methods
/// </summary>
public static class LeaveBookings
{
    /// <summary>
    /// Get all bookings for the year for the staff of the user (inc the user)
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<IResult> GetMyStaffBookingsForYearAsync(
        ILogger logger,
        IConfiguration configuration,
        HttpContext http,
        [FromQuery] int year,
        [FromQuery] string? username = null)
    {
        try
        {
            if (year <= 1900 || year >= 3000)
            {
                logger.LogWarning("LeaveBookings: Invalid year {Year}", year);
                return Results.BadRequest("A valid year must be supplied.");
            }

            var resolvedUsername = ResolveUsername(username, http);
            if (resolvedUsername == null)
            {
                logger.LogWarning("LeaveBookings: Username could not be resolved.");
                return Results.BadRequest("A valid username must be supplied.");
            }

            var connectionString = configuration["LeaveBookings:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogError("LeaveBookings: Connection string not configured.");
                return Results.StatusCode(StatusCodes.Status500InternalServerError);
            }

            await using var connection = new MySqlConnection(connectionString);
            await connection.OpenAsync();

            var staffLookup = await LoadStaffAsync(connection, resolvedUsername);
            if (!staffLookup.Any())
            {
                logger.LogWarning("LeaveBookings: No staff records found for {Username}", resolvedUsername);
                return Results.NotFound();
            }

            var employeeIdList = staffLookup.Keys.ToList();
            await PopulateAllowancesAsync(connection, staffLookup, employeeIdList, year);
            await PopulateBookingsAsync(connection, staffLookup, employeeIdList, year);

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

    private static string? ResolveUsername(string? username, HttpContext http)
    {
        var candidate = string.IsNullOrWhiteSpace(username)
            ? Helpers.GetCurrentUser(http).CASUserName
            : username;

        candidate = candidate?.Trim();
        return string.IsNullOrEmpty(candidate) ? null : candidate;
    }

    private static async Task<Dictionary<string, LeaveBookingAccumulator>> LoadStaffAsync(
        MySqlConnection connection,
        string username)
    {
        var lookup = new Dictionary<string, LeaveBookingAccumulator>(StringComparer.OrdinalIgnoreCase);

        const string staffSql = @"
            SELECT emp_id, sup_id, username, fname, lname
            FROM epshol2.vf_employee
            WHERE ((sup_id = (SELECT emp_id FROM epshol2.vf_employee WHERE username = @username) AND enabled = 'Y')
                   OR username = @username);";

        await using var command = new MySqlCommand(staffSql, connection);
        command.Parameters.Add("@username", MySqlDbType.VarChar).Value = username;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var employeeId = reader.GetString("emp_id");
            var supervisorIdOrdinal = reader.GetOrdinal("sup_id");
            var supervisorId = reader.IsDBNull(supervisorIdOrdinal) ? 0 : reader.GetInt32(supervisorIdOrdinal);
            var usernameOrdinal = reader.GetOrdinal("username");
            var firstNameOrdinal = reader.GetOrdinal("fname");
            var lastNameOrdinal = reader.GetOrdinal("lname");

            var employeeUsername = reader.IsDBNull(usernameOrdinal) ? string.Empty : reader.GetString(usernameOrdinal);
            var firstName = reader.IsDBNull(firstNameOrdinal) ? string.Empty : reader.GetString(firstNameOrdinal);
            var lastName = reader.IsDBNull(lastNameOrdinal) ? string.Empty : reader.GetString(lastNameOrdinal);

            lookup[employeeId] = new LeaveBookingAccumulator(employeeId, supervisorId, employeeUsername, firstName, lastName);
        }

        return lookup;
    }

    private static async Task PopulateAllowancesAsync(
        MySqlConnection connection,
        IDictionary<string, LeaveBookingAccumulator> lookup,
        IReadOnlyList<string> employeeIds,
        int year)
    {
        if (employeeIds.Count == 0)
        {
            return;
        }

        var parameterNames = employeeIds.Select((_, index) => $"@emp{index}").ToArray();
        var sql = $@"
            SELECT emp_id, hours, hours_carried
            FROM epshol2.vf_emp_to_hours
            WHERE emp_id IN ({string.Join(", ", parameterNames)})
              AND year = @year;";

        await using var command = new MySqlCommand(sql, connection);
        for (var i = 0; i < employeeIds.Count; i++)
        {
            command.Parameters.Add(parameterNames[i], MySqlDbType.VarChar).Value = employeeIds[i];
        }
        command.Parameters.Add("@year", MySqlDbType.Int32).Value = year;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var employeeId = reader.GetString("emp_id");
            if (!lookup.TryGetValue(employeeId, out var accumulator))
            {
                continue;
            }

            var coreAllowanceOrdinal = reader.GetOrdinal("hours");
            var adjustmentOrdinal = reader.GetOrdinal("hours_carried");

            accumulator.CoreAllowance = reader.IsDBNull(coreAllowanceOrdinal)
                ? 0d
                : reader.GetDouble(coreAllowanceOrdinal);
            accumulator.Adjustment = reader.IsDBNull(adjustmentOrdinal)
                ? 0d
                : reader.GetDouble(adjustmentOrdinal);
        }
    }

    private static async Task PopulateBookingsAsync(
        MySqlConnection connection,
        IDictionary<string, LeaveBookingAccumulator> lookup,
        IReadOnlyList<string> employeeIds,
        int year)
    {
        if (employeeIds.Count == 0)
        {
            return;
        }

        var parameterNames = employeeIds.Select((_, index) => $"@emp{index}").ToArray();
        var sql = $@"
            SELECT emp_id, date, ampm
            FROM epshol2.vf_vacation
            WHERE emp_id IN ({string.Join(", ", parameterNames)})
              AND deny <> 'Y'
              AND year = @year;";

        await using var command = new MySqlCommand(sql, connection);
        for (var i = 0; i < employeeIds.Count; i++)
        {
            command.Parameters.Add(parameterNames[i], MySqlDbType.VarChar).Value = employeeIds[i];
        }
        command.Parameters.Add("@year", MySqlDbType.Int32).Value = year;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var employeeId = reader.GetString("emp_id");
            if (!lookup.TryGetValue(employeeId, out var accumulator))
            {
                continue;
            }

            var date = reader.GetDateTime("date");
            var ampmOrdinal = reader.GetOrdinal("ampm");
            var ampm = reader.IsDBNull(ampmOrdinal)
                ? string.Empty
                : reader.GetString(ampmOrdinal);

            accumulator.Bookings.Add(new BookingDTO(date, ampm));
        }
    }

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
