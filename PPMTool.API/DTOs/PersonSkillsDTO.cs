// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

namespace PPMTool.API.DTOs
{
    /// <summary>
    /// DTO simplifying the representation of a skill tag.
    /// </summary>
    /// <param name="SkillTagId"></param>
    /// <param name="Name"></param>
    public sealed record SkillTagDTO(
        int SkillTagId,
        string Name
    );

    /// <summary>
    /// DTO grouping a person's name with their owned skills.
    /// </summary>
    /// <param name="Name"></param>
    /// <param name="Skills"></param>
    public sealed record PersonSkillsDTO(
        string Name,
        IEnumerable<SkillTagDTO> Skills
    );
}