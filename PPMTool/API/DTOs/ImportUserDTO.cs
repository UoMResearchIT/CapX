// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// Request body for POST /api/users/add. Creates a User, as a Superuser can
    /// on the Manage Access page: a bare account, or one linked to an existing
    /// Person (a Person can have more than one User). Signing in only finds an
    /// existing User; it never creates one.
    /// </summary>
    /// <param name="CASUserName">Unique, ignoring case and surrounding whitespace</param>
    /// <param name="EmailAddress">Used to match sign-in claims; semicolon-separated if more than one</param>
    /// <param name="Name">Display name; required without PersonId, and replaced by the Person's name with one</param>
    /// <param name="PersonId">Person to link the User to, if any</param>
    /// <param name="RoleType">A PPMTool.Data.Enums.RoleType name; defaults to "None"</param>
    public sealed record ImportUserDTO(
        string CASUserName,
        string EmailAddress,
        string? Name,
        int? PersonId,
        string? RoleType
    );

    /// <summary>Response for a created User.</summary>
    /// <param name="UserId"></param>
    public sealed record ImportUserResponseDTO(
        int UserId
    );
}
