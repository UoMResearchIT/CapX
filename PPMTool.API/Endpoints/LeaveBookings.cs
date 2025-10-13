using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using System.Dynamic;
using System.Data;
using DocumentFormat.OpenXml.Office.Word;

namespace PPMTool.API.Endpoints;

/// <summary>
/// Leave Bookings endpoint methods
/// </summary>
///     [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
[ProducesResponseType(StatusCodes.Status500InternalServerError)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public static class LeaveBookings
{
    /// <summary>
    /// Get all bookings for the year for the staff of the user (inc the user)
    /// </summary>
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<List<LeaveBookingsDTO>> GetMyStaffBookingsForYearAsync(string username, int year)
    {
        var results = new Dictionary<string, LeaveBookingsDTO>();
#if LOCAL
        // Requires a mapping from local machine to route to the MySQL server via Jumpbox. Requires access to the Jumpbox and also the CapX Dev VM
        // Using: ssh -J <<your username>>@styx1.itservices.manchester.ac.uk <<your username>>@10.99.96.160 -L 3307:servalan.its.manchester.ac.uk:3306 -v -N
        string connection = "Server=127.0.0.1;Port=3307;Database=epshol2;Uid=rit_readonly;Pwd=Or7WroucJuont{;";
#else
        string connection = "Server=servalan.its.manchester.ac.uk;Port=3306;Database=epshol2;Uid=rit_readonly;Pwd=Or7WroucJuont{;";
#endif
        using var conn = new MySqlConnection(connection);
        await conn.OpenAsync();

        // Use the provided username to get user's (and any staff who report to them) details.
        // Will use the emp_ids later to shape the final query getting the relevant bookings
        using var staffCmd = new MySqlCommand($"SELECT * FROM epshol2.vf_employee WHERE (sup_id = (select emp_id from epshol2.vf_employee WHERE username='{username}') AND enabled='Y') OR username='{username}';", conn);
        using var staffReader = await staffCmd.ExecuteReaderAsync();

        while (await staffReader.ReadAsync())
        {
            // Store the results so we can easily add to them later
            var employee_id = staffReader.GetString("emp_id");

            results.Add(employee_id, new LeaveBookingsDTO
            (
                employee_id,
                staffReader.GetInt32("sup_id"),
                staffReader.GetString("username"),
                staffReader.GetString("fname"),
                staffReader.GetString("lname")
            ));
        }

        await staffReader.CloseAsync();

        // Get a list of the relevant employee ids so we can target the sql statements being used
        var employeeIds = results.Keys.ToList();

        // ADJUSTMENTS
        using var adjustmentsCmd = new MySqlCommand($"SELECT * FROM epshol2.vf_emp_to_hours WHERE emp_id in ({string.Join(",", employeeIds)}) AND year={year}", conn);
        using var adjustmentsReader = await adjustmentsCmd.ExecuteReaderAsync();

        while (await adjustmentsReader.ReadAsync())
        {
            var employeeId = adjustmentsReader.GetString("emp_id");
            results[employeeId].CoreAllowance = adjustmentsReader.IsDBNull("hours") ? 0.0 : adjustmentsReader.GetDouble("hours");
            results[employeeId].Adjustment = adjustmentsReader.IsDBNull("hours_carried") ? 0.0 : adjustmentsReader.GetDouble("hours_carried");
        }

        await adjustmentsReader.CloseAsync();

        // LEAVE BOOKINGS
        using var bookingsCmd = new MySqlCommand($"SELECT * FROM epshol2.vf_vacation WHERE emp_id in ({string.Join(", ", employeeIds)}) AND deny <> 'Y' AND year='{year}';", conn);
        using var bookingsReader = await bookingsCmd.ExecuteReaderAsync();

        while (await bookingsReader.ReadAsync())
        {
            var employeeId = bookingsReader.GetString("emp_id");
            var date = bookingsReader.GetDateTime("date");
            var ampm = bookingsReader.GetString("ampm");

            results[employeeId].Bookings.Add(new BookingDTO(date, ampm));
        }

        await bookingsReader.CloseAsync();

        return results.Values.ToList();
    }
}