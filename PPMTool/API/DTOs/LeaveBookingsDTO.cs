// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A staff leave summary sourced from the central leave booking system.
    /// </summary>
    /// <param name="EmployeeId"></param>
    /// <param name="SupervisorId"></param>
    /// <param name="Username"></param>
    /// <param name="FirstName"></param>
    /// <param name="LastName"></param>
    /// <param name="CoreAllowance"></param>
    /// <param name="Adjustment"></param>
    /// <param name="Bookings"></param>
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
    /// <param name="Date"></param>
    /// <param name="AmPm"></param>
    public sealed record BookingDTO(
        DateTime Date,
        string AmPm
    );
}
