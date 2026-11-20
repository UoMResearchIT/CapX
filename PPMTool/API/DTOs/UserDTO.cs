// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// A User as the API's endpoints use it, and the request body for POST /api/users/add:
    /// one record serves both, as the API has no separate User read record. Users are
    /// created on Manage Access, or here; signing in never creates one.
    /// </summary>
    /// <param name="CASUserName">Unique, ignoring case and surrounding whitespace</param>
    /// <param name="EmailAddress">Used to match sign-in claims; semicolon-separated if more than one</param>
    /// <param name="Name">Display name; required without PersonId, and replaced by the Person's name with one</param>
    /// <param name="PersonId">Person to link the User to, if any</param>
    /// <param name="RoleType">A <see cref="PPMTool.Data.Enums.RoleType"/> name; defaults to "None"</param>
    public sealed record UserDTO(
        string CASUserName,
        string EmailAddress,
        string? Name,
        int? PersonId,
        string? RoleType
    );

    /// <summary>Response for a created User.</summary>
    /// <param name="UserId"></param>
    public sealed record WriteUserResponseDTO(
        int UserId
    );
}
