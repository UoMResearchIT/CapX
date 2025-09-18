using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using Microsoft.EntityFrameworkCore;
using PPMTool.API.DTOs;
using PPMTool.Data.Context;
using System.Dynamic;
using System.Data;

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
    /// Get all RIT Leave booking instances from the
    /// central db for the relevant year
    /// </summary>
    public static async Task<List<LeaveBookingsDTO>> GetBookingsForYearAsync(PPMToolContext context, ILogger logger, int year)
    {
        // Remove this into env file so not checked into Git.
        string connection = "Server=servalan.its.manchester.ac.uk;Port=3306;Database=epshol2;Uid=rit_readonly;Pwd=Or7WroucJuont{;";
        var results = new List<LeaveBookingsDTO>();

        using var conn = new MySqlConnection(connection);
        await conn.OpenAsync();

        using var cmd = new MySqlCommand($"SELECT * FROM epshol2.vf_vacation v join epshol2.vf_employee e on v.emp_id = e.emp_id WHERE e.sub_dept_id=97 AND v.year={year};", conn);
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            results.Add(new LeaveBookingsDTO
            (
                EmployeeId: reader.GetInt32("emp_id"),
                SupervisorId: reader.GetInt32("sup_id"),
                Username: reader.GetString("username"),
                FirstName: reader.GetString("fname"),
                LastName: reader.GetString("lname"),
                Date: reader.GetDateTime("date"),
                AmPm: reader.GetString("ampm")
            ));
        }

        return results;
    }

    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<LeaveBookingsDTO>))]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public static async Task<List<LeaveBookingsDTO>> GetMyStaffBookingsAsync()
    {
        var bookings = new List<LeaveBookingsDTO>();

        // Get Leave Bookings db Id for employee based on username [table : vf_employee]

        // Get list of staff Ids using above as sup_id [table : vf_employee]

        // Either recraft GetBookingsForYearAsync method to only get staff bookings of the user 
        // using the "MyStaff" details or get all and filter in C# before display instead

        return bookings;
    }
}