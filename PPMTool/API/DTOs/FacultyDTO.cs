// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A flat representation of a faculty suitable for API data exports.
    /// </summary>
    /// <param name="FacultyId">The unique faculty ID.</param>
    /// <param name="Name">The name of the faculty.</param>
    public sealed record FacultyDTO(
        int FacultyId,
        string Name
    );
}