// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;
using PPMTool.Enums.Attributes;
using Radzen;

namespace PPMTool.Enums
{
    public enum AssessmentStatus
    {
        [BadgeStyle(BadgeStyle.Danger)]
        Unmet,
        [BadgeStyle(BadgeStyle.Warning)]
        [Description("Partially Met")]
        PartiallyMet,
        [BadgeStyle(BadgeStyle.Success)]
        [Description("Fully Met")]
        FullyMet
    }
}
