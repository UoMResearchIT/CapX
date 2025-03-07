// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.ComponentModel;

namespace PPMTool.Enums
{
    public enum AssessmentStatus
    {
        Unmet,
        [Description("Partially Met")]
        PartiallyMet,
        [Description("Fully Met")]
        FullyMet
    }
}
