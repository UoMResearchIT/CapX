// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using PPMTool.Data.Enums.Attributes;

namespace PPMTool.Data.Enums
{
    public enum SkillRareness
    {
        [Icon("common")]
        [Colour("", "#888")]
        Common,
        [Icon("uncommon")]
        [Colour("", "#00f")]
        Uncommon,
        [Icon("rare")]
        [Colour("", "#396")]
        Rare,
        [Icon("epic")]
        [Colour("", "#609")]
        Epic,
        [Icon("legendary")]
        [Colour("", "#f00")]
        Legendary
    }
}
