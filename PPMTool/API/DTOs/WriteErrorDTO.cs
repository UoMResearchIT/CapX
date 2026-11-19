// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

#nullable enable

namespace PPMTool.API.DTOs
{
    /// <summary>Response for a rejected write. Nothing was written.</summary>
    /// <param name="Errors">Every validation error found</param>
    public sealed record WriteErrorDTO(
        IReadOnlyList<string> Errors
    );
}
