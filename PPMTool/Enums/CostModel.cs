// SPDX-FileCopyrightText: 2026 University of Manchester
//
// SPDX-License-Identifier: Apache-2.0
// SPDX-License-Identifier: apache-2.0

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace PPMTool.Enums
{
    public enum CostModel
    {
        [Display(Name = "Day Rate")]
        [Description("Uses a specific day rate")]
        DayRate,
        [Display(Name = "Technical Only")]
        [Description("Planned costs computed from resource mid-grades or rates; no leadership charge")]
        TechOnly,
        [Display(Name = "Technical and Leadership")]
        [Description("Planned costs computed from resource mid-grades or rates; leadership charge added over duration")]
        TechAndLeadership,
        [Display(Name = "Technical Only with Indirects")]
        [Description("Planned costs computed from resource mid-grades or rates; no leadership charge; indirects computed based on global rate")]
        TechOnlyWithIndirects,
        [Display(Name = "Technical and Leadership with Indirects")]
        [Description("Planned costs computed from resource mid-grades or rates; leadership charge added over duration; indirects computed based on global rate")]
        TechAndLeadershipWithIndirects
    }
}
