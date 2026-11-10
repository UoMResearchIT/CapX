// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a school suitable for API data exports.
    /// </summary>
    /// <param name="SchoolId">The unique school ID.</param>
    /// <param name="FacultyId">The ID of the faculty that the school belongs to.</param>
    /// <param name="Name">The name of the school.</param>
    public sealed record SchoolDTO(
        int SchoolId,
        int FacultyId,
        string Name
    );
}