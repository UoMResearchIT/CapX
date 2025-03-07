// SPDX-FileCopyrightText: 2025 University of Manchester
//
// SPDX-License-Identifier: apache-2.0

﻿using System.ComponentModel;

namespace PPMTool.Enums
{
    /// <summary>
    /// The type of task. This influences which of the three parameters remains fixed during scheduling.
    /// </summary>
    public enum TaskType
    {
        /// <summary>
        /// When scheduling, maintain work and vary duration (assuming fixed units)
        /// </summary>
        [Description("Fixed Work")]
        FixedWork,

        /// <summary>
        /// When scheduling, maintain duration and vary work (assuming fixed units)
        /// </summary>
        [Description("Fixed Duration")]
        FixedDuration
    }
}
