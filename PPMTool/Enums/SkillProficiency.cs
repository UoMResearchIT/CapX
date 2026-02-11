// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0

using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum SkillProficiency
    {
        [Description("Not Yet Rated")]
        NotRated,
        None,
        Beginner,
        Intermediate,
        Advanced,
        Guru
    }
}
